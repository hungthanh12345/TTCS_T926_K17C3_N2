using InternshipManagementApi.DTOs;
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] string? university,
        [FromQuery] string? major,
        [FromQuery] int? programId,
        [FromQuery] string? reportType,
        CancellationToken cancellationToken)
    {
        if (!TryResolveReportKind(reportType, university, major, programId, out var reportKind, out var error))
            return BadRequest(InternshipManagementApi.Common.Models.ApiResponse.Fail(error));

        var report = await BuildReportAsync(university, major, programId, reportKind, cancellationToken);
        if (!report.HasData)
            return NoContent();

        var file = _exportService.CreateExcel(report);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] string? university,
        [FromQuery] string? major,
        [FromQuery] int? programId,
        [FromQuery] string? reportType,
        CancellationToken cancellationToken)
    {
        if (!TryResolveReportKind(reportType, university, major, programId, out var reportKind, out var error))
            return BadRequest(InternshipManagementApi.Common.Models.ApiResponse.Fail(error));

        var report = await BuildReportAsync(university, major, programId, reportKind, cancellationToken);
        if (!report.HasData)
            return NoContent();

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
        Sprint5ReportKind reportKind,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var mentors = reportKind.Includes(Sprint5ReportKind.MentorWorkload)
            ? await _mentorService.GetAllMentorsAsync()
            : Array.Empty<MentorResponseDto>();
        var demographics = reportKind.Includes(Sprint5ReportKind.StudentDemographics)
            ? await _demographicsService.GetAsync(cancellationToken)
            : new StudentDemographicsResponseDto();
        var completion = reportKind.Includes(Sprint5ReportKind.ProgramCompletion)
            ? await _programCompletionService.GetDashboardAsync(programId, cancellationToken)
            : new ProgramCompletionDashboardDto();
        var selectedProgram = programId.HasValue
            ? completion.Programs.Single(program => program.ProgramId == programId.Value)
            : null;

        return new Sprint5AggregateReportDto
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            ReportKind = reportKind,
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
            CompletionDataAvailable = reportKind.Includes(Sprint5ReportKind.ProgramCompletion),
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

    private static bool TryResolveReportKind(
        string? reportType,
        string? university,
        string? major,
        int? programId,
        out Sprint5ReportKind reportKind,
        out string error)
    {
        if (!Sprint5ReportKinds.TryParse(reportType, out reportKind))
        {
            error = "Loại báo cáo không hợp lệ.";
            return false;
        }

        if (programId.HasValue && !reportKind.Includes(Sprint5ReportKind.ProgramCompletion))
        {
            error = "Bộ lọc chương trình chỉ áp dụng cho báo cáo tỷ lệ hoàn thành.";
            return false;
        }

        if ((!string.IsNullOrWhiteSpace(university) || !string.IsNullOrWhiteSpace(major)) &&
            !reportKind.Includes(Sprint5ReportKind.StudentDemographics))
        {
            error = "Bộ lọc trường/ngành chỉ áp dụng cho báo cáo thống kê theo trường/ngành.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
