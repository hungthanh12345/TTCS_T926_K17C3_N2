namespace InternshipManagementApi.DTOs.Reports;

public sealed class StudentDemographicPairDto
{
    public string University { get; init; } = string.Empty;
    public string Major { get; init; } = string.Empty;
    public int StudentCount { get; init; }
}

public sealed class StudentDemographicsResponseDto
{
    public int TotalApprovedStudents { get; init; }
    public IReadOnlyList<string> Universities { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Majors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<StudentDemographicPairDto> Items { get; init; } = Array.Empty<StudentDemographicPairDto>();
}