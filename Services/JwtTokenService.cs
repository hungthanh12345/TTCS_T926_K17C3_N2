using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InternshipManagementApi.Data.Entities;
using Microsoft.IdentityModel.Tokens;

namespace InternshipManagementApi.Services
{
    public interface IJwtTokenService
    {
        (string Token, DateTime ExpiresAt, long ExpiresInSeconds) GenerateToken(User user);
    }

    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (string Token, DateTime ExpiresAt, long ExpiresInSeconds) GenerateToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyForInternshipManagementSystem2026SecureKey!";
            var issuer = _configuration["Jwt:Issuer"] ?? "InternshipManagementApi";
            var audience = _configuration["Jwt:Audience"] ?? "InternshipManagementClient";
            var expiryHoursStr = _configuration["Jwt:ExpiryInHours"] ?? "8";
            
            if (!double.TryParse(expiryHoursStr, out var expiryHours))
            {
                expiryHours = 8;
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var expiresAt = now.AddHours(expiryHours);
            var expiresInSeconds = (long)(expiresAt - now).TotalSeconds;

            var roleName = user.Role?.Name ?? string.Empty;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, roleName),
                // Custom payload claims as specifically required
                new("userId", user.Id.ToString()),
                new("email", user.Email),
                new("role", roleName),
                new("status", user.Status.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return (tokenString, expiresAt, expiresInSeconds);
        }
    }
}
