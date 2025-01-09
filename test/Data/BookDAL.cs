using Microsoft.EntityFrameworkCore;
using test.Models;
using test.Enums;

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
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<decimal> GetCurrentPriceAsync(int bookId)
        {
            var book = await GetBookByIdAsync(bookId);
            if (book == null || !book.PurchasePrice.HasValue)
                return 0;

            var activeDiscount = await GetActiveDiscountAsync(bookId);

            if (activeDiscount == null)
                return book.PurchasePrice.Value;

            return book.PurchasePrice.Value * (1 - activeDiscount.DiscountAmount / 100);
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

        // // Filter and Sort
        // public async Task<List<BookModel>> GetBooksAsync(
        //     string searchTitle = null,
        //     string searchAuthor = null,
        //     int? searchYear = null,
        //     bool? discountedOnly = null,
        //     Genre? genre = null,
        //     string sortBy = null,
        //     bool ascending = true)
        // {
        //     var query = _context.Books.AsQueryable();
        //
        //     // Apply filters
        //     if (!string.IsNullOrEmpty(searchTitle))
        //         query = query.Where(b => EF.Functions.Like(b.Title, $"%{searchTitle}%"));
        //
        //     if (!string.IsNullOrEmpty(searchAuthor))
        //         query = query.Where(b => EF.Functions.Like(b.Author, $"%{searchAuthor}%"));
        //
        //     if (searchYear.HasValue)
        //         query = query.Where(b => b.YearPublished == searchYear.Value);
        //
        //     if (discountedOnly.HasValue && discountedOnly.Value)
        //         query = query.Where(b => b.Discounts.Any(d => d.IsActive));
        //
        //     if (genre.HasValue)
        //         query = query.Where(b => b.Genre == genre.Value);
        //
        //     // Apply sorting
        //     query = sortBy switch
        //     {
        //         "purchaseprice" => ascending ? query.OrderBy(b => b.PurchasePrice) : query.OrderByDescending(b => b.PurchasePrice),
        //         "borrowprice" => ascending ? query.OrderBy(b => b.BorrowPrice) : query.OrderByDescending(b => b.BorrowPrice),
        //         _ => query
        //     };
        //     
        //     Console.WriteLine(query.ToQueryString());
        //     return await query.ToListAsync();
        // }
        
        // public async Task<List<BookModel>> GetBooksAsync(
        //     string searchTitle = null,
        //     string searchAuthor = null,
        //     int? searchYear = null,
        //     bool? discountedOnly = null,
        //     Genre? genre = null,
        //     string sortBy = null,
        //     bool ascending = true)
        // {
        //     var query = _context.Books.Include(b => b.Discounts).AsQueryable();
        //
        //     // Apply filters
        //     if (!string.IsNullOrEmpty(searchTitle))
        //         query = query.Where(b => EF.Functions.Like(b.Title, $"%{searchTitle}%"));
        //
        //     if (!string.IsNullOrEmpty(searchAuthor))
        //         query = query.Where(b => EF.Functions.Like(b.Author, $"%{searchAuthor}%"));
        //
        //     if (searchYear.HasValue)
        //         query = query.Where(b => b.YearPublished == searchYear.Value);
        //
        //     if (discountedOnly.HasValue)
        //     {
        //         if (discountedOnly.Value)
        //         {
        //             query = query.Where(b =>
        //                 b.Discounts.Any(d =>
        //                     d.IsActive &&
        //                     d.StartDate <= DateTime.UtcNow &&
        //                     d.EndDate >= DateTime.UtcNow));
        //         }
        //         else
        //         {
        //             query = query.Where(b =>
        //                 !b.Discounts.Any(d =>
        //                     d.IsActive &&
        //                     d.StartDate <= DateTime.UtcNow &&
        //                     d.EndDate >= DateTime.UtcNow));
        //         }
        //     }
        //
        //     if (genre.HasValue)
        //         query = query.Where(b => b.Genre == genre.Value);
        //
        //     // Apply sorting
        //     query = sortBy switch
        //     {
        //         "purchaseprice" => ascending
        //             ? query.OrderBy(b => b.PurchasePrice)
        //             : query.OrderByDescending(b => b.PurchasePrice),
        //         "borrowprice" => ascending
        //             ? query.OrderBy(b => b.BorrowPrice)
        //             : query.OrderByDescending(b => b.BorrowPrice),
        //         _ => query
        //     };
        //
        //     return await query.ToListAsync();
        // }

        
        public async Task<List<BookModel>> GetBooksAsync(
    string searchTitle = null,
    string searchAuthor = null,
    int? searchYear = null,
    bool? discountedOnly = null,
    Genre? genre = null,
    string sortBy = null,
    bool ascending = true)
{
    var now = DateTime.UtcNow;
    var query = _context.Books
        .Include(b => b.Discounts)
        .AsQueryable();

    // Apply filters
    if (!string.IsNullOrEmpty(searchTitle))
        query = query.Where(b => EF.Functions.Like(b.Title, $"%{searchTitle}%"));

    if (!string.IsNullOrEmpty(searchAuthor))
        query = query.Where(b => EF.Functions.Like(b.Author, $"%{searchAuthor}%"));

    if (searchYear.HasValue)
        query = query.Where(b => b.YearPublished == searchYear.Value);

    if (discountedOnly.HasValue)
    {
        if (discountedOnly.Value)
        {
            query = query.Where(b => b.Discounts.Any(d =>
                d.IsActive &&
                d.StartDate <= now &&
                d.EndDate >= now));
        }
        else
        {
            query = query.Where(b => !b.Discounts.Any(d =>
                d.IsActive &&
                d.StartDate <= now &&
                d.EndDate >= now));
        }
    }

    if (genre.HasValue)
        query = query.Where(b => b.Genre == genre.Value);

    // Get all books with their current data
    var books = await query.ToListAsync();

    // Calculate effective prices for all books
    var effectivePrices = new Dictionary<int, (decimal? purchase, decimal? borrow)>();
    foreach (var book in books)
    {
        var activeDiscount = book.Discounts
            .FirstOrDefault(d => d.IsActive && d.StartDate <= now && d.EndDate >= now);

        var discountMultiplier = activeDiscount != null ? (1 - activeDiscount.DiscountAmount / 100m) : 1m;

        effectivePrices[book.Id] = (
            purchase: book.PurchasePrice.HasValue ? book.PurchasePrice * discountMultiplier : null,
            borrow: book.BorrowPrice.HasValue ? book.BorrowPrice * discountMultiplier : null
        );
    }

    // Apply sorting based on effective prices
    if (!string.IsNullOrEmpty(sortBy))
    {
        books = sortBy.ToLower() switch
        {
            "purchaseprice" => ascending
                ? books.OrderBy(b => effectivePrices[b.Id].purchase).ToList()
                : books.OrderByDescending(b => effectivePrices[b.Id].purchase).ToList(),
            "borrowprice" => ascending
                ? books.OrderBy(b => effectivePrices[b.Id].borrow).ToList()
                : books.OrderByDescending(b => effectivePrices[b.Id].borrow).ToList(),
            "rating" => books, // Rating sorting is handled separately
            _ => books
        };
    }

    // Handle rating sorting if needed
    if (sortBy == "rating")
    {
        var bookRatings = await _context.Books
            .Select(b => new
            {
                b.Id,
                AvgRating = _context.Ratings
                    .Where(r => r.BookId == b.Id)
                    .Average(r => (double?)r.Value) ?? 0.0
            })
            .ToListAsync();

        var sortedBookIds = ascending
            ? bookRatings.OrderBy(br => br.AvgRating).Select(br => br.Id).ToList()
            : bookRatings.OrderByDescending(br => br.AvgRating).Select(br => br.Id).ToList();

        books = books.OrderBy(b => sortedBookIds.IndexOf(b.Id)).ToList();
    }

    return books;
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
        
        // Create a discount
        public async Task<DiscountModel> CreateDiscountAsync(DiscountModel discount)
        {
            await _context.Discounts.AddAsync(discount);
            await _context.SaveChangesAsync();
            return discount;
        }
        
        // Get the active discount for a book
        public async Task<DiscountModel> GetActiveDiscountAsync(int bookId)
        {
            return await _context.Discounts
                .Where(d => d.BookId == bookId && d.IsActive && 
                            d.StartDate <= DateTime.UtcNow && d.EndDate >= DateTime.UtcNow)
                .FirstOrDefaultAsync();
        }
        
        // Get all discounts for a book
        public async Task<List<DiscountModel>> GetDiscountsByBookIdAsync(int bookId)
        {
            return await _context.Discounts
                .Where(d => d.BookId == bookId)
                .ToListAsync();
        }

// Update a discount
        public async Task<DiscountModel> UpdateDiscountAsync(DiscountModel discount)
        {
            _context.Discounts.Update(discount);
            await _context.SaveChangesAsync();
            return discount;
        }

// Delete a discount
        public async Task<bool> DeleteDiscountAsync(int discountId)
        {
            var discount = await _context.Discounts.FindAsync(discountId);
            if (discount == null)
                return false;

            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<decimal?> GetEffectivePurchasePriceAsync(int bookId)
        {
            var book = await GetBookByIdAsync(bookId);
            if (book == null || !book.PurchasePrice.HasValue)
                return null;
            
            var activeDiscount = book.Discounts
                .FirstOrDefault(d =>
                    d.IsActive &&
                    d.StartDate <= DateTime.UtcNow &&
                    d.EndDate >= DateTime.UtcNow);
            
            if (activeDiscount == null)
                return book.PurchasePrice;
            
            return book.PurchasePrice * (1 - activeDiscount.DiscountAmount / 100);
        }
        
        public async Task<decimal?> GetEffectiveBorrowPriceAsync(int bookId)
        {
            var book = await GetBookByIdAsync(bookId);
            if (book == null || !book.BorrowPrice.HasValue)
                return null;
            
            var activeDiscount = book.Discounts
                .FirstOrDefault(d =>
                    d.IsActive &&
                    d.StartDate <= DateTime.UtcNow &&
                    d.EndDate >= DateTime.UtcNow);
            
            if (activeDiscount == null)
                return book.BorrowPrice;
            
            return book.BorrowPrice * (1 - activeDiscount.DiscountAmount / 100);
        }

        public async Task<List<BookModel>> GetBooksWithEffectivePricesAsync()
        {
            return await _context.Books
                .Include(b => b.Discounts)
                .Select(b => new BookModel
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    PurchasePrice = b.PurchasePrice.HasValue
                        ? b.Discounts.Any(d => d.IsActive && d.StartDate <= DateTime.UtcNow && d.EndDate >= DateTime.UtcNow)
                            ? b.PurchasePrice * (1 - b.Discounts.First().DiscountAmount / 100)
                            : b.PurchasePrice
                        : null,
                    BorrowPrice = b.BorrowPrice, // Add Borrow Price if needed
                    Genre = b.Genre,
                    YearPublished = b.YearPublished,
                    Discounts = b.Discounts // Include discounts if needed for frontend
                })
                .ToListAsync();
        }


        


        
    }
}
