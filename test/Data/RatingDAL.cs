using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Data
{
    public class RatingDAL
{
    private readonly ApplicationDbContext _context;

    public RatingDAL(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanUserRateBook(int userId, int bookId)
    {
        Console.WriteLine($"Checking if user {userId} can rate book {bookId}");

        // Check if user has purchased the book
        var hasPurchased = await _context.Purchases
            .AnyAsync(p => p.UserId == userId && p.BookId == bookId && !p.IsHidden);
        
        Console.WriteLine($"Has purchased: {hasPurchased}");

        // Check if user has borrowed the book
        var hasBorrowed = await _context.Borrows
            .AnyAsync(b => b.UserId == userId && b.BookId == bookId);
        
        Console.WriteLine($"Has borrowed: {hasBorrowed}");

        return hasPurchased || hasBorrowed;
    }

    public async Task<bool> HasUserRatedBook(int userId, int bookId)
    {
        return await _context.Ratings
            .AnyAsync(r => r.UserId == userId && r.BookId == bookId);
    }

    public async Task<RatingModel> AddRating(RatingModel rating)
    {
        rating.CreatedAt = DateTime.UtcNow;
        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync();
        return rating;
    }

    public async Task<double> GetAverageRating(int bookId)
    {
        var ratings = await _context.Ratings
            .Where(r => r.BookId == bookId)
            .Select(r => r.Value)
            .ToListAsync();

        if (!ratings.Any())
            return 0;

        return Math.Round(ratings.Average(), 1);
    }

    public async Task<List<RatingModel>> GetBookRatings(int bookId)
    {
        return await _context.Ratings
            .Where(r => r.BookId == bookId)
            .Include(r => r.Book)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
}
}