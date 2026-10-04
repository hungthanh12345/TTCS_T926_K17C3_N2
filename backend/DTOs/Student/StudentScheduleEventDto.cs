namespace InternshipManagementApi.DTOs.Student
{
    public sealed class StudentScheduleEventDto
    {
        public int TaskId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public DateOnly DueDate { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}
