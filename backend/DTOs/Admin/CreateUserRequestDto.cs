using System.ComponentModel.DataAnnotations;
using InternshipManagementApi.Data.Entities;

namespace InternshipManagementApi.DTOs.Admin
{
    public class CreateUserRequestDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Numeric ID of the role (e.g., 1 for ROLE_ADMIN, 2 for ROLE_HR, 3 for ROLE_MENTOR, 4 for ROLE_STUDENT).
        /// If omitted, RoleName must be specified.
        /// </summary>
        public int? RoleId { get; set; }

        /// <summary>
        /// Name of the role (e.g., ROLE_HR, ROLE_MENTOR, ROLE_STUDENT, ROLE_ADMIN).
        /// Optional if RoleId is provided.
        /// </summary>
        public string? RoleName { get; set; }

        /// <summary>
        /// Initial account status (ACTIVE, INACTIVE, LOCKED). Defaults to ACTIVE.
        /// </summary>
        public UserStatus Status { get; set; } = UserStatus.ACTIVE;
    }
}
