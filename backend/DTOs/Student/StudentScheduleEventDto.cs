namespace InternshipManagementApi.DTOs.Student
{
    public sealed class StudentScheduleResponseDto
    {
        public StudentProgramPeriodDto? Program { get; init; }
        public IReadOnlyList<StudentScheduleEventDto> Events { get; init; } = Array.Empty<StudentScheduleEventDto>();
    }

    public sealed record StudentProgramPeriodDto(
        int Id,
        string Name,
        string DepartmentName,
        DateOnly? StartDate,
        DateOnly? EndDate);

    public sealed class StudentScheduleEventDto
    {
        public int TaskId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public DateOnly DueDate { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}
