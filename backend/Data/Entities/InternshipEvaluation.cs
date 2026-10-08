namespace InternshipManagementApi.Data.Entities
{
    public class InternshipEvaluation
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int? MentorId { get; set; }
        public string MentorNameSnapshot { get; set; } = string.Empty;
        public int SkillsScore { get; set; }
        public int AttitudeScore { get; set; }
        public string Comments { get; set; } = string.Empty;
        public DateTime EvaluatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual Mentor? Mentor { get; set; }
    }
}
