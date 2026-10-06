using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Tasks;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Services
{
    public interface IMentorTaskService
    {
        Task<IReadOnlyList<MentorStudentSummaryDto>> GetAssignedStudentsAsync(int userId);
        Task<IReadOnlyList<InternshipTaskResponseDto>> GetMentorTasksAsync(int userId);
        Task<InternshipTaskResponseDto> CreateTaskAsync(int userId, CreateMentorTaskRequestDto request);
        Task<InternshipTaskResponseDto> GetMentorTaskAsync(int userId, int taskId);
        Task<InternshipTaskResponseDto> UpdateTaskAsync(int userId, int taskId, UpdateMentorTaskRequestDto request);
        Task DeleteTaskAsync(int userId, int taskId);
        Task<IReadOnlyList<InternshipTaskResponseDto>> GetStudentTasksAsync(int userId);
        Task<InternshipTaskResponseDto> GetStudentTaskAsync(int userId, int taskId);
        Task<InternshipTaskResponseDto> UpdateStudentTaskProgressAsync(int userId, int taskId, UpdateStudentTaskProgressDto request);
    }

    public sealed class MentorTaskService : IMentorTaskService
    {
        private readonly AppDbContext _db;

        public MentorTaskService(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<MentorStudentSummaryDto>> GetAssignedStudentsAsync(int userId)
        {
            var mentor = await GetMentorAsync(userId);
            return await _db.Students
                .AsNoTracking()
                .Where(student => student.MentorId == mentor.Id &&
                                  student.User != null &&
                                  student.User.Role.Name == "ROLE_STUDENT" &&
                                  student.User.Status == UserStatus.ACTIVE)
                .OrderBy(student => student.FullName)
                .Select(student => new MentorStudentSummaryDto
                {
                    StudentId = student.Id,
                    StudentCode = student.StudentCode,
                    FullName = student.FullName,
                    Email = student.User!.Email,
                    PhoneNumber = student.PhoneNumber,
                    University = student.University,
                    Major = student.Major
                })
                .ToListAsync();
        }

        public async Task<IReadOnlyList<InternshipTaskResponseDto>> GetMentorTasksAsync(int userId)
        {
            var mentor = await GetMentorAsync(userId);
            var tasks = await _db.Tasks
                .AsNoTracking()
                .Include(task => task.Student)
                .Where(task => task.MentorId == mentor.Id)
                .OrderByDescending(task => task.CreatedAt)
                .ToListAsync();

            return tasks.Select(ToResponse).ToArray();
        }

        public async Task<InternshipTaskResponseDto> CreateTaskAsync(int userId, CreateMentorTaskRequestDto request)
        {
            ValidateTaskFields(request.Title, request.DueDate);
            var mentor = await GetMentorAsync(userId);
            var student = await _db.Students
                .Include(item => item.User)
                    .ThenInclude(user => user!.Role)
                .SingleOrDefaultAsync(item => item.Id == request.StudentId && item.MentorId == mentor.Id);

            if (student == null)
                throw new NotFoundException("The student is not assigned to this mentor.");

            if (student.User == null ||
                !string.Equals(student.User.Role?.Name, "ROLE_STUDENT", StringComparison.OrdinalIgnoreCase) ||
                student.User.Status != UserStatus.ACTIVE)
            {
                throw new ConflictException("Tasks can only be assigned to an active student account.");
            }

            var task = new InternshipTask
            {
                MentorId = mentor.Id,
                StudentId = student.Id,
                Title = request.Title.Trim(),
                Description = NormalizeDescription(request.Description),
                DueDate = request.DueDate,
                Status = "TO_DO",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Mentor = mentor,
                Student = student
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();
            return ToResponse(task);
        }

        public async Task<InternshipTaskResponseDto> GetMentorTaskAsync(int userId, int taskId)
        {
            var mentor = await GetMentorAsync(userId);
            var task = await GetMentorTaskEntityAsync(mentor.Id, taskId);
            return ToResponse(task);
        }

        public async Task<InternshipTaskResponseDto> UpdateTaskAsync(int userId, int taskId, UpdateMentorTaskRequestDto request)
        {
            ValidateTaskFields(request.Title, request.DueDate);
            var mentor = await GetMentorAsync(userId);
            var task = await GetMentorTaskEntityAsync(mentor.Id, taskId);

            task.Title = request.Title.Trim();
            task.Description = NormalizeDescription(request.Description);
            task.DueDate = request.DueDate;
            task.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ToResponse(task);
        }

        public async Task DeleteTaskAsync(int userId, int taskId)
        {
            var mentor = await GetMentorAsync(userId);
            var task = await GetMentorTaskEntityAsync(mentor.Id, taskId);
            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<InternshipTaskResponseDto>> GetStudentTasksAsync(int userId)
        {
            var tasks = await _db.Tasks
                .AsNoTracking()
                .Include(task => task.Student)
                .Where(task => task.Student.UserId == userId)
                .OrderByDescending(task => task.CreatedAt)
                .ToListAsync();

            return tasks.Select(ToResponse).ToArray();
        }

        public async Task<InternshipTaskResponseDto> GetStudentTaskAsync(int userId, int taskId)
        {
            var task = await GetStudentTaskEntityAsync(userId, taskId);
            return ToResponse(task);
        }

        public async Task<InternshipTaskResponseDto> UpdateStudentTaskProgressAsync(
            int userId,
            int taskId,
            UpdateStudentTaskProgressDto request)
        {
            var task = await GetStudentTaskEntityAsync(userId, taskId);

            if (!IsSupportedStudentStatus(request.Status))
                throw new BadRequestException("Status must be TO_DO, IN_PROGRESS, or DONE.");

            task.Status = request.Status;
            task.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ToResponse(task);
        }

        private async Task<Mentor> GetMentorAsync(int userId)
        {
            var mentor = await _db.Mentors.SingleOrDefaultAsync(item => item.UserId == userId);
            return mentor ?? throw new NotFoundException("No mentor profile is linked to this account.");
        }

        private async Task<InternshipTask> GetMentorTaskEntityAsync(int mentorId, int taskId)
        {
            var task = await _db.Tasks
                .Include(item => item.Student)
                .SingleOrDefaultAsync(item => item.Id == taskId && item.MentorId == mentorId);
            return task ?? throw new NotFoundException("Task was not found in this mentor's assignments.");
        }

        private async Task<InternshipTask> GetStudentTaskEntityAsync(int userId, int taskId)
        {
            var task = await _db.Tasks
                .Include(item => item.Student)
                .SingleOrDefaultAsync(item => item.Id == taskId);

            if (task == null)
                throw new NotFoundException("Task was not found.");

            if (task.Student.UserId != userId)
                throw new ForbiddenException("Students can only view or update their own assigned tasks.");

            return task;
        }

        private static bool IsSupportedStudentStatus(string? status) =>
            string.Equals(status, "TO_DO", StringComparison.Ordinal) ||
            string.Equals(status, "IN_PROGRESS", StringComparison.Ordinal) ||
            string.Equals(status, "DONE", StringComparison.Ordinal);

        private static void ValidateTaskFields(string title, DateOnly? dueDate)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new BadRequestException("Task title is required.");

            if (dueDate.HasValue && dueDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
                throw new BadRequestException("Task deadline cannot be in the past.");
        }

        private static string? NormalizeDescription(string? description) =>
            string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        private static InternshipTaskResponseDto ToResponse(InternshipTask task) => new()
        {
            Id = task.Id,
            MentorId = task.MentorId,
            StudentId = task.StudentId,
            StudentCode = task.Student.StudentCode,
            StudentName = task.Student.FullName,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            Status = task.Status,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
