namespace InternshipManagementApi.DTOs.Reports;

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
}
