using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.Student
{
    public sealed class StudentDocumentUploadRequest
    {
        [Required]
        [StringLength(40)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        public IFormFile? File { get; set; }
    }

    public sealed class StudentDocumentReplaceRequest
    {
        [Required]
        public IFormFile? File { get; set; }
    }

    public sealed class ReviewStudentDocumentRequest
    {
        [StringLength(1000)]
        public string? RejectionReason { get; set; }
    }

    public sealed record StudentDocumentResponse(
        int Id,
        string DocumentType,
        string OriginalFileName,
        string ContentType,
        long SizeBytes,
        DateTime UploadedAt);

    public sealed record HrStudentDocumentResponse(
        int Id,
        string DocumentType,
        string OriginalFileName,
        string ContentType,
        long SizeBytes,
        DateTime UploadedAt,
        string ReviewStatus,
        int? ReviewedByUserId,
        string? ReviewedByEmail,
        DateTime? ReviewedAt,
        string? RejectionReason);
}
