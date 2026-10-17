using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Workforce;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Services;

public interface ISupportRequestService
{
    Task<SupportRequestDto> CreateAsync(int userId, SupportRequestCreateDto request, CancellationToken cancellationToken);
    Task<IReadOnlyList<SupportRequestDto>> GetMineAsync(int userId, CancellationToken cancellationToken);
    Task<SupportRequestDto> GetMineByIdAsync(int userId, int id, CancellationToken cancellationToken);
    Task<PagedResult<SupportRequestDto>> GetAllAsync(string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<SupportRequestDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<SupportRequestDto> UpdateAsync(int handlerUserId, int id, SupportRequestUpdateDto request, CancellationToken cancellationToken);
}

public sealed class SupportRequestService : ISupportRequestService
{
    private readonly AppDbContext _db;
    public SupportRequestService(AppDbContext db) => _db = db;

    public async Task<SupportRequestDto> CreateAsync(int userId, SupportRequestCreateDto request, CancellationToken cancellationToken)
    {
        var student = await _db.Students.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("No student profile is linked to this account.");
        var now = DateTime.UtcNow;
        var item = new SupportRequest
        {
            StudentId = student.Id,
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            Status = SupportRequestStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.SupportRequests.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        item.Student = student;
        return ToDto(item);
    }

    public async Task<IReadOnlyList<SupportRequestDto>> GetMineAsync(int userId, CancellationToken cancellationToken)
    {
        var student = await _db.Students.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("No student profile is linked to this account.");
        var rows = await Query().Where(item => item.StudentId == student.Id)
            .OrderByDescending(item => item.CreatedAt).ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToArray();
    }

    public async Task<SupportRequestDto> GetMineByIdAsync(int userId, int id, CancellationToken cancellationToken)
    {
        var student = await _db.Students.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("No student profile is linked to this account.");
        var item = await Query().SingleOrDefaultAsync(request => request.Id == id && request.StudentId == student.Id, cancellationToken);
        return item == null ? throw new NotFoundException("Support request was not found.") : ToDto(item);
    }

    public async Task<PagedResult<SupportRequestDto>> GetAllAsync(string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = Query();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!new[] { SupportRequestStatus.Pending, SupportRequestStatus.InProgress, SupportRequestStatus.Resolved }.Contains(status, StringComparer.Ordinal))
                throw new BadRequestException("Status must be PENDING, IN_PROGRESS, or RESOLVED.");
            query = query.Where(item => item.Status == status);
        }
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(item => item.Status).ThenBy(item => item.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<SupportRequestDto>(rows.Select(ToDto).ToArray(), total, page, pageSize);
    }

    public async Task<SupportRequestDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var item = await Query().SingleOrDefaultAsync(request => request.Id == id, cancellationToken);
        return item == null ? throw new NotFoundException("Support request was not found.") : ToDto(item);
    }

    public async Task<SupportRequestDto> UpdateAsync(int handlerUserId, int id, SupportRequestUpdateDto request, CancellationToken cancellationToken)
    {
        var item = await _db.SupportRequests.Include(record => record.Student)
            .SingleOrDefaultAsync(record => record.Id == id, cancellationToken)
            ?? throw new NotFoundException("Support request was not found.");
        if (item.Status == request.Status)
        {
            if (item.Status != SupportRequestStatus.InProgress || string.IsNullOrWhiteSpace(request.Response))
                return ToDto(item);

            var responseText = request.Response.Trim();
            if (string.Equals(item.Response, responseText, StringComparison.Ordinal))
                return ToDto(item);

            var responseUpdatedAt = DateTime.UtcNow;
            var responseUpdated = await _db.SupportRequests
                .Where(record => record.Id == id &&
                                 record.Status == SupportRequestStatus.InProgress &&
                                 record.Response == item.Response)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(record => record.Response, responseText)
                    .SetProperty(record => record.HandledByUserId, handlerUserId)
                    .SetProperty(record => record.HandledAt, responseUpdatedAt)
                    .SetProperty(record => record.UpdatedAt, responseUpdatedAt), cancellationToken);
            if (responseUpdated != 1)
                throw new ConflictException("The support request changed concurrently. Reload and try again.");

            return await GetByIdAsync(id, cancellationToken);
        }
        var allowed = item.Status == SupportRequestStatus.Pending && request.Status == SupportRequestStatus.InProgress ||
                      item.Status == SupportRequestStatus.InProgress && request.Status == SupportRequestStatus.Resolved;
        if (!allowed)
            throw new ConflictException("Support requests can only move from PENDING to IN_PROGRESS and from IN_PROGRESS to RESOLVED.");
        var expectedStatus = item.Status;
        var nextResponse = string.IsNullOrWhiteSpace(request.Response) ? item.Response : request.Response.Trim();
        var now = DateTime.UtcNow;
        var updated = await _db.SupportRequests
            .Where(record => record.Id == id && record.Status == expectedStatus)
            .ExecuteUpdateAsync(update => update
                .SetProperty(record => record.Status, request.Status)
                .SetProperty(record => record.Response, nextResponse)
                .SetProperty(record => record.HandledByUserId, handlerUserId)
                .SetProperty(record => record.HandledAt, now)
                .SetProperty(record => record.UpdatedAt, now), cancellationToken);
        if (updated != 1)
            throw new ConflictException("The support request changed concurrently. Reload and try again.");
        return await GetByIdAsync(id, cancellationToken);
    }

    private IQueryable<SupportRequest> Query() => _db.SupportRequests.AsNoTracking().Include(item => item.Student);

    private static SupportRequestDto ToDto(SupportRequest item) => new()
    {
        Id = item.Id,
        StudentId = item.StudentId,
        StudentCode = item.Student.StudentCode,
        StudentName = item.Student.FullName,
        Subject = item.Subject,
        Description = item.Description,
        Category = item.Category,
        Status = item.Status,
        Response = item.Response,
        HandledByUserId = item.HandledByUserId,
        HandledAtUtc = item.HandledAt.HasValue ? DateTime.SpecifyKind(item.HandledAt.Value, DateTimeKind.Utc) : null,
        CreatedAtUtc = DateTime.SpecifyKind(item.CreatedAt, DateTimeKind.Utc)
    };
}
