using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Data
{
    public class ShoppingCartDAL
    {
        private readonly ApplicationDbContext _context;

        public ShoppingCartDAL(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create/Get
        public async Task<ShoppingCartModel> GetOrCreateCartAsync(int userId)
        {
            var cart = await _context.ShoppingCarts
                .Include(sc => sc.CartItems)
                    .ThenInclude(ci => ci.Book)
                .Include(sc => sc.CartItems)
                    .ThenInclude(ci => ci.Discount)
                .FirstOrDefaultAsync(sc => sc.UserId == userId);

            if (cart == null)
            {
                cart = new ShoppingCartModel
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.ShoppingCarts.AddAsync(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        // Read
        public async Task<ShoppingCartModel> GetByIdAsync(int id)
        {
            return await _context.ShoppingCarts
                .Include(sc => sc.CartItems)
                    .ThenInclude(ci => ci.Book)
                .Include(sc => sc.CartItems)
                    .ThenInclude(ci => ci.Discount)
                .FirstOrDefaultAsync(sc => sc.Id == id);
        }

        public async Task<ShoppingCartModel> GetByUserIdAsync(int userId)
        {
            return await _context.ShoppingCarts
                .Include(sc => sc.CartItems)
                    .ThenInclude(ci => ci.Book)
                .Include(sc => sc.CartItems)
                    .ThenInclude(ci => ci.Discount)
                .FirstOrDefaultAsync(sc => sc.UserId == userId);
        }

        // Update cart
        public async Task UpdateCartAsync(ShoppingCartModel cart)
        {
            cart.LastUpdated = DateTime.UtcNow;
            _context.Update(cart);
            await _context.SaveChangesAsync();
        }

        // Add item to cart
        public async Task AddItemToCartAsync(CartItemModel item)
        {
            var cart = await GetByIdAsync(item.ShoppingCartId);
            if (cart == null)
                throw new InvalidOperationException("Cart not found");

            cart.CartItems.Add(item);
            cart.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // Remove item from cart
        public async Task RemoveItemFromCartAsync(int cartId, int itemId)
        {
            var cart = await GetByIdAsync(cartId);
            if (cart == null)
                throw new InvalidOperationException("Cart not found");

            var item = cart.CartItems.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                cart.CartItems.Remove(item);
                cart.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        // Get cart summary
        public async Task<CartSummary> GetCartSummaryAsync(int userId)
        {
            var cart = await GetByUserIdAsync(userId);
            if (cart == null) return new CartSummary();

            return new CartSummary
            {
                TotalItems = cart.CartItems.Count,
                BorrowItems = cart.CartItems.Count(ci => ci.IsBorrow),
                BuyItems = cart.CartItems.Count(ci => !ci.IsBorrow),
                TotalPrice = cart.CartItems.Sum(ci => ci.FinalPrice ?? 
                    (ci.IsBorrow ? ci.Book.BorrowPrice : ci.Book.PurchasePrice) ?? 0)
            };
        }

        // Clear cart
        public async Task ClearCartAsync(int userId)
        {
            var cart = await GetByUserIdAsync(userId);
            if (cart != null)
            {
                cart.CartItems.Clear();
                cart.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        // Check if item exists in cart
        public async Task<bool> ItemExistsInCartAsync(int userId, int bookId)
        {
            var cart = await GetByUserIdAsync(userId);
            return cart?.CartItems.Any(i => i.BookId == bookId) ?? false;
        }
    }

    public class CartSummary
    {
        public int TotalItems { get; set; }
        public int BorrowItems { get; set; }
        public int BuyItems { get; set; }
        public decimal TotalPrice { get; set; }
    }
}