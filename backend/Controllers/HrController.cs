using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Mentor;
using InternshipManagementApi.DTOs.Student;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/hr")]
    [Authorize(Roles = "ROLE_HR,ROLE_ADMIN")]
    [Produces("application/json")]
    public class HrController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IMentorService _mentorService;

        public HrController(IStudentService studentService, IMentorService mentorService)
        {
            _studentService = studentService;
            _mentorService = mentorService;
        }

        // ==========================================
        // STUDENT PROFILE MANAGEMENT
        // ==========================================

        /// <summary>
        /// Retrieve or search students with pagination and filtering.
        /// GET /api/hr/students or GET /api/hr/students/search
        /// Requires ROLE_HR or ROLE_ADMIN.
        /// </summary>
        [HttpGet("students")]
        [HttpGet("students/search")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<StudentResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetStudents([FromQuery] StudentSearchFilterDto filter)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("Validation failed.", ModelState));
            }

            var result = await _studentService.SearchStudentsAsync(filter);
            return Ok(ApiResponse<PagedResult<StudentResponseDto>>.Ok(result, "Students retrieved successfully."));
        }

        /// <summary>
        /// Add a new student profile to the students table.
        /// Requires ROLE_HR or ROLE_ADMIN.
        /// </summary>
        /// <param name="request">Student profile data</param>
        /// <returns>Created student profile</returns>
        [HttpPost("students")]
        [ProducesResponseType(typeof(ApiResponse<StudentResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("Validation failed.", ModelState));
            }

            var createdStudent = await _studentService.CreateStudentAsync(request);
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<StudentResponseDto>.Created(createdStudent, "Student profile created successfully."));
        }

        /// <summary>
        /// Get detailed profile of a specific student.
        /// Requires ROLE_HR or ROLE_ADMIN.
        /// </summary>
        /// <param name="id">Student ID</param>
        /// <returns>Detailed student profile</returns>
        [HttpGet("students/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<StudentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStudentById([FromRoute] int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            return Ok(ApiResponse<StudentResponseDto>.Ok(student, "Student profile retrieved successfully."));
        }

        /// <summary>
        /// Update student information.
        /// Requires ROLE_HR or ROLE_ADMIN.
        /// </summary>
        /// <param name="id">Student ID</param>
        /// <param name="request">Updated student information</param>
        /// <returns>Updated student profile</returns>
        [HttpPut("students/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<StudentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateStudent([FromRoute] int id, [FromBody] UpdateStudentRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("Validation failed.", ModelState));
            }

            var updatedStudent = await _studentService.UpdateStudentAsync(id, request);
            return Ok(ApiResponse<StudentResponseDto>.Ok(updatedStudent, "Student profile updated successfully."));
        }

        /// <summary>
        /// Delete an existing student profile.
        /// Requires ROLE_HR or ROLE_ADMIN.
        /// </summary>
        [HttpDelete("students/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteStudent([FromRoute] int id)
        {
            await _studentService.DeleteStudentAsync(id);
            return Ok(ApiResponse.Ok("Student profile deleted successfully."));
        }

        /// <summary>
        /// Assign or update a mentor_id for a specific student.
        /// Requires ROLE_HR or ROLE_ADMIN.
        /// </summary>
        /// <param name="studentId">Student ID</param>
        /// <param name="request">Mentor assignment request payload</param>
        /// <returns>Updated student profile with assigned mentor</returns>
        [HttpPut("students/{studentId:int}/assign-mentor")]
        [ProducesResponseType(typeof(ApiResponse<StudentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignMentor([FromRoute] int studentId, [FromBody] AssignMentorRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("Validation failed.", ModelState));
            }

            var updatedStudent = await _studentService.AssignMentorAsync(studentId, request);
            return Ok(ApiResponse<StudentResponseDto>.Ok(updatedStudent, "Mentor assignment updated successfully."));
        }

        // ==========================================
        // MENTOR MANAGEMENT
        // ==========================================

        /// <summary>
        /// Create a new mentor profile in the mentors table.
        /// Requires ROLE_HR or ROLE_ADMIN.
        /// </summary>
        /// <param name="request">Mentor profile creation data</param>
        /// <returns>Created mentor profile</returns>
        [HttpPost("mentors")]
        [ProducesResponseType(typeof(ApiResponse<MentorResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateMentor([FromBody] CreateMentorRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("Validation failed.", ModelState));
            }

            var createdMentor = await _mentorService.CreateMentorAsync(request);
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<MentorResponseDto>.Created(createdMentor, "Mentor profile created successfully."));
        }

        /// <summary>
        /// Retrieve all mentor profiles with assigned mentee counts.
        /// Requires ROLE_HR or ROLE_ADMIN.
        /// </summary>
        /// <returns>List of all mentor profiles</returns>
        [HttpGet("mentors")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<MentorResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllMentors()
        {
            var mentors = await _mentorService.GetAllMentorsAsync();
            return Ok(ApiResponse<IEnumerable<MentorResponseDto>>.Ok(mentors, "Mentor profiles retrieved successfully."));
        }

        /// <summary>
        /// Delete an existing mentor profile.
        /// Requires ROLE_HR or ROLE_ADMIN.
        /// </summary>
        [HttpDelete("mentors/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMentor([FromRoute] int id)
        {
            await _mentorService.DeleteMentorAsync(id);
            return Ok(ApiResponse.Ok("Mentor profile deleted successfully."));
        }
    }
}
