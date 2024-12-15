using test.Data;
using test.Models;
using Microsoft.EntityFrameworkCore;

public class BookDAL
{
    private readonly ApplicationDbContext _context;

    public BookDAL(ApplicationDbContext context)
    {
        _context = context;
    }

    // Get all books
    public async Task<List<BookModel>> GetAllBooksAsync()
    {
        return await _context.Books.ToListAsync();
    }

    // Get a book by ID
    public async Task<BookModel?> GetBookByIdAsync(int id)
    {
        return await _context.Books.FindAsync(id);
    }

    // Add a new book
    public async Task AddBookAsync(BookModel book)
    {
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
    }

    // Update an existing book
    public async Task<bool> UpdateBookAsync(BookModel book)
    {
        var existingBook = await _context.Books.FindAsync(book.Id);
        if (existingBook == null)
        {
            return false; // Book not found
        }

        _context.Entry(existingBook).CurrentValues.SetValues(book);
        await _context.SaveChangesAsync();
        return true;
    }

    // Delete a book by ID
    public async Task<bool> DeleteBookAsync(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return false; // Book not found
        }

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();
        return true;
    }

    // Find books by title (case insensitive)
    public async Task<List<BookModel>> FindBooksByTitleAsync(string title)
    {
        return await _context.Books
            .Where(b => EF.Functions.Like(b.Title, $"%{title}%"))
            .ToListAsync();
    }

    // Filter books by author
    public async Task<List<BookModel>> GetBooksByAuthorAsync(string author)
    {
        return await _context.Books
            .Where(b => b.Author == author)
            .ToListAsync();
    }

    // Filter books by publication year
    public async Task<List<BookModel>> GetBooksByYearAsync(int year)
    {
        return await _context.Books
            .Where(b => b.YearPublished == year)
            .ToListAsync();
    }

    // Count total books
    public async Task<int> GetTotalBooksCountAsync()
    {
        return await _context.Books.CountAsync();
    }
}
