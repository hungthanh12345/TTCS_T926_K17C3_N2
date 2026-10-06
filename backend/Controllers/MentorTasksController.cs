using System.Security.Claims;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Tasks;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/mentor")]
    [Authorize(Roles = "ROLE_MENTOR")]
    [Produces("application/json")]
    public sealed class MentorTasksController : ControllerBase
    {
        private readonly IMentorTaskService _taskService;

        public MentorTasksController(IMentorTaskService taskService) => _taskService = taskService;

        [HttpGet("students")]
        public async Task<IActionResult> GetAssignedStudents()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var students = await _taskService.GetAssignedStudentsAsync(userId);
            return Ok(ApiResponse<IReadOnlyList<MentorStudentSummaryDto>>.Ok(students));
        }

        [HttpGet("tasks")]
        public async Task<IActionResult> GetTasks()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var tasks = await _taskService.GetMentorTasksAsync(userId);
            return Ok(ApiResponse<IReadOnlyList<InternshipTaskResponseDto>>.Ok(tasks));
        }

        [HttpPost("tasks")]
        [ProducesResponseType(typeof(ApiResponse<InternshipTaskResponseDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateTask([FromBody] CreateMentorTaskRequestDto request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var task = await _taskService.CreateTaskAsync(userId, request);
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<InternshipTaskResponseDto>.Created(task, "Task assigned successfully."));
        }

        [HttpGet("tasks/{taskId:int}")]
        public async Task<IActionResult> GetTask(int taskId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var task = await _taskService.GetMentorTaskAsync(userId, taskId);
            return Ok(ApiResponse<InternshipTaskResponseDto>.Ok(task));
        }

        [HttpPut("tasks/{taskId:int}")]
        public async Task<IActionResult> UpdateTask(int taskId, [FromBody] UpdateMentorTaskRequestDto request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var task = await _taskService.UpdateTaskAsync(userId, taskId, request);
            return Ok(ApiResponse<InternshipTaskResponseDto>.Ok(task, "Task updated successfully."));
        }

        [HttpDelete("tasks/{taskId:int}")]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            await _taskService.DeleteTaskAsync(userId, taskId);
            return Ok(ApiResponse.Ok("Task deleted successfully."));
        }

        private bool TryGetUserId(out int userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(value, out userId);
        }
    }
}
