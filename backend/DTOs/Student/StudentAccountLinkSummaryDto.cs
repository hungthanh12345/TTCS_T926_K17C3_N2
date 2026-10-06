namespace InternshipManagementApi.DTOs.Student
{
    public sealed class StudentAccountLinkSummaryDto
    {
        public int AccountCount { get; set; }
        public int LinkedAccountCount { get; set; }
        public int UnlinkedAccountCount { get; set; }
        public int ProfileCount { get; set; }
        public int UnlinkedProfileCount { get; set; }
        public IReadOnlyList<StudentAccountLinkOptionDto> Accounts { get; set; } = Array.Empty<StudentAccountLinkOptionDto>();
    }

    public sealed class StudentAccountLinkOptionDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool HasStudentProfile { get; set; }
    }
}
