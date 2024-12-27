using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Data
{
    public class BorrowDAL
    {
        private readonly ApplicationDbContext _context;

        public BorrowDAL(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create
        public async Task<BorrowModel> CreateBorrowAsync(BorrowModel borrow)
        {
            await _context.Borrows.AddAsync(borrow);
            await _context.SaveChangesAsync();
            return borrow;
        }

        // Read
        public async Task<BorrowModel> GetBorrowByIdAsync(int id)
        {
            return await _context.Borrows
                .Include(b => b.Book)
                .Include(b => b.User)
                .Include(b => b.Purchases)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<List<BorrowModel>> GetAllBorrowsAsync()
        {
            return await _context.Borrows
                .Include(b => b.Book)
                .Include(b => b.User)
                .ToListAsync();
        }

        public async Task<List<BorrowModel>> GetUserBorrowsAsync(int userId)
        {
            return await _context.Borrows
                .Include(b => b.Book)
                .Where(b => b.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<BorrowModel>> GetBookBorrowsAsync(int bookId)
        {
            return await _context.Borrows
                .Include(b => b.User)
                .Where(b => b.BookId == bookId)
                .ToListAsync();
        }

        public async Task<List<BorrowModel>> GetActiveBorrowsAsync()
        {
            return await _context.Borrows
                .Include(b => b.Book)
                .Include(b => b.User)
                .Where(b => !b.IsReturned && b.EndDate > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<List<BorrowModel>> GetOverdueBorrowsAsync()
        {
            return await _context.Borrows
                .Include(b => b.Book)
                .Include(b => b.User)
                .Where(b => !b.IsReturned && b.EndDate < DateTime.UtcNow)
                .ToListAsync();
        }

        // Update
        public async Task<BorrowModel> UpdateBorrowAsync(BorrowModel borrow)
        {
            _context.Borrows.Update(borrow);
            await _context.SaveChangesAsync();
            return borrow;
        }

        public async Task<BorrowModel> MarkAsReturnedAsync(int id)
        {
            var borrow = await _context.Borrows.FindAsync(id);
            if (borrow != null)
            {
                borrow.IsReturned = true;
                await _context.SaveChangesAsync();
            }
            return borrow;
        }

        // Delete
        public async Task<bool> DeleteBorrowAsync(int id)
        {
            var borrow = await _context.Borrows.FindAsync(id);
            if (borrow == null)
                return false;

            _context.Borrows.Remove(borrow);
            await _context.SaveChangesAsync();
            return true;
        }
        // Add these methods to BorrowDAL
        public async Task<bool> HasActiveBookBorrowAsync(int userId, int bookId)
        {
            return await _context.Borrows
                .AnyAsync(b => b.UserId == userId && 
                               b.BookId == bookId && 
                               !b.IsReturned);
        }

        public async Task<int> GetActiveBookBorrowsCountAsync(int bookId)
        {
            return await _context.Borrows
                .CountAsync(b => b.BookId == bookId && !b.IsReturned);
        }
        

// Add method for returning book with date
        public async Task<BorrowModel> ReturnBookAsync(int id)
        {
            var borrow = await _context.Borrows.FindAsync(id);
            if (borrow != null)
            {
                borrow.IsReturned = true;
                borrow.ReturnedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return borrow;
        }
    }
}