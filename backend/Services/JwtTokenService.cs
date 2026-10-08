using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
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
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT signing key is not configured.");
            var issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("JWT issuer is not configured.");
            var audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("JWT audience is not configured.");
            var expiryMinutes = ResolveExpiryMinutes();

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            if (expiryMinutes > (DateTime.MaxValue - now).TotalMinutes)
                throw new InvalidOperationException("JWT expiration exceeds the supported date range.");

            var expiresAt = now.AddMinutes(expiryMinutes);
            var expiresIn = (expiresAt - now).TotalSeconds;
            if (!double.IsFinite(expiresIn) || expiresIn > long.MaxValue)
                throw new InvalidOperationException("JWT expiration exceeds the supported duration.");
            var expiresInSeconds = (long)expiresIn;

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

        private double ResolveExpiryMinutes()
        {
            var minutesValue = _configuration["Jwt:ExpiryMinutes"];
            if (!string.IsNullOrWhiteSpace(minutesValue))
            {
                if (TryParsePositiveFinite(minutesValue, out var minutes))
                    return minutes;

                throw new InvalidOperationException("Jwt:ExpiryMinutes must be a positive finite number.");
            }

            var hoursValue = _configuration["Jwt:ExpiryInHours"];
            if (string.IsNullOrWhiteSpace(hoursValue))
                return 480;

            if (!TryParsePositiveFinite(hoursValue, out var hours) || !double.IsFinite(hours * 60))
                throw new InvalidOperationException("Jwt:ExpiryInHours must be a positive finite number.");

            return hours * 60;
        }

        private static bool TryParsePositiveFinite(string value, out double parsed)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                && double.IsFinite(parsed)
                && parsed > 0;
        }
    }
}
