using System.IO.Compression;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Reports;
using InternshipManagementApi.Services;
using InternshipManagementApi.Services.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

var testConfiguration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Reports:QuestPdfLicense"] = "Community"
    })
    .Build();
var testEnvironment = new SmokeTestHostEnvironment();
var service = new Sprint5ReportExportService(testConfiguration, testEnvironment);
var newStudent = new Student();
Assert(newStudent.ApplicationStatus == StudentApplicationStatus.PendingApproval,
    "A new Student entity must default to pending approval, never implicit approval.");
Assert(StudentApplicationStatus.PendingApproval.Length <= 20, "PENDING_APPROVAL must fit varchar(20).");
Assert(StudentApplicationStatus.Approved.Length <= 20, "APPROVED must fit varchar(20).");
Assert(StudentApplicationStatus.Rejected.Length <= 20, "REJECTED must fit varchar(20).");

var recurrenceStart = new DateOnly(2026, 10, 13); // Tuesday; a partial first week ends Sunday.
foreach (var weeks in new[] { 1, 2, 4, 8, 12 })
{
    Assert(WorkScheduleRecurrence.TryResolve(recurrenceStart, WorkScheduleRecurrenceType.Weekly, weeks,
        out var finiteRecurrence, out var recurrenceError), recurrenceError ?? "Finite weekly recurrence should be valid.");
    var expectedEnd = recurrenceStart.AddDays(5 + (weeks - 1) * 7);
    Assert(finiteRecurrence!.EndDate == expectedEnd, $"A {weeks}-week recurrence must end on its final Sunday.");
    Assert(WorkScheduleRecurrence.IsScheduledOn(finiteRecurrence.StartDate, finiteRecurrence.EndDate,
        (int)DayOfWeek.Tuesday, recurrenceStart), "The schedule must include its start date when the weekday matches.");
    Assert(!WorkScheduleRecurrence.IsScheduledOn(finiteRecurrence.StartDate, finiteRecurrence.EndDate,
        (int)DayOfWeek.Monday, recurrenceStart), "A weekday before the effective start date must not be scheduled.");
    Assert(WorkScheduleRecurrence.IsScheduledOn(finiteRecurrence.StartDate, finiteRecurrence.EndDate,
        (int)DayOfWeek.Sunday, expectedEnd), "A finite recurrence must include its final Sunday.");
    Assert(!WorkScheduleRecurrence.IsEffectiveOn(finiteRecurrence.StartDate, finiteRecurrence.EndDate,
        expectedEnd.AddDays(1)), "A finite recurrence must not apply after its end date.");
}

Assert(WorkScheduleRecurrence.TryResolve(recurrenceStart, WorkScheduleRecurrenceType.None, null,
    out var oneWeekRecurrence, out _), "A non-repeating schedule should resolve to one effective week.");
Assert(oneWeekRecurrence!.Weeks == 1 && oneWeekRecurrence.EndDate == new DateOnly(2026, 10, 18),
    "A non-repeating schedule must end on Sunday of its starting week.");
Assert(WorkScheduleRecurrence.TryResolve(recurrenceStart, WorkScheduleRecurrenceType.Weekly, null,
    out var unlimitedRecurrence, out _), "An unlimited weekly recurrence should be valid.");
Assert(unlimitedRecurrence!.EndDate is null && WorkScheduleRecurrence.IsScheduledOn(
    unlimitedRecurrence.StartDate, unlimitedRecurrence.EndDate, (int)DayOfWeek.Tuesday, recurrenceStart.AddDays(70)),
    "An unlimited recurrence should remain effective on matching weekdays.");
Assert(!WorkScheduleRecurrence.TryResolve(null, WorkScheduleRecurrenceType.Weekly, null,
    out _, out _), "A recurrence must not be created without an explicit start date.");
Assert(!WorkScheduleRecurrence.TryResolve(recurrenceStart, WorkScheduleRecurrenceType.Weekly, 53,
    out _, out _), "A recurrence longer than 52 weeks must be rejected.");
Assert(!WorkScheduleRecurrence.TryResolve(new DateOnly(9999, 12, 31), WorkScheduleRecurrenceType.None, null,
    out _, out _), "An end date outside DateOnly's valid range must be rejected safely.");
Assert(WorkScheduleRecurrence.IsEffectiveOn(null, null, new DateOnly(2036, 10, 13)),
    "Legacy schedules with no recurrence metadata must retain their pre-existing unrestricted date range.");
