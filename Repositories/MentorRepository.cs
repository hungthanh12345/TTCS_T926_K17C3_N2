using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Repositories
{
    public interface IMentorRepository
    {
        Task<Mentor?> GetByIdAsync(int id);
        Task<Mentor?> GetByIdWithDetailsAsync(int id);
        Task<Mentor?> GetByUserIdAsync(int userId);
        Task<bool> ExistsByUserIdAsync(int userId, int? excludeId = null);
        Task<IEnumerable<Mentor>> GetAllWithDetailsAsync();
        Task<Mentor> AddAsync(Mentor mentor);
        Task UpdateAsync(Mentor mentor);
    }

    public class MentorRepository : IMentorRepository
    {
        private readonly AppDbContext _context;

        public MentorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Mentor?> GetByIdAsync(int id)
        {
            return await _context.Mentors.FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<Mentor?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Mentors
                .Include(m => m.User)
                .Include(m => m.Students)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<Mentor?> GetByUserIdAsync(int userId)
        {
            return await _context.Mentors
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.UserId == userId);
        }

        public async Task<bool> ExistsByUserIdAsync(int userId, int? excludeId = null)
        {
            var query = _context.Mentors.AsQueryable();
            if (excludeId.HasValue)
            {
                query = query.Where(m => m.Id != excludeId.Value);
            }
            return await query.AnyAsync(m => m.UserId == userId);
        }

        public async Task<IEnumerable<Mentor>> GetAllWithDetailsAsync()
        {
            return await _context.Mentors
                .Include(m => m.User)
                .Include(m => m.Students)
                .AsNoTracking()
                .OrderBy(m => m.Id)
                .ToListAsync();
        }

        public async Task<Mentor> AddAsync(Mentor mentor)
        {
            await _context.Mentors.AddAsync(mentor);
            await _context.SaveChangesAsync();
            return mentor;
        }

        public async Task UpdateAsync(Mentor mentor)
        {
            _context.Mentors.Update(mentor);
            await _context.SaveChangesAsync();
        }
    }
}
