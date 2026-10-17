using InternshipManagementApi.DTOs.Mentor;
using InternshipManagementApi.DTOs.Reports;
using InternshipManagementApi.Services;
using InternshipManagementApi.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers;

[ApiController]
[Route("api/hr/reports/export")]
[Authorize(Roles = "ROLE_HR")]
public sealed class HrReportExportController : ControllerBase
{
    private readonly IMentorService _mentorService;
    private readonly IStudentDemographicsService _demographicsService;
    private readonly IProgramCompletionService _programCompletionService;
    private readonly ISprint5ReportExportService _exportService;

    public HrReportExportController(
        IMentorService mentorService,
        IStudentDemographicsService demographicsService,
        IProgramCompletionService programCompletionService,
        ISprint5ReportExportService exportService)
    {
        _mentorService = mentorService;
        _demographicsService = demographicsService;
        _programCompletionService = programCompletionService;
        _exportService = exportService;
    }

    [HttpGet("excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] string? university,
        [FromQuery] string? major,
        [FromQuery] int? programId,
        CancellationToken cancellationToken)
    {
        var report = await BuildReportAsync(university, major, programId, cancellationToken);
        var file = _exportService.CreateExcel(report);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] string? university,
        [FromQuery] string? major,
        [FromQuery] int? programId,
        CancellationToken cancellationToken)
    {
        var report = await BuildReportAsync(university, major, programId, cancellationToken);
        try
        {
            var file = _exportService.CreatePdf(report);
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (PdfExportLicenseNotConfiguredException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                InternshipManagementApi.Common.Models.ApiResponse.Fail(
                    "Xuất PDF đang tạm tắt cho đến khi Owner xác nhận cấu hình giấy phép QuestPDF."));
        }
    }

    private async Task<Sprint5AggregateReportDto> BuildReportAsync(
        string? university,
        string? major,
        int? programId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var mentors = await _mentorService.GetAllMentorsAsync();
        var demographics = await _demographicsService.GetAsync(cancellationToken);
        var completion = await _programCompletionService.GetDashboardAsync(programId, cancellationToken);
        var selectedProgram = programId.HasValue
            ? completion.Programs.Single(program => program.ProgramId == programId.Value)
            : null;

        return new Sprint5AggregateReportDto
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            ProgramFilter = selectedProgram?.ProgramName,
            MentorWorkloads = mentors.Select(mentor => new MentorWorkloadReportRowDto
            {
                MentorName = mentor.FullName,
                Department = mentor.Department,
                AssignedStudentCount = mentor.AssignedStudentsCount
            }).ToArray(),
            UniversityFilter = string.IsNullOrWhiteSpace(university) ? null : university.Trim(),
            MajorFilter = string.IsNullOrWhiteSpace(major) ? null : major.Trim(),
            Demographics = demographics.Items
                .Where(row =>
                    (string.IsNullOrWhiteSpace(university) || string.Equals(row.University, university.Trim(), StringComparison.Ordinal)) &&
                    (string.IsNullOrWhiteSpace(major) || string.Equals(row.Major, major.Trim(), StringComparison.Ordinal)))
                .ToArray(),
            CompletionDataAvailable = true,
            ProgramCompletions = completion.Programs
                .Where(program => !programId.HasValue || program.ProgramId == programId.Value)
                .Select(program => new ProgramCompletionReportRowDto
                {
                    ProgramName = program.ProgramName,
                    EligibleCount = program.EligibleCount,
                    CompletedCount = program.CompletedCount
                })
                .ToArray()
        };
    }
}