var legacySchedule = new WorkSchedule();
Assert(legacySchedule.EffectiveStartDate is null && legacySchedule.RecurrenceType is null &&
       legacySchedule.RecurrenceWeeks is null && legacySchedule.EffectiveEndDate is null,
    "Existing legacy schedules must not be initialized as weekly recurrences automatically.");

using (var modelContext = new AppDbContextFactory().CreateDbContext(Array.Empty<string>()))
{
    var knownMigrations = modelContext.Database.GetMigrations().ToArray();
        Assert(knownMigrations.Contains("20261016040114_AddProgramParticipationAuditAndPendingApplicationDefault"),
            "EF must discover the reviewed US33 migration source.");
    Assert(knownMigrations.Contains("20261013100022_AddWorkScheduleRecurrence"),
        "EF must discover the restored US23 migration source.");
    Assert(!knownMigrations.Contains("20261008065402_AddRegistrationReceivedEmailTemplate"),
        "The missing email migration must remain visibly absent from source.");

    var designTimeModel = modelContext.GetService<IDesignTimeModel>().Model;
    var applicationStatus = designTimeModel.FindEntityType(typeof(Student))!
        .FindProperty(nameof(Student.ApplicationStatus))!;
    Assert(applicationStatus.IsNullable == false, "application_status must map as required.");
    Assert(applicationStatus.GetMaxLength() == 20, "application_status must map to length 20.");
    Assert(applicationStatus.GetDefaultValue() as string == StudentApplicationStatus.PendingApproval,
        "The current EF target model must reflect the reviewed post-migration default; the candidate baseline retains APPROVED.");
    Assert(applicationStatus.GetCollation() == "utf8mb4_unicode_ci", "EF metadata must reflect the verified column collation.");

    var workSchedule = designTimeModel.FindEntityType(typeof(WorkSchedule))!;
    Assert(workSchedule.FindProperty(nameof(WorkSchedule.EffectiveStartDate))?.GetColumnName() == "effective_start_date",
        "EffectiveStartDate must map to the deployed recurrence column.");
    Assert(workSchedule.FindProperty(nameof(WorkSchedule.RecurrenceType))?.GetMaxLength() == 20,
        "RecurrenceType must map to varchar(20).");
    Assert(workSchedule.FindProperty(nameof(WorkSchedule.RecurrenceWeeks))?.IsNullable == true,
        "RecurrenceWeeks must remain nullable for unlimited and legacy schedules.");
    Assert(workSchedule.FindProperty(nameof(WorkSchedule.EffectiveEndDate))?.GetColumnName() == "effective_end_date",
        "EffectiveEndDate must map to the deployed recurrence column.");
    var recurrenceConstraint = workSchedule.GetCheckConstraints()
        .Single(constraint => constraint.Name == "ck_work_schedules_recurrence");
    Assert(recurrenceConstraint.Sql!.Contains("`effective_start_date` IS NULL", StringComparison.Ordinal) &&
           recurrenceConstraint.Sql.Contains("'WEEKLY'", StringComparison.Ordinal) &&
           recurrenceConstraint.Sql.Contains("BETWEEN 1 AND 52", StringComparison.Ordinal),
        "The model check constraint must allow legacy and valid finite/unlimited recurrence states.");

    var auditStatusConstraint = designTimeModel.FindEntityType(typeof(StudentProgramAuditEvent))!
        .GetCheckConstraints()
        .Single(constraint => constraint.Name == "ck_student_program_audit_events_status");
    Assert(auditStatusConstraint.Sql!.Contains("`new_status` IS NOT NULL", StringComparison.Ordinal),
        "Participation status constraint must reject NULL new_status values.");
    var auditEntity = designTimeModel.FindEntityType(typeof(StudentProgramAuditEvent))!;
    Assert(auditEntity.GetTableName() == "student_program_audit_events", "US33 audit table mapping is missing.");
    Assert(auditEntity.GetForeignKeys().Count() == 3 &&
           auditEntity.GetForeignKeys().All(key => key.DeleteBehavior == DeleteBehavior.Restrict),
        "All audit foreign keys must remain RESTRICT.");
    Assert(auditEntity.GetIndexes().Count() == 3, "US33 must retain its three reviewed indexes.");
    Assert(auditEntity.GetCheckConstraints().Count() == 2, "US33 must retain its two reviewed CHECK constraints.");
    foreach (var propertyName in new[]
             {
                 nameof(StudentProgramAuditEvent.EventType),
                 nameof(StudentProgramAuditEvent.PreviousStatus),
                 nameof(StudentProgramAuditEvent.NewStatus),
                 nameof(StudentProgramAuditEvent.Reason)
             })
    {
        Assert(auditEntity.FindProperty(propertyName)?.GetCollation() == "utf8mb4_unicode_ci",
            $"Audit property {propertyName} must use utf8mb4_unicode_ci.");
    }

    var contractEntity = designTimeModel.FindEntityType(typeof(InternshipContract))!;
    Assert(contractEntity.GetForeignKeys().Single(key => key.PrincipalEntityType.ClrType == typeof(Student)).DeleteBehavior ==
           DeleteBehavior.Restrict,
        "The US09 Student foreign key must remain RESTRICT.");

}

