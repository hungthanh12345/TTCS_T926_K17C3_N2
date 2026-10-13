using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Workforce;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace InternshipManagementApi.Services;

public interface IAttendanceService
{
    Task<AttendanceDto> CheckInAsync(int userId, AttendanceCheckInRequestDto request, CancellationToken cancellationToken);
    Task<AttendanceDto> CheckOutAsync(int userId, CancellationToken cancellationToken);
    Task<AttendanceDto?> GetTodayAsync(int userId, CancellationToken cancellationToken);
    Task<PagedResult<AttendanceDto>> GetHistoryAsync(int userId, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize, CancellationToken cancellationToken);
}

public sealed class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _db;

    public AttendanceService(AppDbContext db) => _db = db;

    public async Task<AttendanceDto> CheckInAsync(int userId, AttendanceCheckInRequestDto request, CancellationToken cancellationToken)
    {
        var student = await GetStudentAsync(userId, cancellationToken);
        var now = DateTime.UtcNow;
        var businessDate = VietnamTime.Today(now);
        if (await _db.Attendances.AnyAsync(item => item.StudentId == student.Id && item.Date == businessDate, cancellationToken))
            throw new ConflictException("You have already checked in for the Vietnam business day.");

        var attendance = new Attendance
        {
            StudentId = student.Id,
            Date = businessDate,
            CheckInTime = now,
            Status = AttendanceStatus.CheckedIn,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Attendances.Add(attendance);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            throw new ConflictException("You have already checked in for the Vietnam business day.");
        }

        attendance.Student = student;
        return ToDto(attendance);
    }

    public async Task<AttendanceDto> CheckOutAsync(int userId, CancellationToken cancellationToken)
    {
        var student = await GetStudentAsync(userId, cancellationToken);
        var businessDate = VietnamTime.Today(DateTime.UtcNow);
        var attendance = await _db.Attendances.AsNoTracking()
            .Where(item => item.StudentId == student.Id && item.Date == businessDate)
            .Select(item => new { item.Id, item.CheckInTime, item.CheckOutTime })
            .SingleOrDefaultAsync(cancellationToken);
        if (attendance == null)
            throw new NotFoundException("No attendance check-in exists for today.");
        if (attendance.CheckOutTime != null)
            throw new ConflictException("This attendance session has already been checked out.");

        var now = DateTime.UtcNow;
        if (now < attendance.CheckInTime)
            throw new ConflictException("Check-out cannot be earlier than check-in.");
        var minutes = checked((int)Math.Floor((now - attendance.CheckInTime).TotalMinutes));
        var updated = await _db.Attendances
            .Where(item => item.Id == attendance.Id && item.CheckOutTime == null)
            .ExecuteUpdateAsync(update => update
                .SetProperty(item => item.CheckOutTime, now)
                .SetProperty(item => item.DurationMinutes, minutes)
                .SetProperty(item => item.Status, AttendanceStatus.Completed)
                .SetProperty(item => item.UpdatedAt, now), cancellationToken);
        if (updated != 1)
            throw new ConflictException("This attendance session has already been checked out.");

        var result = await _db.Attendances.AsNoTracking().Include(item => item.Student)
            .SingleAsync(item => item.Id == attendance.Id, cancellationToken);
        return ToDto(result);
    }

    public async Task<AttendanceDto?> GetTodayAsync(int userId, CancellationToken cancellationToken)
    {
        var student = await GetStudentAsync(userId, cancellationToken);
        var businessDate = VietnamTime.Today(DateTime.UtcNow);
        var attendance = await _db.Attendances.AsNoTracking().Include(item => item.Student)
            .SingleOrDefaultAsync(item => item.StudentId == student.Id && item.Date == businessDate, cancellationToken);
        return attendance == null ? null : ToDto(attendance);
    }

    public async Task<PagedResult<AttendanceDto>> GetHistoryAsync(
        int userId, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize, CancellationToken cancellationToken)
    {
        ValidateDateRange(fromDate, toDate);
        var student = await GetStudentAsync(userId, cancellationToken);
        var query = _db.Attendances.AsNoTracking().Include(item => item.Student)
            .Where(item => item.StudentId == student.Id);
        if (fromDate.HasValue) query = query.Where(item => item.Date >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(item => item.Date <= toDate.Value);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(item => item.Date).ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<AttendanceDto>(rows.Select(ToDto).ToArray(), total, page, pageSize);
    }

    internal static void ValidateDateRange(DateOnly? fromDate, DateOnly? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
            throw new BadRequestException("fromDate must not be after toDate.");
    }

    internal static int NormalizePage(int page) => Math.Max(1, page);
    internal static int NormalizePageSize(int size) => Math.Clamp(size, 1, 100);

    private async Task<Student> GetStudentAsync(int userId, CancellationToken cancellationToken)
    {
        return await _db.Students.SingleOrDefaultAsync(student => student.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("No student profile is linked to this account.");
    }

    internal static AttendanceDto ToDto(Attendance item) => new()
    {
        Id = item.Id,
        StudentId = item.StudentId,
        StudentCode = item.Student.StudentCode,
        StudentName = item.Student.FullName,
        Date = item.Date,
        CheckInTimeUtc = DateTime.SpecifyKind(item.CheckInTime, DateTimeKind.Utc),
        CheckOutTimeUtc = item.CheckOutTime.HasValue ? DateTime.SpecifyKind(item.CheckOutTime.Value, DateTimeKind.Utc) : null,
        DurationMinutes = item.DurationMinutes,
        Status = item.Status,
        Notes = item.Notes
    };

    private static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.GetBaseException() is MySqlException { Number: 1062 };
}
