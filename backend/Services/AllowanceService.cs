using System.Data;
using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Workforce;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace InternshipManagementApi.Services;

public interface IAllowanceService
{
    Task<PagedResult<AllowanceDto>> GetAllAsync(int? studentId, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize, CancellationToken cancellationToken);
    Task<AllowanceDto> GetAsync(int id, CancellationToken cancellationToken);
    Task<AllowanceDto> CreateAsync(int studentId, AllowanceRequestDto request, CancellationToken cancellationToken);
    Task<AllowanceDto> UpdateAsync(int id, AllowanceRequestDto request, CancellationToken cancellationToken);
    Task CancelAsync(int id, CancellationToken cancellationToken);
    Task<AllowancePaymentDto> RecordPaymentAsync(int userId, int allowanceId, AllowancePaymentRequestDto request, CancellationToken cancellationToken);
    Task<AllowanceHistoryResponseDto> GetMineAsync(int userId, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize, CancellationToken cancellationToken);
}

public sealed class AllowanceService : IAllowanceService
{
    private readonly AppDbContext _db;
    public AllowanceService(AppDbContext db) => _db = db;

    public async Task<PagedResult<AllowanceDto>> GetAllAsync(int? studentId, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize, CancellationToken cancellationToken)
    {
        AttendanceService.ValidateDateRange(fromDate, toDate);
        var query = _db.Allowances.AsNoTracking().Include(item => item.Student).AsQueryable();
        if (studentId.HasValue) query = query.Where(item => item.StudentId == studentId.Value);
        if (fromDate.HasValue) query = query.Where(item => item.PeriodEnd >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(item => item.PeriodStart <= toDate.Value);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(item => item.PeriodStart).ThenBy(item => item.Student.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<AllowanceDto>(rows.Select(item => ToDto(item)).ToArray(), total, page, pageSize);
    }

    public async Task<AllowanceDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Allowances.AsNoTracking().Include(allowance => allowance.Student)
            .Include(allowance => allowance.Payments).SingleOrDefaultAsync(allowance => allowance.Id == id, cancellationToken);
        return item == null ? throw new NotFoundException("Allowance was not found.") : ToDto(item, includePayments: true);
    }

    public async Task<AllowanceDto> CreateAsync(int studentId, AllowanceRequestDto request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        if (!await _db.Students.AnyAsync(student => student.Id == studentId, cancellationToken))
            throw new NotFoundException("Student was not found.");
        var now = DateTime.UtcNow;
        var allowance = new Allowance
        {
            StudentId = studentId,
            AllowanceType = request.AllowanceType.Trim(),
            Description = Clean(request.Description),
            Amount = request.Amount,
            PaidAmount = 0m,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            Status = AllowanceStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Allowances.Add(allowance);
        try { await _db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.GetBaseException() is MySqlException { Number: 1062 })
        { throw new ConflictException("An allowance for this student, type, and period already exists."); }
        return await GetAsync(allowance.Id, cancellationToken);
    }

    public async Task<AllowanceDto> UpdateAsync(int id, AllowanceRequestDto request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var allowance = await _db.Allowances.Include(item => item.Student)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Allowance was not found.");
        if (allowance.Status != AllowanceStatus.Active)
            throw new ConflictException("Cancelled allowances cannot be edited.");
        if (request.Amount < allowance.PaidAmount)
            throw new ConflictException("Allowance amount cannot be lower than the amount already paid.");
        allowance.AllowanceType = request.AllowanceType.Trim();
        allowance.Description = Clean(request.Description);
        allowance.Amount = request.Amount;
        allowance.PeriodStart = request.PeriodStart;
        allowance.PeriodEnd = request.PeriodEnd;
        allowance.UpdatedAt = DateTime.UtcNow;
        try { await _db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.GetBaseException() is MySqlException { Number: 1062 })
        { throw new ConflictException("An allowance for this student, type, and period already exists."); }
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task CancelAsync(int id, CancellationToken cancellationToken)
    {
        var allowance = await _db.Allowances.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Allowance was not found.");
        if (allowance.Status == AllowanceStatus.Cancelled) return;
        allowance.Status = AllowanceStatus.Cancelled;
        allowance.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AllowancePaymentDto> RecordPaymentAsync(int userId, int allowanceId, AllowancePaymentRequestDto request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0m || decimal.Round(request.Amount, 2) != request.Amount)
            throw new BadRequestException("Payment amount must be positive and have at most two decimal places.");
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var allowance = await _db.Allowances.Include(item => item.Student)
            .SingleOrDefaultAsync(item => item.Id == allowanceId, cancellationToken)
            ?? throw new NotFoundException("Allowance was not found.");
        if (allowance.Status != AllowanceStatus.Active)
            throw new ConflictException("Cancelled allowances cannot receive payments.");
        if (allowance.PaidAmount + request.Amount > allowance.Amount)
            throw new ConflictException("Payment exceeds the remaining allowance amount.");
        var now = DateTime.UtcNow;
        allowance.PaidAmount += request.Amount;
        allowance.UpdatedAt = now;
        var payment = new AllowancePayment
        {
            AllowanceId = allowance.Id,
            Amount = request.Amount,
            PaidAt = now,
            RecordedByUserId = userId,
            Reference = Clean(request.Reference),
            CreatedAt = now
        };
        _db.AllowancePayments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToPaymentDto(payment, allowance);
    }

    public async Task<AllowanceHistoryResponseDto> GetMineAsync(
        int userId, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize, CancellationToken cancellationToken)
    {
        AttendanceService.ValidateDateRange(fromDate, toDate);
        var student = await _db.Students.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("No student profile is linked to this account.");
        var query = _db.AllowancePayments.AsNoTracking().Include(payment => payment.Allowance)
            .Where(payment => payment.Allowance.StudentId == student.Id);
        if (fromDate.HasValue) query = query.Where(payment => payment.PaidAt >= VietnamTime.ToUtcStartOfDay(fromDate.Value));
        if (toDate.HasValue) query = query.Where(payment => payment.PaidAt < VietnamTime.ToUtcStartOfDay(toDate.Value.AddDays(1)));
        var total = await query.CountAsync(cancellationToken);
        var sum = await query.SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;
        var rows = await query.OrderByDescending(payment => payment.PaidAt).ThenByDescending(payment => payment.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new AllowanceHistoryResponseDto
        {
            Items = rows.Select(payment => ToPaymentDto(payment, payment.Allowance)).ToArray(),
            TotalItems = total,
            PageNumber = page,
            PageSize = pageSize,
            TotalReceived = sum
        };
    }

    private static void ValidateRequest(AllowanceRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.AllowanceType) || request.AllowanceType.Trim().Length > 100)
            throw new BadRequestException("Allowance type is required and must not exceed 100 characters.");
        if (request.Amount <= 0m || decimal.Round(request.Amount, 2) != request.Amount)
            throw new BadRequestException("Amount must be positive and have at most two decimal places.");
        if (request.PeriodStart == default || request.PeriodEnd < request.PeriodStart)
            throw new BadRequestException("Allowance period is invalid.");
    }

    private static AllowanceDto ToDto(Allowance item, bool includePayments = false) => new()
    {
        Id = item.Id,
        StudentId = item.StudentId,
        StudentCode = item.Student.StudentCode,
        StudentName = item.Student.FullName,
        AllowanceType = item.AllowanceType,
        Description = item.Description,
        Amount = item.Amount,
        PaidAmount = item.PaidAmount,
        PeriodStart = item.PeriodStart,
        PeriodEnd = item.PeriodEnd,
        Status = item.Status,
        CreatedAtUtc = DateTime.SpecifyKind(item.CreatedAt, DateTimeKind.Utc),
        Payments = includePayments ? item.Payments.OrderByDescending(payment => payment.PaidAt)
            .Select(payment => ToPaymentDto(payment, item)).ToArray() : Array.Empty<AllowancePaymentDto>()
    };

    private static AllowancePaymentDto ToPaymentDto(AllowancePayment payment, Allowance allowance) => new()
    {
        Id = payment.Id,
        AllowanceId = allowance.Id,
        AllowanceType = allowance.AllowanceType,
        PeriodStart = allowance.PeriodStart,
        PeriodEnd = allowance.PeriodEnd,
        Amount = payment.Amount,
        PaidAtUtc = DateTime.SpecifyKind(payment.PaidAt, DateTimeKind.Utc),
        Reference = payment.Reference
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
