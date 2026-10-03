using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.StudentRegistration;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/hr/student-registrations")]
    [Authorize(Roles = "ROLE_HR")]
    [Produces("application/json")]
    public sealed class StudentRegistrationsController : ControllerBase
    {
        private readonly IStudentRegistrationService _registrationService;

        public StudentRegistrationsController(IStudentRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StudentRegistrationReviewDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPending()
        {
            var registrations = await _registrationService.GetPendingAsync();
            return Ok(ApiResponse<IReadOnlyList<StudentRegistrationReviewDto>>.Ok(registrations));
        }

        [HttpGet("{studentId:int}")]
        [ProducesResponseType(typeof(ApiResponse<StudentRegistrationReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> GetPendingDetails(int studentId)
        {
            var registration = await _registrationService.GetPendingDetailsAsync(studentId);
            return Ok(ApiResponse<StudentRegistrationReviewDto>.Ok(registration));
        }

        [HttpPost("{studentId:int}/approve")]
        [ProducesResponseType(typeof(ApiResponse<StudentRegistrationReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Approve(int studentId)
        {
            var registration = await _registrationService.ApproveAsync(studentId);
            return Ok(ApiResponse<StudentRegistrationReviewDto>.Ok(registration, "Student registration approved."));
        }

        [HttpPost("{studentId:int}/reject")]
        [ProducesResponseType(typeof(ApiResponse<StudentRegistrationReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Reject(int studentId)
        {
            var registration = await _registrationService.RejectAsync(studentId);
            return Ok(ApiResponse<StudentRegistrationReviewDto>.Ok(registration, "Student registration rejected."));
        }
    }
}
