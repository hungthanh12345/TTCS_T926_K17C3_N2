using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Data;
using InternshipManagementApi.DTOs.Student;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Services
{
    public interface IStudentScheduleService
    {
        Task<IReadOnlyList<StudentScheduleEventDto>> GetMyScheduleAsync(
            int userId,
            CancellationToken cancellationToken = default);
    }

    public sealed class StudentScheduleService : IStudentScheduleService
    {
        private readonly AppDbContext _db;

        public StudentScheduleService(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<StudentScheduleEventDto>> GetMyScheduleAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var hasStudentProfile = await _db.Students
                .AsNoTracking()
                .AnyAsync(student => student.UserId == userId, cancellationToken);

            if (!hasStudentProfile)
                throw new NotFoundException("No student profile is linked to this account.");

            return await _db.Tasks
                .AsNoTracking()
                .Where(task => task.Student.UserId == userId && task.DueDate.HasValue)
                .OrderBy(task => task.DueDate)
                .ThenBy(task => task.Id)
                .Select(task => new StudentScheduleEventDto
                {
                    TaskId = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    DueDate = task.DueDate!.Value,
                    Status = task.Status
                })
                .ToListAsync(cancellationToken);
        }
    }
}
