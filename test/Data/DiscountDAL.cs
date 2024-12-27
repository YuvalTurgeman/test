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
            await _context.Discounts.AddAsync(discount);
            await _context.SaveChangesAsync();
            return discount;
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
    }
}