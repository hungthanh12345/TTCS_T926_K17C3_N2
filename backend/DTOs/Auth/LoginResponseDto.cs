namespace InternshipManagementApi.DTOs.Auth
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public long ExpiresIn { get; set; }
        public DateTime ExpiresAt { get; set; }
        public AuthUserInfoDto User { get; set; } = null!;
    }

    public class AuthUserInfoDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
