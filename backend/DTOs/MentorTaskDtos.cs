using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.Tasks
{
    public sealed class CreateMentorTaskRequestDto
    {
        [Range(1, int.MaxValue)]
        public int StudentId { get; set; }

        [Required, MinLength(2), MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public DateOnly? DueDate { get; set; }
    }

    public sealed class UpdateMentorTaskRequestDto
    {
        [Required, MinLength(2), MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public DateOnly? DueDate { get; set; }
    }

    public sealed class MentorStudentSummaryDto
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string University { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
    }

    public sealed class InternshipTaskResponseDto
    {
        public int Id { get; set; }
        public int MentorId { get; set; }
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateOnly? DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
