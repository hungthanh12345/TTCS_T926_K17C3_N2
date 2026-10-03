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
    }
}
