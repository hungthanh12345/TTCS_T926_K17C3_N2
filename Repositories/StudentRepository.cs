using InternshipManagementApi.Common.Models;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Student;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Repositories
{
    public interface IStudentRepository
    {
        Task<Student?> GetByIdAsync(int id);
        Task<Student?> GetByIdWithDetailsAsync(int id);
        Task<Student?> GetByStudentCodeAsync(string studentCode);
        Task<bool> ExistsByStudentCodeAsync(string studentCode, int? excludeId = null);
        Task<bool> ExistsByUserIdAsync(int userId, int? excludeId = null);
        Task<PagedResult<Student>> SearchAsync(StudentSearchFilterDto filter);
        Task<Student> AddAsync(Student student);
        Task UpdateAsync(Student student);
    }

    public class StudentRepository : IStudentRepository
    {
        private readonly AppDbContext _context;

        public StudentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Student?> GetByIdAsync(int id)
        {
            return await _context.Students.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Student?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Mentor)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Student?> GetByStudentCodeAsync(string studentCode)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Mentor)
                .FirstOrDefaultAsync(s => s.StudentCode.ToLower() == studentCode.Trim().ToLower());
        }

        public async Task<bool> ExistsByStudentCodeAsync(string studentCode, int? excludeId = null)
        {
            var query = _context.Students.AsQueryable();
            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }
            return await query.AnyAsync(s => s.StudentCode.ToLower() == studentCode.Trim().ToLower());
        }

        public async Task<bool> ExistsByUserIdAsync(int userId, int? excludeId = null)
        {
            var query = _context.Students.AsQueryable();
            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }
            return await query.AnyAsync(s => s.UserId == userId);
        }

        public async Task<PagedResult<Student>> SearchAsync(StudentSearchFilterDto filter)
        {
            var query = _context.Students
                .Include(s => s.User)
                .Include(s => s.Mentor)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.University))
            {
                var uni = filter.University.Trim().ToLower();
                query = query.Where(s => s.University.ToLower().Contains(uni));
            }

            if (!string.IsNullOrWhiteSpace(filter.Major))
            {
                var major = filter.Major.Trim().ToLower();
                query = query.Where(s => s.Major.ToLower().Contains(major));
            }

            if (!string.IsNullOrWhiteSpace(filter.FullName))
            {
                var name = filter.FullName.Trim().ToLower();
                query = query.Where(s => s.FullName.ToLower().Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim().ToLower();
                query = query.Where(s => s.FullName.ToLower().Contains(kw) ||
                                         s.StudentCode.ToLower().Contains(kw) ||
                                         s.University.ToLower().Contains(kw) ||
                                         s.Major.ToLower().Contains(kw));
            }

            var totalItems = await query.CountAsync();

            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 100);

            var items = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Student>(items, totalItems, page, pageSize);
        }

        public async Task<Student> AddAsync(Student student)
        {
            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();
            return student;
        }

        public async Task UpdateAsync(Student student)
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }
    }
}
