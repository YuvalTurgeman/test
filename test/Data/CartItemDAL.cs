using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Data
{
    public class CartItemDAL
    {
        private readonly ApplicationDbContext _context;

        public CartItemDAL(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create
        public async Task<CartItemModel> AddItemAsync(CartItemModel item)
        {
            // Validate book exists
            var book = await _context.Books.FindAsync(item.BookId);
            if (book == null)
                throw new KeyNotFoundException($"Book with ID {item.BookId} not found");

            // Validate book availability and borrowing rules
            if (item.IsBorrow)
            {
                if (book.IsBuyOnly)
                    throw new InvalidOperationException("This book is only available for purchase");

                // Check if user has reached borrow limit
                var borrowCount = await _context.CartItems
                    .CountAsync(ci => ci.ShoppingCart.UserId == item.ShoppingCart.UserId && ci.IsBorrow);
                if (borrowCount >= 3)
                    throw new InvalidOperationException("Cannot borrow more than 3 books");

                // Check if book has available copies
                var activeBorrows = await _context.Borrows
                    .CountAsync(b => b.BookId == item.BookId && !b.IsReturned);
                if (activeBorrows >= 3)
                    throw new InvalidOperationException("No copies available for borrowing");
            }

            // Calculate final price
            if (item.DiscountId.HasValue)
            {
                var discount = await _context.Discounts.FindAsync(item.DiscountId);
                if (discount != null && discount.IsActive)
                {
                    var basePrice = item.IsBorrow ? book.BorrowPrice : book.PurchasePrice;
                    item.FinalPrice = basePrice * (1 - discount.DiscountAmount / 100);
                }
            }

            await _context.CartItems.AddAsync(item);
            await _context.SaveChangesAsync();
            return item;
        }

        // Read
        public async Task<CartItemModel> GetByIdAsync(int id)
        {
            return await _context.CartItems
                .Include(ci => ci.Book)
                .Include(ci => ci.Discount)
                .FirstOrDefaultAsync(ci => ci.Id == id);
        }

        public async Task<List<CartItemModel>> GetByShoppingCartIdAsync(int shoppingCartId)
        {
            return await _context.CartItems
                .Include(ci => ci.Book)
                .Include(ci => ci.Discount)
                .Where(ci => ci.ShoppingCartId == shoppingCartId)
                .ToListAsync();
        }

        // Update
        public async Task<CartItemModel> UpdateAsync(CartItemModel item)
        {
            _context.CartItems.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }

        // Delete
        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item == null) return false;

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ShoppingCartModel> GetByUserIdAsync(int userId)
        {
            return await _context.ShoppingCarts
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task CreateAsync(ShoppingCartModel cart)
        {
            await _context.ShoppingCarts.AddAsync(cart);
            await _context.SaveChangesAsync();
        }

    }
}