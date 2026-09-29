using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Auth;
using InternshipManagementApi.Repositories;

namespace InternshipManagementApi.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new UnauthorizedException("Invalid email or password.");
            }

            if (user.Status == UserStatus.LOCKED)
            {
                throw new UnauthorizedException("Account is locked. Please contact the system administrator.");
            }

            if (user.Status == UserStatus.INACTIVE)
            {
                throw new UnauthorizedException("Account is inactive. Please contact the administrator.");
            }

            bool isPasswordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                throw new UnauthorizedException("Invalid email or password.");
            }

            var (token, expiresAt, expiresInSeconds) = _jwtTokenService.GenerateToken(user);

            return new LoginResponseDto
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresIn = expiresInSeconds,
                ExpiresAt = expiresAt,
                User = new AuthUserInfoDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Role = user.Role.Name,
                    Status = user.Status.ToString()
                }
            };
        }
    }
}
