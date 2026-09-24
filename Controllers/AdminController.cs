using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Admin;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "ROLE_ADMIN")]
    [Produces("application/json")]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;

        public AdminController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Create a new user account (HR, Mentor, or Student) with BCrypt password hashing.
        /// Requires ROLE_ADMIN.
        /// </summary>
        /// <param name="request">User account creation parameters</param>
        /// <returns>Created user account details</returns>
        [HttpPost("users")]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("Validation failed.", ModelState));
            }

            var createdUser = await _userService.CreateUserAsync(request);
            return StatusCode(StatusCodes.Status201Created, 
                ApiResponse<UserResponseDto>.Created(createdUser, "User account created successfully."));
        }

        /// <summary>
        /// Fetch a list of all system users with their assigned roles.
        /// Requires ROLE_ADMIN.
        /// </summary>
        /// <returns>List of system users</returns>
        [HttpGet("users")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(ApiResponse<IEnumerable<UserResponseDto>>.Ok(users, "System users retrieved successfully."));
        }
    }
}
