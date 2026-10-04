namespace InternshipManagementApi.Data.Entities
{
    public class MentorFeedback
    {
        public int Id { get; set; }
        public int WeeklyReportId { get; set; }
        public int MentorId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual WeeklyReport WeeklyReport { get; set; } = null!;
        public virtual Mentor Mentor { get; set; } = null!;
    }
}
