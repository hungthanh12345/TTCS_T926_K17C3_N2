using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Auth;
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

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
    }
}
