using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Evaluations;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/hr/internship-summary")]
    [Authorize(Roles = "ROLE_HR,ROLE_ADMIN")]
    [Produces("application/json")]
    public sealed class HrInternshipSummaryController : ControllerBase
    {
        private readonly IHrInternshipSummaryService _summary;

        public HrInternshipSummaryController(IHrInternshipSummaryService summary) => _summary = summary;

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<HrInternshipSummaryResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary()
        {
            var summary = await _summary.GetSummaryAsync();
            return Ok(ApiResponse<HrInternshipSummaryResponseDto>.Ok(summary));
        }
    }
}
