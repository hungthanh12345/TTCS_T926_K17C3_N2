using System.Text.RegularExpressions;
using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Mentor;
using InternshipManagementApi.Repositories;

namespace InternshipManagementApi.Services
{
    public interface IMentorService
    {
        Task<MentorResponseDto> CreateMentorAsync(CreateMentorRequestDto request);
        Task<IEnumerable<MentorResponseDto>> GetAllMentorsAsync();
        Task<MentorResponseDto?> GetMentorByIdAsync(int id);
        Task DeleteMentorAsync(int id);
    }

    public class MentorService : IMentorService
    {
        private readonly IMentorRepository _mentorRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPasswordHasher _passwordHasher;

        public MentorService(
            IMentorRepository mentorRepository,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IPasswordHasher passwordHasher)
        {
            _mentorRepository = mentorRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<MentorResponseDto> CreateMentorAsync(CreateMentorRequestDto request)
        {
            User? user = null;

            if (request.UserId.HasValue && request.UserId.Value > 0)
            {
                user = await _userRepository.GetByIdWithRoleAsync(request.UserId.Value);
                if (user == null)
                {
                    throw new NotFoundException($"User with ID {request.UserId.Value} not found.");
                }

                if (user.Role == null || !string.Equals(user.Role.Name, "ROLE_MENTOR", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BadRequestException($"User with ID {request.UserId.Value} has role '{(user.Role?.Name ?? "UNKNOWN")}'. Only users with role 'ROLE_MENTOR' can be assigned a mentor profile.");
                }

                bool mentorExists = await _mentorRepository.ExistsByUserIdAsync(request.UserId.Value);
                if (mentorExists)
                {
                    throw new ConflictException($"A mentor profile already exists for User ID {request.UserId.Value}.");
                }
            }
            else
            {
                // Auto link or create a user account for the mentor
                string email;
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    email = request.Email.Trim().ToLower();
                }
                else
                {
                    var cleanName = Regex.Replace(request.FullName.ToLower(), @"[^a-z0-9]+", ".");
                    email = $"{cleanName.Trim('.')}@ims.edu.vn";
                }

                user = await _userRepository.GetByEmailAsync(email);
                if (user != null)
                {
                    bool mentorExists = await _mentorRepository.ExistsByUserIdAsync(user.Id);
                    if (mentorExists)
                    {
                        throw new ConflictException($"A mentor profile already exists for user email '{email}'.");
                    }
                }
                else
                {
                    var mentorRole = await _roleRepository.GetByNameAsync("ROLE_MENTOR")
                        ?? throw new InvalidOperationException("System role 'ROLE_MENTOR' not found in database.");

                    user = new User
                    {
                        Email = email,
                        PasswordHash = _passwordHasher.Hash("Admin@123"),
                        RoleId = mentorRole.Id,
                        Status = UserStatus.ACTIVE
                    };
                    await _userRepository.AddAsync(user);
                }
            }

            var phone = !string.IsNullOrWhiteSpace(request.PhoneNumber) ? request.PhoneNumber : request.Phone;

            var mentor = new Mentor
            {
                UserId = user.Id,
                FullName = request.FullName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                Department = request.Department.Trim(),
                Specialization = string.IsNullOrWhiteSpace(request.Specialization) ? null : request.Specialization.Trim()
            };

            await _mentorRepository.AddAsync(mentor);

            return new MentorResponseDto
            {
                Id = mentor.Id,
                UserId = mentor.UserId,
                Email = user.Email,
                FullName = mentor.FullName,
                PhoneNumber = mentor.PhoneNumber,
                Department = mentor.Department,
                Specialization = mentor.Specialization,
                AssignedStudentsCount = 0,
                CreatedAt = mentor.CreatedAt,
                UpdatedAt = mentor.UpdatedAt
            };
        }

        public async Task<IEnumerable<MentorResponseDto>> GetAllMentorsAsync()
        {
            var mentors = await _mentorRepository.GetAllWithDetailsAsync();

            return mentors.Select(m => new MentorResponseDto
            {
                Id = m.Id,
                UserId = m.UserId,
                Email = m.User?.Email ?? string.Empty,
                FullName = m.FullName,
                PhoneNumber = m.PhoneNumber,
                Department = m.Department,
                Specialization = m.Specialization,
                AssignedStudentsCount = m.Students?.Count ?? 0,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            });
        }

        public async Task<MentorResponseDto?> GetMentorByIdAsync(int id)
        {
            var mentor = await _mentorRepository.GetByIdWithDetailsAsync(id);
            if (mentor == null)
            {
                return null;
            }

            return new MentorResponseDto
            {
                Id = mentor.Id,
                UserId = mentor.UserId,
                Email = mentor.User?.Email ?? string.Empty,
                FullName = mentor.FullName,
                PhoneNumber = mentor.PhoneNumber,
                Department = mentor.Department,
                Specialization = mentor.Specialization,
                AssignedStudentsCount = mentor.Students?.Count ?? 0,
                CreatedAt = mentor.CreatedAt,
                UpdatedAt = mentor.UpdatedAt
            };
        }

        public async Task DeleteMentorAsync(int id)
        {
            var mentor = await _mentorRepository.GetByIdAsync(id);
            if (mentor == null)
            {
                throw new NotFoundException($"Mentor with ID {id} not found.");
            }

            await _mentorRepository.DeleteAsync(mentor);
        }
    }
}
