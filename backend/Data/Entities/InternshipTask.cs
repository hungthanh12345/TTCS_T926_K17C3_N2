namespace InternshipManagementApi.Data.Entities
{
    public class InternshipTask
    {
        public int Id { get; set; }
        public int MentorId { get; set; }
        public int StudentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateOnly? DueDate { get; set; }
        public string Status { get; set; } = "TO_DO";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Mentor Mentor { get; set; } = null!;
        public virtual Student Student { get; set; } = null!;
    }
}