var report = new Sprint5AggregateReportDto
{
    GeneratedAtUtc = new DateTimeOffset(2026, 10, 15, 8, 30, 0, TimeSpan.Zero),
    ProgramFilter = "Chương trình kiểm thử",
    UniversityFilter = "Đại học kiểm thử",
    MajorFilter = "Khoa học dữ liệu",
    FromDate = new DateOnly(2026, 1, 1),
    ToDate = new DateOnly(2026, 12, 31),
    MentorWorkloads =
    [
        new MentorWorkloadReportRowDto
        {
            MentorName = "Mentor Mẫu",
            Department = "Phòng Thực tập",
            AssignedStudentCount = 2
        }
    ],
    Demographics =
    [
        new StudentDemographicPairDto
        {
            University = "Chưa cập nhật",
            Major = "Kỹ thuật phần mềm",
            StudentCount = 3,
        },
        new StudentDemographicPairDto
        {
            University = "Chưa cập nhật",
            Major = "Khoa học dữ liệu",
            StudentCount = 4
        }
    ],
    CompletionDataAvailable = true,
    ProgramCompletions =
    [
        new ProgramCompletionReportRowDto
        {
            ProgramName = "Chương trình kiểm thử",
            EligibleCount = 17,
            CompletedCount = 4
        },
        new ProgramCompletionReportRowDto
        {
            ProgramName = "Chưa có lượt hợp lệ",
            EligibleCount = 0,
            CompletedCount = 0
        }
    ]
};

