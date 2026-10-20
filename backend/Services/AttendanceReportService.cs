using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Workforce;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Services;

public interface IAttendanceReportService
{
    Task<AttendanceReportResponseDto> GetAsync(DateOnly? fromDate, DateOnly? toDate, int? studentId, int page, int pageSize, CancellationToken cancellationToken);
}

public sealed class AttendanceReportService : IAttendanceReportService
{
    private readonly AppDbContext _db;
    public AttendanceReportService(AppDbContext db) => _db = db;

    public async Task<AttendanceReportResponseDto> GetAsync(
        DateOnly? fromDate, DateOnly? toDate, int? studentId, int page, int pageSize, CancellationToken cancellationToken)
    {
        AttendanceService.ValidateDateRange(fromDate, toDate);
        var query = _db.Attendances.AsNoTracking().Include(item => item.Student).AsQueryable();
        if (fromDate.HasValue) query = query.Where(item => item.Date >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(item => item.Date <= toDate.Value);
        if (studentId.HasValue) query = query.Where(item => item.StudentId == studentId.Value);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(item => item.Date).ThenBy(item => item.Student.FullName).ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var leaveQuery = _db.LeaveRequests.AsNoTracking().Where(item => item.Status == LeaveRequestStatus.Approved);
        if (fromDate.HasValue) leaveQuery = leaveQuery.Where(item => item.EndDate >= fromDate.Value);
        if (toDate.HasValue) leaveQuery = leaveQuery.Where(item => item.StartDate <= toDate.Value);
        if (studentId.HasValue) leaveQuery = leaveQuery.Where(item => item.StudentId == studentId.Value);
        var approvedLeaves = await leaveQuery.Select(item => new { item.StudentId, item.StartDate, item.EndDate })
            .ToListAsync(cancellationToken);
        var approvedLeaveDays = 0;
        foreach (var studentLeaves in approvedLeaves.GroupBy(item => item.StudentId))
        {
            var intervals = studentLeaves.Select(leave => (
                    Start: fromDate.HasValue && fromDate.Value > leave.StartDate ? fromDate.Value : leave.StartDate,
                    End: toDate.HasValue && toDate.Value < leave.EndDate ? toDate.Value : leave.EndDate))
                .Where(interval => interval.Start <= interval.End)
                .OrderBy(interval => interval.Start)
                .ToArray();
            if (intervals.Length == 0) continue;
            var currentStart = intervals[0].Start;
            var currentEnd = intervals[0].End;
            foreach (var interval in intervals.Skip(1))
            {
                var touches = interval.Start <= currentEnd ||
                    (currentEnd < DateOnly.MaxValue && interval.Start == currentEnd.AddDays(1));
                if (touches)
                {
                    if (interval.End > currentEnd) currentEnd = interval.End;
                    continue;
                }
                approvedLeaveDays += currentEnd.DayNumber - currentStart.DayNumber + 1;
                currentStart = interval.Start;
                currentEnd = interval.End;
            }
            approvedLeaveDays += currentEnd.DayNumber - currentStart.DayNumber + 1;
        }

        var unclassified = await query.CountAsync(item => item.Status != "PRESENT" && item.Status != "LATE" && item.Status != "ABSENT", cancellationToken);
        return new AttendanceReportResponseDto
        {
            Items = rows.Select(AttendanceService.ToDto).ToArray(),
            TotalItems = total,
            PageNumber = page,
            PageSize = pageSize,
            UnclassifiedSessions = unclassified,
            PresentDays = 0,
            LateDays = 0,
            AbsentDays = 0,
            ApprovedLeaveCalendarDays = approvedLeaveDays,
            AttendanceClassificationConfigured = false
        };
    }
}
