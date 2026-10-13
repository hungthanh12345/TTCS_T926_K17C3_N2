namespace InternshipManagementApi.Data.Entities;

public sealed class StudentGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<StudentGroupMembership> Memberships { get; set; } = new List<StudentGroupMembership>();
    public ICollection<WorkSchedule> WorkSchedules { get; set; } = new List<WorkSchedule>();
}

public sealed class StudentGroupMembership
{
    public int StudentId { get; set; }
    public int StudentGroupId { get; set; }
    public DateTime JoinedAt { get; set; }
    public Student Student { get; set; } = null!;
    public StudentGroup Group { get; set; } = null!;
}
