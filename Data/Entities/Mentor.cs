using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementApi.Data.Entities
{
    [Table("mentors")]
    public class Mentor
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string Department { get; set; } = string.Empty;

        public string? Specialization { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
