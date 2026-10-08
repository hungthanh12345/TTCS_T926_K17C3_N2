using System.Security.Claims;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Evaluations;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/student/evaluations")]
    [Authorize(Roles = "ROLE_STUDENT")]
    [Produces("application/json")]
    public sealed class StudentEvaluationsController : ControllerBase
    {
        private readonly IInternshipEvaluationService _evaluations;

        public StudentEvaluationsController(IInternshipEvaluationService evaluations) => _evaluations = evaluations;

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var evaluations = await _evaluations.GetOwnAsync(userId);
            return Ok(ApiResponse<IReadOnlyList<InternshipEvaluationResponseDto>>.Ok(evaluations));
        }

        [HttpGet("{evaluationId:int}")]
        public async Task<IActionResult> GetOne(int evaluationId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
            var evaluation = await _evaluations.GetOwnByIdAsync(userId, evaluationId);
            return Ok(ApiResponse<InternshipEvaluationResponseDto>.Ok(evaluation));
        }

        private bool TryGetUserId(out int userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(value, out userId);
        }
    }
}