var excel = service.CreateExcel(report);
Assert(excel.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Unexpected Excel MIME type.");
Assert(excel.FileName.EndsWith(".xlsx", StringComparison.Ordinal), "Excel file name must use .xlsx.");
using (var workbook = SpreadsheetDocument.Open(new MemoryStream(excel.Content), false))
{
    var worksheet = workbook.WorkbookPart?.WorksheetParts.SingleOrDefault()?.Worksheet;
    Assert(worksheet is not null, "The workbook must contain one worksheet.");
    var content = worksheet!.InnerText;
    Assert(content.Contains("Báo cáo thống kê thực tập", StringComparison.Ordinal), "Excel title missing.");
    Assert(content.Contains("Chưa cập nhật", StringComparison.Ordinal), "Excel Vietnamese text missing.");
    Assert(content.Contains("Đại học kiểm thử", StringComparison.Ordinal) && content.Contains("Khoa học dữ liệu", StringComparison.Ordinal), "Excel filters missing.");
    Assert(content.Contains("7", StringComparison.Ordinal), "School totals must match the dashboard aggregation.");
    Assert(content.Contains("23.53", StringComparison.Ordinal), "Completion rate must be rounded to two decimal places in Excel.");
    Assert(content.Contains("Không có dữ liệu", StringComparison.Ordinal), "Zero denominator must not display a fabricated rate.");
}

var manyDemographics = report.Demographics.Concat(
    Enumerable.Range(1, 48).Select(index => new StudentDemographicPairDto
    {
        University = $"Đại học kiểm thử nhóm {index:00} " + string.Join(' ', Enumerable.Repeat("tên trường tiếng Việt dài", 3)),
        Major = $"Ngành kiểm thử nhóm {index:00} " + string.Join(' ', Enumerable.Repeat("chuyên ngành dữ liệu dài", 3)),
        StudentCount = index
    })).ToArray();
var manyMentors = Enumerable.Range(1, 24).Select(index => new MentorWorkloadReportRowDto
{
    MentorName = $"Mentor kiểm thử {index:00} " + string.Join(' ', Enumerable.Repeat("họ tên tiếng Việt dài", 2)),
    Department = $"Phòng ban {index:00}",
    AssignedStudentCount = index
}).ToArray();
var manyCompletions = Enumerable.Range(1, 24).Select(index => new ProgramCompletionReportRowDto
{
    ProgramName = $"Chương trình thử {index:00} " + string.Join(' ', Enumerable.Repeat("tên chương trình dài", 3)),
    EligibleCount = 20 + index,
    CompletedCount = index % 7
}).ToArray();
var stressReport = new Sprint5AggregateReportDto
{
    GeneratedAtUtc = report.GeneratedAtUtc,
    ProgramFilter = report.ProgramFilter,
    UniversityFilter = report.UniversityFilter,
    MajorFilter = report.MajorFilter,
    FromDate = report.FromDate,
    ToDate = report.ToDate,
    MentorWorkloads = manyMentors,
    Demographics = manyDemographics,
    CompletionDataAvailable = true,
    ProgramCompletions = manyCompletions
};

var stressExcel = service.CreateExcel(stressReport);
using (var workbook = SpreadsheetDocument.Open(new MemoryStream(stressExcel.Content), false))
{
    var worksheet = workbook.WorkbookPart!.WorksheetParts.Single().Worksheet!;
    Assert(worksheet.InnerText.Contains("Đại học kiểm thử nhóm 48", StringComparison.Ordinal),
        "Excel must preserve all demographic groups.");
    Assert(worksheet.InnerText.Contains("Mentor kiểm thử 24", StringComparison.Ordinal),
        "Excel must preserve the complete Mentor workload report.");
    Assert(worksheet.InnerText.Contains("Chương trình thử 24", StringComparison.Ordinal),
        "Excel must preserve the complete program completion report.");
    Assert(worksheet.Descendants<DocumentFormat.OpenXml.Spreadsheet.Row>().Count() > 130,
        "Excel should contain the expected multi-group rows.");
}

var pdf = service.CreatePdf(stressReport);
var emptyReport = new Sprint5AggregateReportDto
{
    GeneratedAtUtc = report.GeneratedAtUtc,
    CompletionDataAvailable = true
};
var emptyExcel = service.CreateExcel(emptyReport);
using (var workbook = SpreadsheetDocument.Open(new MemoryStream(emptyExcel.Content), false))
{
    var content = workbook.WorkbookPart!.WorksheetParts.Single().Worksheet!.InnerText;
    Assert(content.Contains("Không có dữ liệu", StringComparison.Ordinal),
        "Empty Excel reports must state that no data is available.");
}
var emptyPdf = service.CreatePdf(emptyReport);
Assert(emptyPdf.Content.Length > 1000 &&
       Encoding.ASCII.GetString(emptyPdf.Content, 0, 5) == "%PDF-",
    "An empty report must still produce a valid PDF document.");

var emptyPdfSmokeOutputPath = Environment.GetEnvironmentVariable("SPRINT5_PDF_EMPTY_SMOKE_OUTPUT_PATH");
if (!string.IsNullOrWhiteSpace(emptyPdfSmokeOutputPath))
{
    var fullEmptyPdfPath = Path.GetFullPath(emptyPdfSmokeOutputPath);
    var configuredTempRoot = Environment.GetEnvironmentVariable("TEMP");
    var tempRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredTempRoot) ? Path.GetTempPath() : configuredTempRoot) + Path.DirectorySeparatorChar;
    if (!fullEmptyPdfPath.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Empty PDF smoke output must be inside the system temporary directory.");
    File.WriteAllBytes(fullEmptyPdfPath, emptyPdf.Content);
    Console.WriteLine("PDF_EMPTY_TEXT_SMOKE_OUTPUT=" + fullEmptyPdfPath);
}
var pdfSmokeOutputPath = Environment.GetEnvironmentVariable("SPRINT5_PDF_SMOKE_OUTPUT_PATH");
if (!string.IsNullOrWhiteSpace(pdfSmokeOutputPath))
{
    var fullPdfSmokeOutputPath = Path.GetFullPath(pdfSmokeOutputPath);
    var configuredTempRoot = Environment.GetEnvironmentVariable("TEMP");
    var tempRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredTempRoot) ? Path.GetTempPath() : configuredTempRoot) + Path.DirectorySeparatorChar;
    if (!fullPdfSmokeOutputPath.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("PDF smoke output must be inside the system temporary directory.");
    }

    File.WriteAllBytes(fullPdfSmokeOutputPath, pdf.Content);
    Console.WriteLine("PDF_TEXT_SMOKE_OUTPUT=" + fullPdfSmokeOutputPath);
}
Assert(pdf.ContentType == "application/pdf", "Unexpected PDF MIME type.");
Assert(pdf.FileName.EndsWith(".pdf", StringComparison.Ordinal), "PDF file name must use .pdf.");
Assert(pdf.Content.Length > 1000, "PDF output is unexpectedly small.");
Assert(Encoding.ASCII.GetString(pdf.Content, 0, 5) == "%PDF-", "PDF header is invalid.");
Assert(Encoding.ASCII.GetString(pdf.Content).Contains("%%EOF", StringComparison.Ordinal), "PDF trailer is invalid.");

