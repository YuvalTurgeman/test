using test.Data;
using test.Models;
using Microsoft.EntityFrameworkCore;


namespace test.Data
{
    public class DiscountDAL
    {
        private readonly ApplicationDbContext _context;

        public DiscountDAL(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DiscountModel> CreateDiscountAsync(DiscountModel discount)
        {
            try
            {
                Console.WriteLine($"Creating discount: {discount.BookId}, {discount.DiscountAmount}");
                await _context.Discounts.AddAsync(discount);
                await _context.SaveChangesAsync();
                return discount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating discount: {ex.Message}");
                throw;
            }
        }

        public async Task<DiscountModel> GetDiscountAsync(int id)
        {
            return await _context.Discounts
                .Include(d => d.Book)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<DiscountModel>> GetActiveDiscountsAsync()
        {
            return await _context.Discounts
                .Include(d => d.Book)
                .Where(d => d.IsActive && d.EndDate > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<List<DiscountModel>> GetDiscountsByBookAsync(int bookId)
        {
            return await _context.Discounts
                .Include(d => d.Book)
                .Where(d => d.BookId == bookId && d.IsActive && d.EndDate > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<DiscountModel> UpdateDiscountAsync(DiscountModel discount)
        {
            _context.Discounts.Update(discount);
            await _context.SaveChangesAsync();
            return discount;
        }

        public async Task DeleteDiscountAsync(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount != null)
            {
                _context.Discounts.Remove(discount);
                await _context.SaveChangesAsync();
            }
        }
        // Add these methods to DiscountDAL
        public async Task<List<DiscountModel>> GetAllDiscountsAsync()
        {
            return await _context.Discounts
                .Include(d => d.Book)
                .ToListAsync();
        }

        public async Task<decimal> CalculateDiscountedPriceAsync(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null || !book.PurchasePrice.HasValue)
                return 0;

            var activeDiscount = await _context.Discounts
                .Where(d => d.BookId == bookId && 
                            d.IsActive && 
                            d.EndDate > DateTime.UtcNow)
                .OrderByDescending(d => d.DiscountAmount)
                .FirstOrDefaultAsync();

            if (activeDiscount == null)
                return book.PurchasePrice.Value;

            return book.PurchasePrice.Value * (1 - activeDiscount.DiscountAmount / 100);
        }

// Add validation method
        public async Task<bool> HasActiveDiscountAsync(int bookId)
        {
            return await _context.Discounts
                .AnyAsync(d => d.BookId == bookId && 
                               d.IsActive && 
                               d.EndDate > DateTime.UtcNow);
        }
    }
}