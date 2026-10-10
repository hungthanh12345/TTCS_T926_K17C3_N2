using InternshipManagementApi.DTOs.Hr;
using InternshipManagementApi.Data;
using InternshipManagementApi.Common;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Services
{
    public class AttendanceReportService : IAttendanceReportService
    {
        private readonly AppDbContext _db;

        public AttendanceReportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<PaginatedResult<AttendanceSummaryDto>>> GetAttendanceSummaryAsync(AttendanceReportRequestDto request)
        {
            if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate.Value > request.EndDate.Value)
            {
                return ApiResponse<PaginatedResult<AttendanceSummaryDto>>.ErrorResponse("Start date cannot be greater than end date.");
            }

            var query = _db.Students.AsQueryable();

            if (request.StudentId.HasValue)
            {
                query = query.Where(s => s.Id == request.StudentId.Value);
            }

            var totalCount = await query.CountAsync();
            var students = await query
                .OrderBy(s => s.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var studentIds = students.Select(s => s.Id).ToList();

            var attendancesQuery = _db.Attendances.Where(a => studentIds.Contains(a.StudentId));
            var leavesQuery = _db.LeaveRequests.Where(l => studentIds.Contains(l.StudentId) && l.Status == "APPROVED");

            if (request.StartDate.HasValue)
            {
                attendancesQuery = attendancesQuery.Where(a => a.Date >= request.StartDate.Value);
                leavesQuery = leavesQuery.Where(l => l.EndDate >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                attendancesQuery = attendancesQuery.Where(a => a.Date <= request.EndDate.Value);
                leavesQuery = leavesQuery.Where(l => l.StartDate <= request.EndDate.Value);
            }

            var attendances = await attendancesQuery.ToListAsync();
            var leaves = await leavesQuery.ToListAsync();

            var summary = new List<AttendanceSummaryDto>();

            foreach (var student in students)
            {
                var studentAttendances = attendances.Where(a => a.StudentId == student.Id).ToList();
                var studentLeaves = leaves.Where(l => l.StudentId == student.Id).ToList();

                int leaveDays = 0;
                foreach (var leave in studentLeaves)
                {
                    var start = request.StartDate.HasValue && request.StartDate.Value > leave.StartDate ? request.StartDate.Value : leave.StartDate;
                    var end = request.EndDate.HasValue && request.EndDate.Value < leave.EndDate ? request.EndDate.Value : leave.EndDate;
                    
                    if (start <= end)
                    {
                        leaveDays += end.DayNumber - start.DayNumber + 1;
                    }
                }

                summary.Add(new AttendanceSummaryDto
                {
                    StudentId = student.Id,
                    StudentCode = student.StudentCode,
                    FullName = student.FullName,
                    TotalPresent = studentAttendances.Count(a => a.Status == "PRESENT"),
                    TotalLate = studentAttendances.Count(a => a.Status == "LATE"),
                    TotalAbsent = studentAttendances.Count(a => a.Status == "ABSENT"),
                    TotalLeaveApproved = leaveDays
                });
            }

            var result = new PaginatedResult<AttendanceSummaryDto>
            {
                Items = summary,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return ApiResponse<PaginatedResult<AttendanceSummaryDto>>.SuccessResponse(result);
        }
    }
}
