using System.Security.Claims;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.WeeklyReports;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/mentor/weekly-reports")]
    [Authorize(Roles = "ROLE_MENTOR")]
    [Produces("application/json")]
    public sealed class MentorWeeklyReportsController : ControllerBase
    {
        private readonly IWeeklyReportService _reports;
        private readonly IMentorFeedbackService _feedback;

        public MentorWeeklyReportsController(IWeeklyReportService reports, IMentorFeedbackService feedback)
        {
            _reports = reports;
            _feedback = feedback;
        }

        [HttpGet]
        public async Task<IActionResult> GetAssigned()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var reports = await _reports.GetAssignedToMentorAsync(userId);
            return Ok(ApiResponse<IReadOnlyList<WeeklyReportResponseDto>>.Ok(reports));
        }

        [HttpGet("{reportId:int}")]
        public async Task<IActionResult> GetOne(int reportId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var report = await _reports.GetAssignedToMentorByIdAsync(userId, reportId);
            return Ok(ApiResponse<WeeklyReportResponseDto>.Ok(report));
        }

        [HttpPost("{reportId:int}/feedback")]
        [ProducesResponseType(typeof(ApiResponse<MentorFeedbackResponseDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateFeedback(int reportId, [FromBody] MentorFeedbackRequestDto request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var feedback = await _feedback.CreateAsync(userId, reportId, request);
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<MentorFeedbackResponseDto>.Created(feedback, "Mentor feedback submitted."));
        }

        [HttpPut("{reportId:int}/feedback")]
        public async Task<IActionResult> UpdateFeedback(int reportId, [FromBody] MentorFeedbackRequestDto request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var feedback = await _feedback.UpdateAsync(userId, reportId, request);
            return Ok(ApiResponse<MentorFeedbackResponseDto>.Ok(feedback, "Mentor feedback updated."));
        }

        private bool TryGetUserId(out int userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(value, out userId);
        }
    }
}
