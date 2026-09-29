using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.Mentor
{
    public class CreateMentorRequestDto
    {
        public int? UserId { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Invalid phone number format.")]
        public string? PhoneNumber { get; set; }

        public string? Phone { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        [MaxLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
        public string Department { get; set; } = string.Empty;

        [MaxLength(150, ErrorMessage = "Specialization cannot exceed 150 characters.")]
        public string? Specialization { get; set; }
    }
}
