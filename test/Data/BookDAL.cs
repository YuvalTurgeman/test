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
                .ToListAsync();
        }

        public async Task<BookModel> GetBookByIdAsync(int id)
        {
            return await _context.Books
                .Include(b => b.Discounts)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<bool> ExistsByTitleAndAuthorAsync(string title, string author)
        {
            return await _context.Books.AnyAsync(b => b.Title == title && b.Author == author);
        }

        // Get filtered books
        public async Task<List<BookModel>> GetFilteredBooksAsync(string genre = null, decimal? minPrice = null, decimal? maxPrice = null)
        {
            var query = _context.Books.AsQueryable();

            if (!string.IsNullOrEmpty(genre))
                query = query.Where(b => b.Genre == genre);
            
            if (minPrice.HasValue)
                query = query.Where(b => b.PurchasePrice >= minPrice.Value || b.BorrowPrice >= minPrice.Value);
            
            if (maxPrice.HasValue)
                query = query.Where(b => b.PurchasePrice <= maxPrice.Value || b.BorrowPrice <= maxPrice.Value);

            return await query.Include(b => b.Discounts).ToListAsync();
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
    }
}