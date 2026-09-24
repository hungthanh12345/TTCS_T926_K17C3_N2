namespace InternshipManagementApi.DTOs.Mentor
{
    public class MentorResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Department { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public int AssignedStudentsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class MentorSummaryDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
