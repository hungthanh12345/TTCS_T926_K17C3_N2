namespace InternshipManagementApi.Data.Entities;

public sealed class EmailLog
{
    public int Id { get; set; }
    public int? StudentId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime NextAttemptAt { get; set; }

    public Student? Student { get; set; }
    public EmailTemplate Template { get; set; } = null!;
}
