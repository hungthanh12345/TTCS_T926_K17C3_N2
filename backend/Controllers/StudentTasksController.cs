using System.Security.Claims;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Tasks;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/student/tasks")]
    [Authorize(Roles = "ROLE_STUDENT")]
    [Produces("application/json")]
    public sealed class StudentTasksController : ControllerBase
    {
        private readonly IMentorTaskService _taskService;

        public StudentTasksController(IMentorTaskService taskService) => _taskService = taskService;

        [HttpGet]
        public async Task<IActionResult> GetMyTasks()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            if (!int.TryParse(value, out var userId))
                return Unauthorized(ApiResponse.Fail("Invalid user identity."));

            var tasks = await _taskService.GetStudentTasksAsync(userId);
            return Ok(ApiResponse<IReadOnlyList<InternshipTaskResponseDto>>.Ok(tasks));
        }

        [HttpGet("{taskId:int}")]
        public async Task<IActionResult> GetMyTask(int taskId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse.Fail("Invalid user identity."));

            var task = await _taskService.GetStudentTaskAsync(userId, taskId);
            return Ok(ApiResponse<InternshipTaskResponseDto>.Ok(task));
        }

        [HttpPut("{taskId:int}/progress")]
        public async Task<IActionResult> UpdateMyTaskProgress(
            int taskId,
            [FromBody] UpdateStudentTaskProgressDto request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse.Fail("Invalid user identity."));

            var task = await _taskService.UpdateStudentTaskProgressAsync(userId, taskId, request);
            return Ok(ApiResponse<InternshipTaskResponseDto>.Ok(task, "Task progress updated successfully."));
        }

        private bool TryGetUserId(out int userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(value, out userId);
        }
    }
}
