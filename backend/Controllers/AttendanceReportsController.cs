using InternshipManagementApi.DTOs.Hr;
using InternshipManagementApi.Services;
using InternshipManagementApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ROLE_HR")]
    public class AttendanceReportsController : ControllerBase
    {
        private readonly IAttendanceReportService _attendanceReportService;

        public AttendanceReportsController(IAttendanceReportService attendanceReportService)
        {
            _attendanceReportService = attendanceReportService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PaginatedResult<AttendanceSummaryDto>>>> GetSummary([FromQuery] AttendanceReportRequestDto request)
        {
            var response = await _attendanceReportService.GetAttendanceSummaryAsync(request);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }
    }
}
