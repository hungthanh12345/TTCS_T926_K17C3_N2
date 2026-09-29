using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagementApi.Data.Entities
{
    [Table("roles")]
    public class Role
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
