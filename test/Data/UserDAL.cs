using Microsoft.EntityFrameworkCore;
using test.Models;
using test.Data;

namespace test.Data
{
    public class UserDAL
    {
        private readonly ApplicationDbContext _context;

        public UserDAL(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create
        public async Task<User> CreateUserAsync(User user)
        {
            await _context.users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        // Read
        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _context.users.FindAsync(id);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _context.users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.users.ToListAsync();
        }

        public async Task<List<User>> GetUsersByPermissionAsync(UserPermission permission)
        {
            return await _context.users
                .Where(u => u.Permission == permission)
                .ToListAsync();
        }

        // Update
        public async Task<User> UpdateUserAsync(User user)
        {
            _context.users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        // Delete
        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.users.FindAsync(id);
            if (user == null)
                return false;

            _context.users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        // Authentication methods
        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            var user = await _context.users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
                return false;

            // Note: In a real application, you should use proper password hashing
            return user.Password == password;
        }

        // Validation methods
        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            return !await _context.users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> IsUsernameUniqueAsync(string username)
        {
            return !await _context.users
                .AnyAsync(u => u.Username.ToLower() == username.ToLower());
        }
    }
}