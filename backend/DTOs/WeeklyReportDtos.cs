using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.WeeklyReports
{
    public sealed class WeeklyReportRequestDto
    {
        [Required]
        public DateOnly WeekStartDate { get; set; }

        [Required, StringLength(4000, MinimumLength = 5)]
        public string WorkSummary { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Results { get; set; }

        [StringLength(4000)]
        public string? Challenges { get; set; }

        [StringLength(4000)]
        public string? NextWeekPlan { get; set; }

        [StringLength(2048)]
        public string? AttachmentUrl { get; set; }
    }

    public sealed class MentorFeedbackRequestDto
    {
        [Required, StringLength(4000, MinimumLength = 2)]
        public string Content { get; set; } = string.Empty;
    }

    public sealed class WeeklyReportResponseDto
    {
        public int Id { get; init; }
        public string StudentName { get; init; } = string.Empty;
        public string StudentCode { get; init; } = string.Empty;
        public DateOnly WeekStartDate { get; init; }
        public string WorkSummary { get; init; } = string.Empty;
        public string? Results { get; init; }
        public string? Challenges { get; init; }
        public string? NextWeekPlan { get; init; }
        public string? AttachmentUrl { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public bool CanEditFeedback { get; init; }
        public MentorFeedbackResponseDto? MentorFeedback { get; init; }
    }

    public sealed class MentorFeedbackResponseDto
    {
        public int Id { get; init; }
        public string Content { get; init; } = string.Empty;
        public string MentorName { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
