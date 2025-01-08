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

        public async Task UpdateCartItemPricesAsync(CartItemModel cartItem)
        {
            var book = await _context.Books
                .Include(b => b.Discounts)
                .FirstOrDefaultAsync(b => b.Id == cartItem.BookId);

            if (book == null) return;

            var now = DateTime.UtcNow;
            var activeDiscount = book.Discounts
                .FirstOrDefault(d => d.IsActive && 
                                   d.StartDate <= now && 
                                   d.EndDate >= now);

            if (activeDiscount != null)
            {
                var basePrice = cartItem.IsBorrow ? book.BorrowPrice : book.PurchasePrice;
                if (basePrice.HasValue)
                {
                    cartItem.FinalPrice = basePrice.Value * (1 - activeDiscount.DiscountAmount / 100);
                    cartItem.DiscountId = activeDiscount.Id;
                }
            }
            else
            {
                cartItem.FinalPrice = cartItem.IsBorrow ? book.BorrowPrice : book.PurchasePrice;
                cartItem.DiscountId = null;
            }

            _context.CartItems.Update(cartItem);
            await _context.SaveChangesAsync();
        }

        // Create
        public async Task<CartItemModel> AddItemAsync(CartItemModel item)
        {
            // Validate book exists
            var book = await _context.Books
                .Include(b => b.Discounts)
                .FirstOrDefaultAsync(b => b.Id == item.BookId);

            if (book == null)
                throw new KeyNotFoundException($"Book with ID {item.BookId} not found");

            // Validate book availability and borrowing rules
            if (item.IsBorrow)
            {
                if (book.IsBuyOnly)
                    throw new InvalidOperationException("This book is only available for purchase");

                // Check if user has reached borrow limit
                var borrowCount = await _context.Borrows
                    .CountAsync(b => b.UserId == item.ShoppingCart.UserId && !b.IsReturned);
                if (borrowCount >= 3)
                    throw new InvalidOperationException("Cannot borrow more than 3 books");

                // Check if book has available copies
                var activeBorrows = await _context.Borrows
                    .CountAsync(b => b.BookId == item.BookId && !b.IsReturned);
                if (activeBorrows >= book.TotalCopies)
                    throw new InvalidOperationException("No copies available for borrowing");

                // Update available copies
                book.AvailableCopies = book.TotalCopies - activeBorrows - 1;
            }

            // Check for active discount
            var now = DateTime.UtcNow;
            var activeDiscount = book.Discounts
                .FirstOrDefault(d => d.IsActive && 
                                   d.StartDate <= now && 
                                   d.EndDate >= now);

            // Calculate final price
            var basePrice = item.IsBorrow ? book.BorrowPrice : book.PurchasePrice;
            if (activeDiscount != null && basePrice.HasValue)
            {
                item.DiscountId = activeDiscount.Id;
                item.FinalPrice = basePrice.Value * (1 - activeDiscount.DiscountAmount / 100);
            }
            else
            {
                item.FinalPrice = basePrice;
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
                    .ThenInclude(b => b.Discounts)
                .Include(ci => ci.Discount)
                .FirstOrDefaultAsync(ci => ci.Id == id);
        }

        public async Task<List<CartItemModel>> GetByShoppingCartIdAsync(int shoppingCartId)
        {
            var items = await _context.CartItems
                .Include(ci => ci.Book)
                    .ThenInclude(b => b.Discounts)
                .Include(ci => ci.Discount)
                .Where(ci => ci.ShoppingCartId == shoppingCartId)
                .ToListAsync();

            // Update prices with current discounts
            foreach (var item in items)
            {
                await UpdateCartItemPricesAsync(item);
            }

            return items;
        }

        // Update
        public async Task<CartItemModel> UpdateAsync(CartItemModel item)
        {
            await UpdateCartItemPricesAsync(item);
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
                .Include(sc => sc.CartItems)
                    .ThenInclude(ci => ci.Book)
                        .ThenInclude(b => b.Discounts)
                .Include(sc => sc.CartItems)
                    .ThenInclude(ci => ci.Discount)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task CreateAsync(ShoppingCartModel cart)
        {
            await _context.ShoppingCarts.AddAsync(cart);
            await _context.SaveChangesAsync();
        }
    }
}