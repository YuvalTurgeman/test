using Microsoft.EntityFrameworkCore;
using test.Models;
using test.Data;
using test.Enums;
using test.Helpers;

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
            return await _context.users
                .Include(u => u.Purchases)
                    .ThenInclude(p => p.Book)
                .Include(u => u.Borrows)
                    .ThenInclude(b => b.Book)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.users
                .Include(u => u.Purchases)
                .Include(u => u.Borrows)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _context.users
                .Include(u => u.Purchases)
                .Include(u => u.Borrows)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.users
                .Include(u => u.Purchases)
                .Include(u => u.Borrows)
                .ToListAsync();
        }

        public async Task<List<User>> GetUsersByPermissionAsync(UserPermission permission)
        {
            return await _context.users
                .Include(u => u.Purchases)
                .Include(u => u.Borrows)
                .Where(u => u.Permission == permission)
                .ToListAsync();
        }

        // Update
        public async Task<User> UpdateUserAsync(User user)
        {
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return user;
        }

        // Delete
        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.users
                .Include(u => u.Purchases)
                .Include(u => u.Borrows)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return false;

            // Optional: Add logic to handle related records
            // For example, you might want to check if the user has active borrows

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

            // 🔁 Replace BCrypt with SHA-256 hash + salt
            string hashedInput = HashHelper.HashPassword(password, user.Salt);
            return user.Password == hashedInput;
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

        // Statistics methods
        public async Task<int> GetUserPurchaseCountAsync(int userId)
        {
            return await _context.users
                .Where(u => u.Id == userId)
                .SelectMany(u => u.Purchases)
                .CountAsync();
        }

        public async Task<int> GetUserActiveBorrowsCountAsync(int userId)
        {
            return await _context.users
                .Where(u => u.Id == userId)
                .SelectMany(u => u.Borrows)
                .CountAsync(b => !b.IsReturned);
        }

        // Profile management
        public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
        {
            var user = await _context.users.FindAsync(userId);
            if (user == null)
                return false;

            // 🔁 Replace BCrypt with SHA-256 + generate new salt
            string newSalt = HashHelper.GenerateSalt();
            string hashedPassword = HashHelper.HashPassword(newPassword, newSalt);

            user.Salt = newSalt;
            user.Password = hashedPassword;

            await _context.SaveChangesAsync();
            return true;
        }

        
        // Reset Password Tokens
        public async Task SaveResetTokenAsync(string email, string token, DateTime expiration)
        {
            var user = await _context.users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null) return;

            user.ResetToken = token;
            user.ResetTokenExpires = expiration;
            await _context.SaveChangesAsync();
        }

        public async Task<User> ValidateResetTokenAsync(string email, string token)
        {
            return await _context.users.FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email.ToLower() &&
                u.ResetToken == token &&
                u.ResetTokenExpires > DateTime.UtcNow);
        }

    }
}