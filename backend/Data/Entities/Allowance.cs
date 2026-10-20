namespace InternshipManagementApi.Data.Entities;

public static class AllowanceStatus
{
    public const string Active = "ACTIVE";
    public const string Cancelled = "CANCELLED";
}

public sealed class Allowance
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string AllowanceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string Status { get; set; } = AllowanceStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Student Student { get; set; } = null!;
    public ICollection<AllowancePayment> Payments { get; set; } = new List<AllowancePayment>();
}

public sealed class AllowancePayment
{
    public int Id { get; set; }
    public int AllowanceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public int RecordedByUserId { get; set; }
    public string? Reference { get; set; }
    public DateTime CreatedAt { get; set; }
    public Allowance Allowance { get; set; } = null!;
}
