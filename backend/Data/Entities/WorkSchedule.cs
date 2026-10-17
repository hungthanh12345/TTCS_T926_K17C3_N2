namespace InternshipManagementApi.Data.Entities;

public static class WorkScheduleTargetType
{
    public const string Student = "STUDENT";
    public const string Group = "GROUP";
}

public sealed class WorkSchedule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetType { get; set; } = WorkScheduleTargetType.Student;
    public int? StudentGroupId { get; set; }
    public int? StudentId { get; set; }
    public bool IsFlexible { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateOnly? EffectiveStartDate { get; set; }
    public string? RecurrenceType { get; set; }
    public int? RecurrenceWeeks { get; set; }
    public DateOnly? EffectiveEndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public StudentGroup? Group { get; set; }
    public Student? Student { get; set; }
    public ICollection<WorkScheduleDay> Days { get; set; } = new List<WorkScheduleDay>();
}

public sealed class WorkScheduleDay
{
    public int Id { get; set; }
    public int WorkScheduleId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakStart { get; set; }
    public TimeSpan? BreakEnd { get; set; }
    public WorkSchedule WorkSchedule { get; set; } = null!;
}
