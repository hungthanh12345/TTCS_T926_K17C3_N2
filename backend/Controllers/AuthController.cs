using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Auth;
using InternshipManagementApi.DTOs.StudentRegistration;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IStudentRegistrationService _registrationService;

        public AuthController(IAuthService authService, IStudentRegistrationService registrationService)
        {
            _authService = authService;
            _registrationService = registrationService;
        }

        /// <summary>
        /// Authenticate user credentials and return a signed JWT token containing userId, email, and role.
        /// </summary>
        /// <param name="request">Email and password login payload</param>
        /// <returns>JWT authentication token and user information</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("Validation failed.", ModelState));
            }

            var response = await _authService.LoginAsync(request);
            return Ok(ApiResponse<LoginResponseDto>.Ok(response, "Login successful."));
        }

        [HttpPost("register")]
        [Consumes("application/json")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<StudentRegistrationStatusResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RegisterStudent([FromBody] StudentRegistrationRequestDto request)
        {
            var response = await _registrationService.RegisterAsync(request);
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<StudentRegistrationStatusResponseDto>.Created(response, "Registration submitted for HR approval."));
        }

        [HttpPost("register-application")]
        [Consumes("multipart/form-data")]
        [AllowAnonymous]
        [RequestSizeLimit(20 * 1024 * 1024 + 512 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024 + 512 * 1024)]
        [ProducesResponseType(typeof(ApiResponse<StudentRegistrationStatusResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RegisterStudentWithApplication([FromForm] StudentRegistrationApplicationRequestDto request)
        {
            var response = await _registrationService.RegisterWithApplicationAsync(request);
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<StudentRegistrationStatusResponseDto>.Created(response, "Registration and application submitted for HR approval."));
        }

        [HttpPost("registration-status")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<StudentRegistrationStatusResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetOwnRegistrationStatus([FromBody] LoginRequestDto request)
        {
            var response = await _registrationService.GetOwnStatusAsync(request);
            return Ok(ApiResponse<StudentRegistrationStatusResponseDto>.Ok(response, "Registration status retrieved."));
        }
    }
}
