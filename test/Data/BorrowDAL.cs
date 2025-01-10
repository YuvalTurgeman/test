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

        public async Task<BorrowModel> CreateBorrowAsync(BorrowModel borrow)
        {
            try
            {
                // Check if user already has this book borrowed
                var existingBorrow = await _context.Borrows
                    .AnyAsync(b => b.UserId == borrow.UserId && 
                                 b.BookId == borrow.BookId && 
                                 !b.IsReturned);

                if (existingBorrow)
                    throw new InvalidOperationException("User already has this book borrowed");

                // Check total number of different books borrowed
                var distinctBorrowedBooks = await _context.Borrows
                    .Where(b => b.UserId == borrow.UserId && !b.IsReturned)
                    .Select(b => b.BookId)
                    .Distinct()
                    .CountAsync();

                if (distinctBorrowedBooks >= 3)
                    throw new InvalidOperationException("User has reached maximum borrow limit of 3 different books");

                // Validate book availability
                var activeBookBorrows = await _context.Borrows
                    .CountAsync(b => b.BookId == borrow.BookId && !b.IsReturned);

                var book = await _context.Books.FindAsync(borrow.BookId);
                if (book == null || activeBookBorrows >= book.TotalCopies)
                    throw new InvalidOperationException("Book is not available for borrowing");

                // Set borrow period to 30 days
                borrow.StartDate = DateTime.UtcNow;
                borrow.EndDate = borrow.StartDate.AddDays(30);
                borrow.IsReturned = false;

                await _context.Borrows.AddAsync(borrow);
                await _context.SaveChangesAsync();
                return borrow;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateBorrowAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<BorrowModel> GetBorrowByIdAsync(int id)
        {
            try
            {
                return await _context.Borrows
                    .Include(b => b.Book)
                    .Include(b => b.User)
                    .Include(b => b.Purchases)
                    .FirstOrDefaultAsync(b => b.Id == id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBorrowByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<BorrowModel>> GetAllBorrowsAsync()
        {
            try
            {
                return await _context.Borrows
                    .Include(b => b.Book)
                    .Include(b => b.User)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllBorrowsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<BorrowModel>> GetUserBorrowsAsync(int userId)
        {
            try
            {
                return await _context.Borrows
                    .Include(b => b.Book)
                    .Where(b => b.UserId == userId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserBorrowsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<BorrowModel>> GetActiveUserBorrowsAsync(int userId)
        {
            try
            {
                return await _context.Borrows
                    .Include(b => b.Book)
                    .Where(b => b.UserId == userId && 
                               !b.IsReturned && 
                               b.EndDate > DateTime.UtcNow)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetActiveUserBorrowsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<BorrowModel>> GetBookBorrowsAsync(int bookId)
        {
            try
            {
                return await _context.Borrows
                    .Include(b => b.User)
                    .Where(b => b.BookId == bookId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBookBorrowsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<BorrowModel>> GetActiveBorrowsAsync()
        {
            try
            {
                return await _context.Borrows
                    .Include(b => b.Book)
                    .Include(b => b.User)
                    .Where(b => !b.IsReturned && b.EndDate > DateTime.UtcNow)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetActiveBorrowsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<BorrowModel>> GetOverdueBorrowsAsync()
        {
            try
            {
                return await _context.Borrows
                    .Include(b => b.Book)
                    .Include(b => b.User)
                    .Where(b => !b.IsReturned && b.EndDate < DateTime.UtcNow)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetOverdueBorrowsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<BorrowModel> UpdateBorrowAsync(BorrowModel borrow)
        {
            try
            {
                _context.Borrows.Update(borrow);
                await _context.SaveChangesAsync();
                return borrow;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateBorrowAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<BorrowModel> ReturnBookAsync(int id)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ReturnBookAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteBorrowAsync(int id)
        {
            try
            {
                var borrow = await _context.Borrows.FindAsync(id);
                if (borrow == null)
                    return false;

                _context.Borrows.Remove(borrow);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteBorrowAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> HasUserBorrowedBookAsync(int userId, int bookId)
        {
            try
            {
                return await _context.Borrows
                    .AnyAsync(b => b.UserId == userId && 
                                 b.BookId == bookId && 
                                 !b.IsReturned);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HasUserBorrowedBookAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetActiveBookBorrowsCountAsync(int bookId)
        {
            try
            {
                return await _context.Borrows
                    .CountAsync(b => b.BookId == bookId && !b.IsReturned);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetActiveBookBorrowsCountAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> HasReachedBorrowLimitAsync(int userId)
        {
            try
            {
                var distinctBorrowedBooks = await _context.Borrows
                    .Where(b => b.UserId == userId && !b.IsReturned)
                    .Select(b => b.BookId)
                    .Distinct()
                    .CountAsync();
                
                return distinctBorrowedBooks >= 3;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HasReachedBorrowLimitAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetDistinctBorrowedBooksCountAsync(int userId)
        {
            try
            {
                return await _context.Borrows
                    .Where(b => b.UserId == userId && !b.IsReturned)
                    .Select(b => b.BookId)
                    .Distinct()
                    .CountAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDistinctBorrowedBooksCountAsync: {ex.Message}");
                throw;
            }
        }

        public async Task RemoveExpiredBorrowsAsync()
        {
            try
            {
                var expiredBorrows = await _context.Borrows
                    .Where(b => !b.IsReturned && b.EndDate < DateTime.UtcNow)
                    .ToListAsync();

                foreach (var borrow in expiredBorrows)
                {
                    borrow.IsReturned = true;
                    borrow.ReturnedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveExpiredBorrowsAsync: {ex.Message}");
                throw;
            }
        }
    }
}