var unavailable = service.CreateExcel(new Sprint5AggregateReportDto
{
    GeneratedAtUtc = DateTimeOffset.UtcNow,
    CompletionDataAvailable = false,
    CompletionUnavailableReason = "Chưa xác minh trạng thái hủy."
});
using (var workbook = SpreadsheetDocument.Open(new MemoryStream(unavailable.Content), false))
{
    var content = workbook.WorkbookPart!.WorksheetParts.Single().Worksheet!.InnerText;
    Assert(content.Contains("Chưa có dữ liệu hoàn thành đã được xác minh.", StringComparison.Ordinal), "Unavailable completion data must be labeled explicitly.");
    Assert(content.Contains("Chưa xác minh trạng thái hủy.", StringComparison.Ordinal), "Unavailable reason missing.");
}

Assert(ProgramCompletionRate.CalculatePercent(0, 0) is null, "Zero denominator must return no rate.");
Assert(ProgramCompletionRate.CalculatePercent(17, 4) == 400m / 17m, "Completion formula is incorrect.");
AssertThrows<ArgumentOutOfRangeException>(() => ProgramCompletionRate.CalculatePercent(2, 3));
AssertThrows<PdfExportLicenseNotConfiguredException>(() =>
    new Sprint5ReportExportService(new ConfigurationBuilder().Build(), testEnvironment).CreatePdf(report));
AssertThrows<PdfExportLicenseNotConfiguredException>(() =>
    new Sprint5ReportExportService(testConfiguration, new SmokeTestHostEnvironment { EnvironmentName = "Production" }).CreatePdf(report));

Assert(ProgramCompletionRules.IsApplicationApproved(StudentApplicationStatus.Approved), "APPROVED application must pass regardless of account status or review timestamp.");
Assert(ProgramCompletionRules.IsApplicationApproved("approved"), "Approval comparison should tolerate case differences from a case-insensitive MySQL collation.");
Assert(!ProgramCompletionRules.IsApplicationApproved(StudentApplicationStatus.Rejected), "REJECTED application must not pass.");
Assert(!ProgramCompletionRules.IsApplicationApproved(StudentApplicationStatus.PendingApproval), "Pending application must not pass.");
Assert(!ProgramCompletionRules.IsApplicationApproved(null), "Missing application status must not pass.");
Assert(!ProgramCompletionRules.IsApplicationApproved(string.Empty), "Empty application status must not pass.");
Assert(ProgramCompletionRules.IsEligible(true, 3, ProgramParticipationStatus.Active), "Approved active participation should be eligible.");
Assert(ProgramCompletionRules.IsEligible(true, 3, ProgramParticipationStatus.Ended), "Approved ended participation should be eligible.");
Assert(!ProgramCompletionRules.IsEligible(true, 3, ProgramParticipationStatus.Unverified), "Unverified participation must not be eligible.");
Assert(!ProgramCompletionRules.IsEligible(true, 3, ProgramParticipationStatus.Cancelled), "Cancelled participation must not be eligible.");
Assert(!ProgramCompletionRules.IsEligible(true, null, ProgramParticipationStatus.Active), "A student without a program must not be eligible.");

var candidates = new[]
{
    new ProgramCompletionCandidate(3, true, ProgramParticipationStatus.Active, null),
    new ProgramCompletionCandidate(3, true, ProgramParticipationStatus.Ended, StudentProgramAuditEvent.CompletionConfirmed),
    new ProgramCompletionCandidate(3, true, ProgramParticipationStatus.Cancelled, StudentProgramAuditEvent.CompletionConfirmed),
    new ProgramCompletionCandidate(3, true, ProgramParticipationStatus.Unverified, StudentProgramAuditEvent.CompletionConfirmed),
    new ProgramCompletionCandidate(3, false, ProgramParticipationStatus.Active, StudentProgramAuditEvent.CompletionConfirmed),
    new ProgramCompletionCandidate(4, true, ProgramParticipationStatus.Active, StudentProgramAuditEvent.CompletionConfirmed)
};
var completionCounts = ProgramCompletionRules.Count(candidates, 3);
Assert(completionCounts.EligibleCount == 2, "Only approved active/ended records in the selected program belong in the denominator.");
Assert(completionCounts.CompletedCount == 1, "Only eligible HR-confirmed records belong in the numerator.");
Assert(!ProgramCompletionRules.IsCompletionConfirmed(true, StudentProgramAuditEvent.CompletionRevoked), "A revoked completion must not count.");

