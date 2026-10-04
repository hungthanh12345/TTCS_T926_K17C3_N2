using System.Security.Claims;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Evaluations;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/mentor/evaluations")]
    [Authorize(Roles = "ROLE_MENTOR")]
    [Produces("application/json")]
    public sealed class MentorEvaluationsController : ControllerBase
    {
        private readonly IInternshipEvaluationService _evaluations;

        public MentorEvaluationsController(IInternshipEvaluationService evaluations) => _evaluations = evaluations;

        [HttpGet]
        public async Task<IActionResult> GetAssignedStudents()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var students = await _evaluations.GetAssignedStudentsAsync(userId);
            return Ok(ApiResponse<IReadOnlyList<MentorEvaluationStudentDto>>.Ok(students));
        }

        [HttpGet("{evaluationId:int}")]
        public async Task<IActionResult> GetOne(int evaluationId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var evaluation = await _evaluations.GetAssignedEvaluationAsync(userId, evaluationId);
            return Ok(ApiResponse<InternshipEvaluationResponseDto>.Ok(evaluation));
        }

        [HttpPost("students/{studentId:int}")]
        [ProducesResponseType(typeof(ApiResponse<InternshipEvaluationResponseDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create(int studentId, [FromBody] InternshipEvaluationRequestDto request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var evaluation = await _evaluations.CreateAsync(userId, studentId, request);
            return CreatedAtAction(nameof(GetOne), new { evaluationId = evaluation.Id },
                ApiResponse<InternshipEvaluationResponseDto>.Created(evaluation, "Internship evaluation submitted."));
        }

        [HttpPut("{evaluationId:int}")]
        public async Task<IActionResult> Update(int evaluationId, [FromBody] InternshipEvaluationRequestDto request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var evaluation = await _evaluations.UpdateAsync(userId, evaluationId, request);
            return Ok(ApiResponse<InternshipEvaluationResponseDto>.Ok(evaluation, "Internship evaluation updated."));
        }

        private bool TryGetUserId(out int userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(value, out userId);
        }
    }
}
