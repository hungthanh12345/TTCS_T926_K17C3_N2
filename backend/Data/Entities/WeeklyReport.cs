namespace InternshipManagementApi.Data.Entities
{
    public class WeeklyReport
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateOnly WeekStartDate { get; set; }
        public string WorkSummary { get; set; } = string.Empty;
        public string? Results { get; set; }
        public string? Challenges { get; set; }
        public string? NextWeekPlan { get; set; }
        public string? AttachmentUrl { get; set; }
        public string Status { get; set; } = "SUBMITTED";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual MentorFeedback? MentorFeedback { get; set; }
    }
}
