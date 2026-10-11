using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementApi.Data.Entities
{
    [Table("attendances")]
    public class Attendance
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateOnly Date { get; set; }
        public TimeSpan? CheckIn { get; set; }
        public TimeSpan? CheckOut { get; set; }
        public string Status { get; set; } = "ABSENT";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Student? Student { get; set; }
    }
}
