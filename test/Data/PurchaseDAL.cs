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

        public async Task<PurchaseModel> CreatePurchaseAsync(PurchaseModel purchase)
        {
            try
            {
                Console.WriteLine($"Creating purchase record for BookId: {purchase.BookId}, UserId: {purchase.UserId}, Quantity: {purchase.Quantity}");

                // Check if the book and user exist
                var book = await _context.Books.FindAsync(purchase.BookId);
                var user = await _context.users.FindAsync(purchase.UserId);

                if (book == null || user == null)
                {
                    throw new InvalidOperationException("Invalid book or user ID");
                }

                // Add the purchase record
                await _context.Purchases.AddAsync(purchase);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Successfully created purchase with ID: {purchase.Id}");
                return purchase;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreatePurchaseAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        public async Task<PurchaseModel> GetPurchaseByIdAsync(int id)
        {
            try
            {
                return await _context.Purchases
                    .Include(p => p.Book)
                    .Include(p => p.User)
                    .Include(p => p.Discount)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPurchaseByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<PurchaseModel>> GetAllPurchasesAsync()
        {
            try
            {
                return await _context.Purchases
                    .Include(p => p.Book)
                    .Include(p => p.User)
                    .Include(p => p.Discount)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllPurchasesAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<PurchaseModel>> GetUserPurchasesAsync(int userId)
        {
            try
            {
                Console.WriteLine($"Getting purchases for user {userId}");

                // Query to get non-hidden purchases with books included
                var purchases = await _context.Purchases
                    .Include(p => p.Book)
                    .Where(p => p.UserId == userId && !p.IsHidden)
                    .OrderByDescending(p => p.PurchaseDate)
                    .ToListAsync();

                Console.WriteLine($"Found {purchases.Count} active purchases for user");
                foreach (var purchase in purchases)
                {
                    Console.WriteLine($"Purchase ID: {purchase.Id}, Book: {purchase.Book?.Title}, Date: {purchase.PurchaseDate}");
                }

                return purchases;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserPurchasesAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<List<PurchaseModel>> GetBookPurchasesAsync(int bookId)
        {
            try
            {
                return await _context.Purchases
                    .Include(p => p.User)
                    .Include(p => p.Discount)
                    .Where(p => p.BookId == bookId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBookPurchasesAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<PurchaseModel>> GetRecentPurchasesAsync(int count = 10)
        {
            try
            {
                return await _context.Purchases
                    .Include(p => p.Book)
                    .Include(p => p.User)
                    .Include(p => p.Discount)
                    .Where(p => !p.IsHidden)
                    .OrderByDescending(p => p.PurchaseDate)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetRecentPurchasesAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<PurchaseModel> UpdatePurchaseAsync(PurchaseModel purchase)
        {
            try
            {
                Console.WriteLine($"Updating purchase {purchase.Id}");
                _context.Purchases.Update(purchase);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Successfully updated purchase {purchase.Id}");
                return purchase;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdatePurchaseAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeletePurchaseAsync(int id)
        {
            try
            {
                var purchase = await _context.Purchases.FindAsync(id);
                if (purchase == null)
                {
                    return false;
                }

                _context.Purchases.Remove(purchase);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeletePurchaseAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> HidePurchaseAsync(int id)
        {
            try
            {
                var purchase = await _context.Purchases.FindAsync(id);
                if (purchase == null)
                {
                    return false;
                }

                purchase.IsHidden = true;
                await _context.SaveChangesAsync();
                Console.WriteLine($"Successfully hidden purchase {id}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HidePurchaseAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> HasUserPurchasedBookAsync(int userId, int bookId)
        {
            try
            {
                return await _context.Purchases
                    .AnyAsync(p => p.UserId == userId && 
                                 p.BookId == bookId && 
                                 !p.IsHidden);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HasUserPurchasedBookAsync: {ex.Message}");
                throw;
            }
        }
    }
}