Assert(ProgramAssignmentAuditPolicy.GetAffectedProgramIds(7, 7).Count == 0,
    "An unchanged ProgramId must not reset participation or completion history.");
Assert(ProgramAssignmentAuditPolicy.GetAffectedProgramIds(7, 9).SequenceEqual(new[] { 7, 9 }),
    "Changing A to B must reset both the previous and target program histories.");

var assignmentHistory = new Dictionary<int, List<(string EventType, string? Status)>>
{
    [7] = new()
    {
        (StudentProgramAuditEvent.CompletionConfirmed, null),
        (StudentProgramAuditEvent.ParticipationStatusChanged, ProgramParticipationStatus.Ended)
    },
    [9] = new()
    {
        (StudentProgramAuditEvent.CompletionConfirmed, null),
        (StudentProgramAuditEvent.ParticipationStatusChanged, ProgramParticipationStatus.Active)
    }
};
ApplyProgramAssignmentAuditPolicy(assignmentHistory, 7, 9);
ApplyProgramAssignmentAuditPolicy(assignmentHistory, 9, 7);
Assert(GetLatestCompletionAction(assignmentHistory[7]) == StudentProgramAuditEvent.CompletionRevoked,
    "A to B to A must not reactivate A's earlier completion confirmation.");
Assert(assignmentHistory[7].Last(item => item.EventType == StudentProgramAuditEvent.ParticipationStatusChanged).Status
       == ProgramParticipationStatus.Unverified,
    "Returning to A must require HR to classify participation again.");

Assert(ProgramAssignmentAuditPolicy.GetAffectedProgramIds(null, 7).SequenceEqual(new[] { 7 }),
    "Assigning a previously unassigned student to A must clear stale A history.");
var nullToAHistory = new List<(string EventType, string? Status)>
{
    (StudentProgramAuditEvent.CompletionConfirmed, null),
    (StudentProgramAuditEvent.ParticipationStatusChanged, ProgramParticipationStatus.Active)
};
ResetProgramAuditHistory(nullToAHistory);
Assert(GetLatestCompletionAction(nullToAHistory) == StudentProgramAuditEvent.CompletionRevoked,
    "NULL to A must not revive a stale confirmation already associated with A.");
Assert(ProgramAssignmentAuditPolicy.GetAffectedProgramIds(7, null).SequenceEqual(new[] { 7 }),
    "Removing a program assignment must reset its previous program history.");

Console.WriteLine("Sprint 5 report export smoke tests passed.");

static void ApplyProgramAssignmentAuditPolicy(
    Dictionary<int, List<(string EventType, string? Status)>> history,
    int? previousProgramId,
    int? requestedProgramId)
{
    foreach (var programId in ProgramAssignmentAuditPolicy.GetAffectedProgramIds(previousProgramId, requestedProgramId))
    {
        if (!history.TryGetValue(programId, out var events))
            history[programId] = events = new List<(string EventType, string? Status)>();
        ResetProgramAuditHistory(events);
    }
}

static void ResetProgramAuditHistory(List<(string EventType, string? Status)> events)
{
    if (GetLatestCompletionAction(events) == StudentProgramAuditEvent.CompletionConfirmed)
        events.Add((StudentProgramAuditEvent.CompletionRevoked, null));
    events.Add((StudentProgramAuditEvent.ParticipationStatusChanged, ProgramParticipationStatus.Unverified));
}

static string? GetLatestCompletionAction(List<(string EventType, string? Status)> events) =>
    events.LastOrDefault(item =>
        item.EventType is StudentProgramAuditEvent.CompletionConfirmed or StudentProgramAuditEvent.CompletionRevoked)
        .EventType;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertThrows<TException>(Action action) where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
}

sealed class SmokeTestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Test";
    public string ApplicationName { get; set; } = "Sprint5ReportExportSmoke";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
