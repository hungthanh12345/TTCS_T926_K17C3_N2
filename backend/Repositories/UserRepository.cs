using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByIdWithRoleAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
        Task<IEnumerable<User>> GetAllWithRolesAsync();
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
    }

    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByIdWithRoleAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower());
        }

        public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
        {
            var query = _context.Users.AsQueryable();
            if (excludeId.HasValue)
            {
                query = query.Where(u => u.Id != excludeId.Value);
            }
            return await query.AnyAsync(u => u.Email.ToLower() == email.Trim().ToLower());
        }

        public async Task<IEnumerable<User>> GetAllWithRolesAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .OrderBy(u => u.Id)
                .ToListAsync();
        }

        public async Task<User> AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
