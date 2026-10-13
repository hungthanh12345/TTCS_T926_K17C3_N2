namespace InternshipManagementApi.Data.Entities;

public static class SupportRequestStatus
{
    public const string Pending = "PENDING";
    public const string InProgress = "IN_PROGRESS";
    public const string Resolved = "RESOLVED";
}

public sealed class SupportRequest
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = SupportRequestStatus.Pending;
    public string? Response { get; set; }
    public int? HandledByUserId { get; set; }
    public DateTime? HandledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Student Student { get; set; } = null!;
}
