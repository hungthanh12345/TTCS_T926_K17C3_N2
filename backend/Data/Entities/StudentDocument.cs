namespace InternshipManagementApi.Data.Entities
{
    public sealed class StudentDocument
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public byte[]? FileContent { get; set; }
        public DateTime UploadedAt { get; set; }
        public string ReviewStatus { get; set; } = "PENDING";
        public int? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
        public Student Student { get; set; } = null!;
    }
}
