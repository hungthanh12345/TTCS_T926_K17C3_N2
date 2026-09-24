using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Mentor;
using InternshipManagementApi.DTOs.Student;
using InternshipManagementApi.Repositories;

namespace InternshipManagementApi.Services
{
    public interface IStudentService
    {
        Task<StudentResponseDto> CreateStudentAsync(CreateStudentRequestDto request);
        Task<StudentResponseDto> GetStudentByIdAsync(int id);
        Task<StudentResponseDto> UpdateStudentAsync(int id, UpdateStudentRequestDto request);
        Task<PagedResult<StudentResponseDto>> SearchStudentsAsync(StudentSearchFilterDto filter);
        Task<StudentResponseDto> AssignMentorAsync(int studentId, AssignMentorRequestDto request);
        Task DeleteStudentAsync(int id);
    }

    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMentorRepository _mentorRepository;

        public StudentService(
            IStudentRepository studentRepository,
            IUserRepository userRepository,
            IMentorRepository mentorRepository)
        {
            _studentRepository = studentRepository;
            _userRepository = userRepository;
            _mentorRepository = mentorRepository;
        }

        public async Task<StudentResponseDto> CreateStudentAsync(CreateStudentRequestDto request)
        {
            var code = request.StudentCode.Trim().ToUpper();

            bool codeExists = await _studentRepository.ExistsByStudentCodeAsync(code);
            if (codeExists)
            {
                throw new ConflictException($"Student code '{request.StudentCode}' already exists.");
            }

            User? user = null;
            if (request.UserId.HasValue)
            {
                user = await _userRepository.GetByIdWithRoleAsync(request.UserId.Value);
                if (user == null)
                {
                    throw new NotFoundException($"User with ID {request.UserId.Value} not found.");
                }

                if (user.Role == null || !string.Equals(user.Role.Name, "ROLE_STUDENT", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BadRequestException($"User with ID {request.UserId.Value} has role '{(user.Role?.Name ?? "UNKNOWN")}'. Only users with role 'ROLE_STUDENT' can be linked to a student profile.");
                }

                bool userAlreadyLinked = await _studentRepository.ExistsByUserIdAsync(request.UserId.Value);
                if (userAlreadyLinked)
                {
                    throw new ConflictException($"User ID {request.UserId.Value} is already linked to another student profile.");
                }
            }

            Mentor? mentor = null;
            if (request.MentorId.HasValue)
            {
                mentor = await _mentorRepository.GetByIdAsync(request.MentorId.Value);
                if (mentor == null)
                {
                    throw new NotFoundException($"Mentor with ID {request.MentorId.Value} not found.");
                }
            }

            var student = new Student
            {
                StudentCode = code,
                FullName = request.FullName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                University = request.University.Trim(),
                Major = request.Major.Trim(),
                UserId = request.UserId,
                MentorId = request.MentorId
            };

            await _studentRepository.AddAsync(student);

            var createdStudent = await _studentRepository.GetByIdWithDetailsAsync(student.Id);
            return MapToResponseDto(createdStudent ?? student);
        }

        public async Task<StudentResponseDto> GetStudentByIdAsync(int id)
        {
            var student = await _studentRepository.GetByIdWithDetailsAsync(id);
            if (student == null)
            {
                throw new NotFoundException($"Student with ID {id} not found.");
            }

            return MapToResponseDto(student);
        }

        public async Task<StudentResponseDto> UpdateStudentAsync(int id, UpdateStudentRequestDto request)
        {
            var student = await _studentRepository.GetByIdWithDetailsAsync(id);
            if (student == null)
            {
                throw new NotFoundException($"Student with ID {id} not found.");
            }

            if (!string.IsNullOrWhiteSpace(request.StudentCode))
            {
                var newCode = request.StudentCode.Trim().ToUpper();
                if (!string.Equals(student.StudentCode, newCode, StringComparison.OrdinalIgnoreCase))
                {
                    bool codeExists = await _studentRepository.ExistsByStudentCodeAsync(newCode, excludeId: id);
                    if (codeExists)
                    {
                        throw new ConflictException($"Student code '{request.StudentCode}' is already in use by another student.");
                    }
                    student.StudentCode = newCode;
                }
            }

            if (request.UserId.HasValue && request.UserId.Value != student.UserId)
            {
                var user = await _userRepository.GetByIdWithRoleAsync(request.UserId.Value);
                if (user == null)
                {
                    throw new NotFoundException($"User with ID {request.UserId.Value} not found.");
                }

                if (user.Role == null || !string.Equals(user.Role.Name, "ROLE_STUDENT", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BadRequestException($"User with ID {request.UserId.Value} has role '{(user.Role?.Name ?? "UNKNOWN")}'. Only users with role 'ROLE_STUDENT' can be linked to a student profile.");
                }

                bool userAlreadyLinked = await _studentRepository.ExistsByUserIdAsync(request.UserId.Value, excludeId: id);
                if (userAlreadyLinked)
                {
                    throw new ConflictException($"User ID {request.UserId.Value} is already linked to another student profile.");
                }

                student.UserId = request.UserId.Value;
            }

            if (request.MentorId.HasValue && request.MentorId.Value != student.MentorId)
            {
                var mentor = await _mentorRepository.GetByIdAsync(request.MentorId.Value);
                if (mentor == null)
                {
                    throw new NotFoundException($"Mentor with ID {request.MentorId.Value} not found.");
                }
                student.MentorId = request.MentorId.Value;
            }

            student.FullName = request.FullName.Trim();
            var phone = !string.IsNullOrWhiteSpace(request.PhoneNumber) ? request.PhoneNumber : request.Phone;
            student.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            student.University = request.University.Trim();
            student.Major = request.Major.Trim();

            await _studentRepository.UpdateAsync(student);

            var updatedStudent = await _studentRepository.GetByIdWithDetailsAsync(student.Id);
            return MapToResponseDto(updatedStudent ?? student);
        }

        public async Task<PagedResult<StudentResponseDto>> SearchStudentsAsync(StudentSearchFilterDto filter)
        {
            var pagedStudents = await _studentRepository.SearchAsync(filter);

            var items = pagedStudents.Items.Select(MapToResponseDto).ToList();

            return new PagedResult<StudentResponseDto>(
                items,
                pagedStudents.TotalItems,
                pagedStudents.PageNumber,
                pagedStudents.PageSize
            );
        }

        public async Task<StudentResponseDto> AssignMentorAsync(int studentId, AssignMentorRequestDto request)
        {
            var student = await _studentRepository.GetByIdWithDetailsAsync(studentId);
            if (student == null)
            {
                throw new NotFoundException($"Student with ID {studentId} not found.");
            }

            if (request.MentorId.HasValue && request.MentorId.Value > 0)
            {
                var mentor = await _mentorRepository.GetByIdAsync(request.MentorId.Value);
                if (mentor == null)
                {
                    throw new NotFoundException($"Mentor with ID {request.MentorId.Value} not found.");
                }

                student.MentorId = request.MentorId.Value;
            }
            else
            {
                student.MentorId = null;
            }

            await _studentRepository.UpdateAsync(student);

            var updatedStudent = await _studentRepository.GetByIdWithDetailsAsync(student.Id);
            return MapToResponseDto(updatedStudent ?? student);
        }

        public async Task DeleteStudentAsync(int id)
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
            {
                throw new NotFoundException($"Student with ID {id} not found.");
            }

            await _studentRepository.DeleteAsync(student);
        }

        private static StudentResponseDto MapToResponseDto(Student student)
        {
            return new StudentResponseDto
            {
                Id = student.Id,
                UserId = student.UserId,
                StudentCode = student.StudentCode,
                FullName = student.FullName,
                PhoneNumber = student.PhoneNumber,
                University = student.University,
                Major = student.Major,
                MentorId = student.MentorId,
                Mentor = student.Mentor != null ? new MentorSummaryDto
                {
                    Id = student.Mentor.Id,
                    FullName = student.Mentor.FullName,
                    Department = student.Mentor.Department,
                    Specialization = student.Mentor.Specialization,
                    PhoneNumber = student.Mentor.PhoneNumber
                } : null,
                User = student.User != null ? new StudentUserSummaryDto
                {
                    Id = student.User.Id,
                    Email = student.User.Email,
                    Status = student.User.Status.ToString()
                } : null,
                CreatedAt = student.CreatedAt,
                UpdatedAt = student.UpdatedAt
            };
        }
    }
}
