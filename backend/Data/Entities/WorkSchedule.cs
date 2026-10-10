
namespace InternshipManagementApi.Data.Entities;

public class WorkSchedule
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // GROUP: lich cho nhom; STUDENT: lich rieng cho thuc tap sinh
    public string TargetType { get; set; } = "GROUP";

    public string? GroupName { get; set; }

    public int? StudentId { get; set; }

    public bool IsFlexible { get; set; }

    public string Status { get; set; } = "ACTIVE";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Student? Student { get; set; }

    public ICollection<WorkScheduleDay> Days { get; set; }
        = new List<WorkScheduleDay>();
}
