using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementApi.Data.Entities
{
    [Table("users")]
    public class User
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public int RoleId { get; set; }

        public UserStatus Status { get; set; } = UserStatus.ACTIVE;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual Role Role { get; set; } = null!;

        public virtual Mentor? Mentor { get; set; }

        public virtual Student? Student { get; set; }
    }
}
