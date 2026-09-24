using InternshipManagementApi.DTOs.Mentor;

namespace InternshipManagementApi.DTOs.Student
{
    public class StudentResponseDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string University { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public int? MentorId { get; set; }
        public MentorSummaryDto? Mentor { get; set; }
        public StudentUserSummaryDto? User { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class StudentUserSummaryDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
