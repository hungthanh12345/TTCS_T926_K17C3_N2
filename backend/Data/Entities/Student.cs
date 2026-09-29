using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementApi.Data.Entities
{
    [Table("students")]
    public class Student
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public string StudentCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string University { get; set; } = string.Empty;

        public string Major { get; set; } = string.Empty;

        public int? MentorId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual User? User { get; set; }

        public virtual Mentor? Mentor { get; set; }
    }
}
