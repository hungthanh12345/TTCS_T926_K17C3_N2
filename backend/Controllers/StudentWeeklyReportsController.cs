using System.Security.Claims;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.WeeklyReports;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/student/weekly-reports")]
    [Authorize(Roles = "ROLE_STUDENT")]
    [Produces("application/json")]
    public sealed class StudentWeeklyReportsController : ControllerBase
    {
        private readonly IWeeklyReportService _reports;

        public StudentWeeklyReportsController(IWeeklyReportService reports) => _reports = reports;

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var reports = await _reports.GetOwnAsync(userId);
            return Ok(ApiResponse<IReadOnlyList<WeeklyReportResponseDto>>.Ok(reports));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<WeeklyReportResponseDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] WeeklyReportRequestDto request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var report = await _reports.CreateOwnAsync(userId, request);
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<WeeklyReportResponseDto>.Created(report, "Weekly report submitted."));
        }

        [HttpGet("{reportId:int}")]
        public async Task<IActionResult> GetOne(int reportId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var report = await _reports.GetOwnByIdAsync(userId, reportId);
            return Ok(ApiResponse<WeeklyReportResponseDto>.Ok(report));
        }

        [HttpPut("{reportId:int}")]
        public async Task<IActionResult> Update(int reportId, [FromBody] WeeklyReportRequestDto request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var report = await _reports.UpdateOwnAsync(userId, reportId, request);
            return Ok(ApiResponse<WeeklyReportResponseDto>.Ok(report, "Weekly report updated."));
        }

        private bool TryGetUserId(out int userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(value, out userId);
        }
    }
}
