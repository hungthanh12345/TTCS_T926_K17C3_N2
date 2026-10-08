using System.Security.Claims;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Student;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/student/profile")]
    [Authorize(Roles = "ROLE_STUDENT")]
    [Produces("application/json")]
    public sealed class StudentProfileController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentProfileController(IStudentService studentService) => _studentService = studentService;

        [HttpGet]
        public async Task<IActionResult> GetMyProfile()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            if (!int.TryParse(value, out var userId))
                return Unauthorized(ApiResponse.Fail("Invalid user identity."));

            var student = await _studentService.GetStudentByUserIdAsync(userId);
            return Ok(ApiResponse<StudentResponseDto>.Ok(student));
        }
    }
}
