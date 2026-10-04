using System.Security.Claims;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Student;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/student/schedule")]
    [Authorize(Roles = "ROLE_STUDENT")]
    [Produces("application/json")]
    public sealed class StudentScheduleController : ControllerBase
    {
        private readonly IStudentScheduleService _scheduleService;

        public StudentScheduleController(IStudentScheduleService scheduleService) => _scheduleService = scheduleService;

        [HttpGet]
        public async Task<IActionResult> GetMySchedule(CancellationToken cancellationToken)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            if (!int.TryParse(value, out var userId))
                return Unauthorized(ApiResponse.Fail("Invalid user identity."));

            var events = await _scheduleService.GetMyScheduleAsync(userId, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<StudentScheduleEventDto>>.Ok(events));
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetMyScheduleOverview(CancellationToken cancellationToken)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            if (!int.TryParse(value, out var userId))
                return Unauthorized(ApiResponse.Fail("Invalid user identity."));

            var schedule = await _scheduleService.GetMyScheduleOverviewAsync(userId, cancellationToken);
            return Ok(ApiResponse<StudentScheduleResponseDto>.Ok(schedule));
        }
    }
}
