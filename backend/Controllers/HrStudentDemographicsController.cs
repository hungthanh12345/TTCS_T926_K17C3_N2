using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Reports;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers;

[ApiController]
[Route("api/hr/reports/student-demographics")]
[Authorize(Roles = "ROLE_HR")]
[Produces("application/json")]
public sealed class HrStudentDemographicsController : ControllerBase
{
    private readonly IStudentDemographicsService _service;

    public HrStudentDemographicsController(IStudentDemographicsService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<StudentDemographicsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(cancellationToken);
        return Ok(ApiResponse<StudentDemographicsResponseDto>.Ok(result));
    }
}