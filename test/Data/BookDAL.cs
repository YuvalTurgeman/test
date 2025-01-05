using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Data
{
    public class BookDAL
    {
        private readonly ApplicationDbContext _context;

        public BookDAL(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create
        public async Task<BookModel> CreateBookAsync(BookModel book)
        {
            await _context.Books.AddAsync(book);
            await _context.SaveChangesAsync();
            return book;
        }

        // Read
        public async Task<List<BookModel>> GetAllBooksAsync()
        {
            return await _context.Books
                .Include(b => b.Discounts)
                .Include(b => b.Borrows)
                .Include(b => b.WaitingList)
                .ToListAsync();
        }

        public async Task<BookModel> GetBookByIdAsync(int id)
        {
            return await _context.Books
                .Include(b => b.Discounts)
                .Include(b => b.Borrows)
                .Include(b => b.WaitingList)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<bool> ExistsByTitleAndAuthorAsync(string title, string author)
        {
            return await _context.Books.AnyAsync(b => b.Title == title && b.Author == author);
        }

        // Filter and Sort
        public async Task<List<BookModel>> GetBooksAsync(
            string searchTitle = null,
            string searchAuthor = null,
            int? searchYear = null,
            bool? discountedOnly = null,
            string sortBy = null,
            bool ascending = true)
        {
            var query = _context.Books.AsQueryable();

            // Filter by title
            if (!string.IsNullOrEmpty(searchTitle))
                query = query.Where(b => EF.Functions.Like(b.Title, $"%{searchTitle}%"));

            // Filter by author
            if (!string.IsNullOrEmpty(searchAuthor))
                query = query.Where(b => EF.Functions.Like(b.Author, $"%{searchAuthor}%"));

            // Filter by year published
            if (searchYear.HasValue)
                query = query.Where(b => b.YearPublished == searchYear.Value);

            // Filter by discounted books
            if (discountedOnly.HasValue && discountedOnly.Value)
                query = query.Where(b => b.Discounts.Any(d => d.IsActive));

            // Sorting
            query = sortBy switch
            {
                "PurchasePrice" => ascending ? query.OrderBy(b => b.PurchasePrice) : query.OrderByDescending(b => b.PurchasePrice),
                "BorrowPrice" => ascending ? query.OrderBy(b => b.BorrowPrice) : query.OrderByDescending(b => b.BorrowPrice),
                _ => query // No sorting
            };

            return await query
                .Include(b => b.Discounts)
                .Include(b => b.Borrows)
                .Include(b => b.WaitingList)
                .ToListAsync();
        }

        // Update
        public async Task<BookModel> UpdateBookAsync(BookModel book)
        {
            _context.Books.Update(book);
            await _context.SaveChangesAsync();
            return book;
        }

        // Delete
        public async Task<bool> DeleteBookAsync(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return false;

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            return true;
        }

        // Availability Methods
        public async Task<bool> IsBookAvailableForBorrowAsync(int bookId)
        {
            var book = await GetBookByIdAsync(bookId);
            if (book == null || book.IsBuyOnly)
                return false;

            var activeBorrows = await _context.Borrows
                .CountAsync(b => b.BookId == bookId && !b.IsReturned);

            return activeBorrows < book.TotalCopies;
        }

        public async Task<int> GetAvailableCopiesAsync(int bookId)
        {
            var book = await GetBookByIdAsync(bookId);
            if (book == null)
                return 0;

            var activeBorrows = await _context.Borrows
                .CountAsync(b => b.BookId == bookId && !b.IsReturned);

            return Math.Max(0, book.TotalCopies - activeBorrows);
        }

        public async Task UpdateAvailableCopiesAsync(int bookId)
        {
            var book = await GetBookByIdAsync(bookId);
            if (book != null)
            {
                var activeBorrows = await _context.Borrows
                    .CountAsync(b => b.BookId == bookId && !b.IsReturned);

                book.AvailableCopies = Math.Max(0, book.TotalCopies - activeBorrows);
                await UpdateBookAsync(book);
            }
        }

        public async Task<bool> CanUserBorrowBookAsync(int userId, int bookId)
        {
            if (!await IsBookAvailableForBorrowAsync(bookId))
                return false;

            var activeUserBorrows = await _context.Borrows
                .CountAsync(b => b.UserId == userId && !b.IsReturned);

            return activeUserBorrows < 3;
        }
    }
}
