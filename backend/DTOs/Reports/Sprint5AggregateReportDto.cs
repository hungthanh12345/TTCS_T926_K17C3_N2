namespace InternshipManagementApi.DTOs.Reports;

public enum Sprint5ReportKind
{
    Aggregate,
    MentorWorkload,
    StudentDemographics,
    ProgramCompletion
}

public static class Sprint5ReportKinds
{
    public static bool TryParse(string? value, out Sprint5ReportKind reportKind)
    {
        reportKind = value?.Trim().ToLowerInvariant() switch
        {
            null or "" or "aggregate" => Sprint5ReportKind.Aggregate,
            "mentor-workload" => Sprint5ReportKind.MentorWorkload,
            "student-demographics" => Sprint5ReportKind.StudentDemographics,
            "program-completion" => Sprint5ReportKind.ProgramCompletion,
            _ => (Sprint5ReportKind)(-1)
        };

        return Enum.IsDefined(reportKind);
    }

    public static bool Includes(this Sprint5ReportKind reportKind, Sprint5ReportKind section) =>
        reportKind == Sprint5ReportKind.Aggregate || reportKind == section;

    public static string GetTitle(this Sprint5ReportKind reportKind) => reportKind switch
    {
        Sprint5ReportKind.MentorWorkload => "Báo cáo khối lượng hướng dẫn của Mentor",
        Sprint5ReportKind.StudentDemographics => "Báo cáo thống kê theo trường/ngành",
        Sprint5ReportKind.ProgramCompletion => "Báo cáo tỷ lệ hoàn thành chương trình",
        _ => "Báo cáo thống kê thực tập tổng hợp"
    };
}

public sealed class MentorWorkloadReportRowDto
{
    public string MentorName { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public int AssignedStudentCount { get; init; }
}

public sealed class ProgramCompletionReportRowDto
{
    public string ProgramName { get; init; } = string.Empty;
    public int EligibleCount { get; init; }
    public int CompletedCount { get; init; }
}

public sealed class Sprint5AggregateReportDto
{
    public required DateTimeOffset GeneratedAtUtc { get; init; }
    public Sprint5ReportKind ReportKind { get; init; } = Sprint5ReportKind.Aggregate;
    public string? ProgramFilter { get; init; }
    public string? UniversityFilter { get; init; }
    public string? MajorFilter { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public IReadOnlyList<MentorWorkloadReportRowDto> MentorWorkloads { get; init; }
        = Array.Empty<MentorWorkloadReportRowDto>();
    public IReadOnlyList<StudentDemographicPairDto> Demographics { get; init; }
        = Array.Empty<StudentDemographicPairDto>();
    public bool CompletionDataAvailable { get; init; }
    public string? CompletionUnavailableReason { get; init; }
    public IReadOnlyList<ProgramCompletionReportRowDto> ProgramCompletions { get; init; }
        = Array.Empty<ProgramCompletionReportRowDto>();

    public bool HasData => ReportKind switch
    {
        Sprint5ReportKind.MentorWorkload => MentorWorkloads.Count > 0,
        Sprint5ReportKind.StudentDemographics => Demographics.Count > 0,
        Sprint5ReportKind.ProgramCompletion => ProgramCompletions.Any(row => row.EligibleCount > 0),
        _ => MentorWorkloads.Count > 0 ||
             Demographics.Count > 0 ||
             ProgramCompletions.Any(row => row.EligibleCount > 0)
    };
}
