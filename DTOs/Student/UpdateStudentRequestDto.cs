using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.Student
{
    public class UpdateStudentRequestDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Invalid phone number format.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "University is required.")]
        [MaxLength(150, ErrorMessage = "University cannot exceed 150 characters.")]
        public string University { get; set; } = string.Empty;

        [Required(ErrorMessage = "Major is required.")]
        [MaxLength(100, ErrorMessage = "Major cannot exceed 100 characters.")]
        public string Major { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Student code cannot exceed 50 characters.")]
        public string? StudentCode { get; set; }

        public int? UserId { get; set; }

        public int? MentorId { get; set; }
    }
}
