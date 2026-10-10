using InternshipManagementApi.DTOs.Hr;
using InternshipManagementApi.Common;

namespace InternshipManagementApi.Services
{
    public interface IAttendanceReportService
    {
        Task<ApiResponse<PaginatedResult<AttendanceSummaryDto>>> GetAttendanceSummaryAsync(AttendanceReportRequestDto request);
    }
}
