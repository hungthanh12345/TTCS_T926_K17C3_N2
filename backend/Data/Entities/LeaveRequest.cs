using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementApi.Data.Entities
{
    [Table("leave_requests")]
    public class LeaveRequest
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Student? Student { get; set; }
    }
}
