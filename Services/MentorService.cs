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
    }

    public class MentorService : IMentorService
    {
        private readonly IMentorRepository _mentorRepository;
        private readonly IUserRepository _userRepository;

        public MentorService(
            IMentorRepository mentorRepository,
            IUserRepository userRepository)
        {
            _mentorRepository = mentorRepository;
            _userRepository = userRepository;
        }

        public async Task<MentorResponseDto> CreateMentorAsync(CreateMentorRequestDto request)
        {
            var user = await _userRepository.GetByIdWithRoleAsync(request.UserId);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {request.UserId} not found.");
            }

            if (user.Role == null || !string.Equals(user.Role.Name, "ROLE_MENTOR", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException($"User with ID {request.UserId} has role '{(user.Role?.Name ?? "UNKNOWN")}'. Only users with role 'ROLE_MENTOR' can be assigned a mentor profile.");
            }

            bool mentorExists = await _mentorRepository.ExistsByUserIdAsync(request.UserId);
            if (mentorExists)
            {
                throw new ConflictException($"A mentor profile already exists for User ID {request.UserId}.");
            }

            var mentor = new Mentor
            {
                UserId = request.UserId,
                FullName = request.FullName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
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
    }
}
