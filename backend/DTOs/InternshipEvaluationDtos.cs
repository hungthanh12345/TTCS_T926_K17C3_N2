using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.Evaluations
{
    public sealed class InternshipEvaluationRequestDto
    {
        [Range(1, 10)]
        public int SkillsScore { get; set; }

        [Range(1, 10)]
        public int AttitudeScore { get; set; }

        [Required, StringLength(4000, MinimumLength = 2)]
        public string Comments { get; set; } = string.Empty;
    }

    public sealed class InternshipEvaluationResponseDto
    {
        public int Id { get; init; }
        public int StudentId { get; init; }
        public string StudentCode { get; init; } = string.Empty;
        public string StudentName { get; init; } = string.Empty;
        public string University { get; init; } = string.Empty;
        public string Major { get; init; } = string.Empty;
        public int SkillsScore { get; init; }
        public int AttitudeScore { get; init; }
        public decimal OverallScore { get; init; }
        public string Comments { get; init; } = string.Empty;
        public string MentorName { get; init; } = string.Empty;
        public DateTime EvaluatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public bool CanEdit { get; init; }
    }

    public sealed class MentorEvaluationStudentDto
    {
        public int StudentId { get; init; }
        public string StudentCode { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string University { get; init; } = string.Empty;
        public string Major { get; init; } = string.Empty;
        public InternshipEvaluationResponseDto? Evaluation { get; init; }
    }

    public sealed class HrInternshipSummaryItemDto
    {
        public int StudentId { get; init; }
        public string StudentCode { get; init; } = string.Empty;
        public string StudentName { get; init; } = string.Empty;
        public string University { get; init; } = string.Empty;
        public string Major { get; init; } = string.Empty;
        public string? MentorName { get; init; }
        public string? MentorDepartment { get; init; }
        public int WeeklyReportCount { get; init; }
        public int MentorFeedbackCount { get; init; }
        public DateOnly? LatestReportWeek { get; init; }
        public string EvaluationStatus { get; init; } = "PENDING";
        public int? SkillsScore { get; init; }
        public int? AttitudeScore { get; init; }
        public decimal? OverallScore { get; init; }
        public string? EvaluationComments { get; init; }
        public string? EvaluatedBy { get; init; }
        public DateTime? EvaluatedAt { get; init; }
    }

    public sealed class HrInternshipSummaryResponseDto
    {
        public int TotalStudents { get; init; }
        public int EvaluatedStudents { get; init; }
        public int PendingEvaluations { get; init; }
        public decimal? AverageOverallScore { get; init; }
        public IReadOnlyList<HrInternshipSummaryItemDto> Items { get; init; } = Array.Empty<HrInternshipSummaryItemDto>();
    }
}
