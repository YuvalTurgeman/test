using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Data
{
    public class PurchaseDAL
    {
        private readonly ApplicationDbContext _context;

        public PurchaseDAL(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create
        public async Task<PurchaseModel> CreatePurchaseAsync(PurchaseModel purchase)
        {
            await _context.Purchases.AddAsync(purchase);
            await _context.SaveChangesAsync();
            return purchase;
        }

        // Read
        public async Task<PurchaseModel> GetPurchaseByIdAsync(int id)
        {
            return await _context.Purchases
                .Include(p => p.Book)
                .Include(p => p.User)
                .Include(p => p.Discount)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<PurchaseModel>> GetAllPurchasesAsync()
        {
            return await _context.Purchases
                .Include(p => p.Book)
                .Include(p => p.User)
                .Include(p => p.Discount)
                .ToListAsync();
        }

        public async Task<List<PurchaseModel>> GetUserPurchasesAsync(int userId)
        {
            return await _context.Purchases
                .Include(p => p.Book)
                .Include(p => p.Discount)
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<PurchaseModel>> GetBookPurchasesAsync(int bookId)
        {
            return await _context.Purchases
                .Include(p => p.User)
                .Include(p => p.Discount)
                .Where(p => p.BookId == bookId)
                .ToListAsync();
        }

        // Update
        public async Task<PurchaseModel> UpdatePurchaseAsync(PurchaseModel purchase)
        {
            _context.Purchases.Update(purchase);
            await _context.SaveChangesAsync();
            return purchase;
        }

        // Delete
        public async Task<bool> DeletePurchaseAsync(int id)
        {
            var purchase = await _context.Purchases.FindAsync(id);
            if (purchase == null)
                return false;

            _context.Purchases.Remove(purchase);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}