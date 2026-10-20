using System.Data;
using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Workforce;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace InternshipManagementApi.Services;

public interface ILeaveRequestService
{
    Task<LeaveRequestDto> CreateAsync(int userId, LeaveRequestCreateDto request, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeaveRequestDto>> GetMineAsync(int userId, CancellationToken cancellationToken);
    Task<PagedResult<LeaveRequestDto>> GetAllAsync(string? status, int? studentId, int page, int pageSize, CancellationToken cancellationToken);
    Task<LeaveRequestDto> ReviewAsync(int userId, int requestId, LeaveRequestReviewDto request, CancellationToken cancellationToken);
}

public sealed class LeaveRequestService : ILeaveRequestService
{
    private readonly AppDbContext _db;
    public LeaveRequestService(AppDbContext db) => _db = db;

    public async Task<LeaveRequestDto> CreateAsync(int userId, LeaveRequestCreateDto request, CancellationToken cancellationToken)
    {
        var reason = request.Reason.Trim();
        if (request.StartDate == default || request.EndDate == default || request.EndDate < request.StartDate)
            throw new BadRequestException("A valid leave date range is required.");
        if (reason.Length < 2 || reason.Length > 2000)
            throw new BadRequestException("Reason must contain 2 to 2000 characters.");

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var student = await _db.Students.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
                ?? throw new NotFoundException("No student profile is linked to this account.");

            var overlapping = await _db.LeaveRequests.AnyAsync(item => item.StudentId == student.Id &&
                (item.Status == LeaveRequestStatus.Pending || item.Status == LeaveRequestStatus.Approved) &&
                item.StartDate <= request.EndDate && item.EndDate >= request.StartDate, cancellationToken);
            if (overlapping)
                throw new ConflictException("This leave period overlaps a pending or approved request.");

            var now = DateTime.UtcNow;
            var leave = new LeaveRequest
            {
                StudentId = student.Id,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Reason = reason,
                Status = LeaveRequestStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.LeaveRequests.Add(leave);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            leave.Student = student;
            return ToDto(leave);
        }
        catch (Exception exception) when (IsDeadlock(exception))
        {
            var studentId = await _db.Students.AsNoTracking()
                .Where(student => student.UserId == userId)
                .Select(student => (int?)student.Id)
                .SingleOrDefaultAsync(cancellationToken);
            if (studentId.HasValue && await _db.LeaveRequests.AsNoTracking().AnyAsync(item =>
                    item.StudentId == studentId.Value &&
                    (item.Status == LeaveRequestStatus.Pending || item.Status == LeaveRequestStatus.Approved) &&
                    item.StartDate <= request.EndDate && item.EndDate >= request.StartDate,
                    cancellationToken))
                throw new ConflictException("This leave period overlaps a pending or approved request.");

            throw;
        }
    }
    public async Task<IReadOnlyList<LeaveRequestDto>> GetMineAsync(int userId, CancellationToken cancellationToken)
    {
        var student = await _db.Students.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("No student profile is linked to this account.");
        var rows = await Query().Where(item => item.StudentId == student.Id)
            .OrderByDescending(item => item.CreatedAt).ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToArray();
    }

    public async Task<PagedResult<LeaveRequestDto>> GetAllAsync(
        string? status, int? studentId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = Query();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!new[] { LeaveRequestStatus.Pending, LeaveRequestStatus.Approved, LeaveRequestStatus.Rejected }.Contains(status, StringComparer.Ordinal))
                throw new BadRequestException("Status must be PENDING, APPROVED, or REJECTED.");
            query = query.Where(item => item.Status == status);
        }
        if (studentId.HasValue) query = query.Where(item => item.StudentId == studentId.Value);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(item => item.Status).ThenBy(item => item.StartDate).ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<LeaveRequestDto>(rows.Select(ToDto).ToArray(), total, page, pageSize);
    }

    public async Task<LeaveRequestDto> ReviewAsync(int userId, int requestId, LeaveRequestReviewDto request, CancellationToken cancellationToken)
    {
        var leave = await _db.LeaveRequests.Include(item => item.Student)
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken)
            ?? throw new NotFoundException("Leave request was not found.");
        if (leave.Status != LeaveRequestStatus.Pending)
            throw new ConflictException("Only pending leave requests can be reviewed.");
        if (request.Approve && await _db.LeaveRequests.AnyAsync(item => item.Id != leave.Id && item.StudentId == leave.StudentId &&
            (item.Status == LeaveRequestStatus.Pending || item.Status == LeaveRequestStatus.Approved) &&
            item.StartDate <= leave.EndDate && item.EndDate >= leave.StartDate, cancellationToken))
            throw new ConflictException("Another pending or approved leave request overlaps this period.");

        var now = DateTime.UtcNow;
        leave.Status = request.Approve ? LeaveRequestStatus.Approved : LeaveRequestStatus.Rejected;
        leave.ReviewedByUserId = userId;
        leave.ReviewedAt = now;
        leave.RejectionReason = request.Approve || string.IsNullOrWhiteSpace(request.RejectionReason)
            ? null : request.RejectionReason.Trim();
        leave.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(leave);
    }

    private static bool IsDeadlock(Exception exception) =>
        exception.GetBaseException() is MySqlException { Number: 1213 };
    private IQueryable<LeaveRequest> Query() => _db.LeaveRequests.AsNoTracking().Include(item => item.Student);

    private static LeaveRequestDto ToDto(LeaveRequest item) => new()
    {
        Id = item.Id,
        StudentId = item.StudentId,
        StudentCode = item.Student.StudentCode,
        StudentName = item.Student.FullName,
        StartDate = item.StartDate,
        EndDate = item.EndDate,
        Reason = item.Reason,
        Status = item.Status,
        ReviewedByUserId = item.ReviewedByUserId,
        ReviewedAtUtc = item.ReviewedAt.HasValue ? DateTime.SpecifyKind(item.ReviewedAt.Value, DateTimeKind.Utc) : null,
        RejectionReason = item.RejectionReason,
        CreatedAtUtc = DateTime.SpecifyKind(item.CreatedAt, DateTimeKind.Utc)
    };
}
