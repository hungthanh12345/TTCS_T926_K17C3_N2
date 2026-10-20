namespace InternshipManagementApi.Data.Entities;

public static class AttendanceStatus
{
    public const string CheckedIn = "CHECKED_IN";
    public const string Completed = "COMPLETED";
}

public sealed class Attendance
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public DateOnly Date { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string Status { get; set; } = AttendanceStatus.CheckedIn;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Student Student { get; set; } = null!;
}
