using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Admin;
using InternshipManagementApi.Repositories;

namespace InternshipManagementApi.Services
{
    public interface IUserService
    {
        Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto request);
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto request)
        {
            var normalizedEmail = request.Email.Trim().ToLower();

            bool emailExists = await _userRepository.ExistsByEmailAsync(normalizedEmail);
            if (emailExists)
            {
                throw new ConflictException($"Email '{request.Email}' is already registered.");
            }

            Role? role = null;

            if (request.RoleId.HasValue)
            {
                role = await _roleRepository.GetByIdAsync(request.RoleId.Value);
            }

            if (role == null && !string.IsNullOrWhiteSpace(request.RoleName))
            {
                var roleName = request.RoleName.Trim();
                if (!roleName.StartsWith("ROLE_", StringComparison.OrdinalIgnoreCase))
                {
                    roleName = $"ROLE_{roleName}";
                }
                role = await _roleRepository.GetByNameAsync(roleName);
            }

            if (role == null)
            {
                throw new BadRequestException("Invalid role specified. Please provide a valid role ID (1=ROLE_ADMIN, 2=ROLE_HR, 3=ROLE_MENTOR, 4=ROLE_STUDENT) or valid role name.");
            }

            var passwordHash = _passwordHasher.Hash(request.Password);

            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = passwordHash,
                RoleId = role.Id,
                Status = request.Status
            };

            await _userRepository.AddAsync(user);

            return new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                RoleId = role.Id,
                RoleName = role.Name,
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllWithRolesAsync();

            return users.Select(u => new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                RoleId = u.RoleId,
                RoleName = u.Role?.Name ?? string.Empty,
                Status = u.Status.ToString(),
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            });
        }
    }
}
