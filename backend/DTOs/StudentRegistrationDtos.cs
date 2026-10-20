using System.ComponentModel.DataAnnotations;
using InternshipManagementApi.Common;

namespace InternshipManagementApi.DTOs.StudentRegistration
{
    public sealed class StudentRegistrationRequestDto
    {
        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6), MaxLength(100), MaxUtf8ByteLength(72)]
        public string Password { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string StudentCode { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Invalid phone number format.")]
        public string? PhoneNumber { get; set; }

        [Required, MaxLength(150)]
        public string University { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Major { get; set; } = string.Empty;
    }

    public sealed class StudentRegistrationApplicationRequestDto
    {
        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6), MaxLength(100), MaxUtf8ByteLength(72)]
        public string Password { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string StudentCode { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Invalid phone number format.")]
        public string? PhoneNumber { get; set; }

        [Required, MaxLength(150)]
        public string University { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Major { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int ProgramId { get; set; }

        [Required]
        public IFormFile? Cv { get; set; }

        [Required]
        public IFormFile? InternshipLetter { get; set; }
    }

    public sealed class StudentRegistrationStatusResponseDto
    {
        public int StudentId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class StudentRegistrationReviewDto
    {
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string University { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public string? ProgramName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public int? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
    }
}
