namespace InternshipManagementApi.Data.Entities
{
    public static class ContractStatus
    {
        public const string PendingConfirmation = "PENDING_CONFIRMATION";
        public const string Confirmed = "CONFIRMED";
        public const string Cancelled = "CANCELLED";
    }

    public sealed class InternshipContract
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long SizeBytes { get; set; }
        public byte[]? FileContent { get; set; }
        public string? ContentHash { get; set; }
        public int? UploadedByUserId { get; set; }
        public DateTime? FileUploadedAt { get; set; }
        public string Status { get; set; } = ContractStatus.PendingConfirmation;
        public DateTime IssuedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public int? ConfirmedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Student Student { get; set; } = null!;
    }
}
