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
            // Validate borrow limit
            var activeUserBorrows = await _context.Borrows
                .CountAsync(b => b.UserId == borrow.UserId && !b.IsReturned);

            if (activeUserBorrows >= 3)
                throw new InvalidOperationException("User has reached maximum borrow limit");

            // Validate book availability
            var activeBookBorrows = await _context.Borrows
                .CountAsync(b => b.BookId == borrow.BookId && !b.IsReturned);

            var book = await _context.Books.FindAsync(borrow.BookId);
            if (book == null || activeBookBorrows >= book.TotalCopies)
                throw new InvalidOperationException("Book is not available for borrowing");

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

        public async Task<List<BorrowModel>> GetActiveUserBorrowsAsync(int userId)
        {
            return await _context.Borrows
                .Include(b => b.Book)
                .Where(b => b.UserId == userId && !b.IsReturned)
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

        // Return book
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

        // Validation methods
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

        public async Task<bool> HasReachedBorrowLimitAsync(int userId)
        {
            var activeCount = await _context.Borrows
                .CountAsync(b => b.UserId == userId && !b.IsReturned);
            return activeCount >= 3;
        }
    }
}