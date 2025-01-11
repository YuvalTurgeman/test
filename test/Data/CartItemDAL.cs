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
            try
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

                if (cartItem.IsBorrow)
                {
                    if (!book.BorrowPrice.HasValue)
                        throw new InvalidOperationException("Book does not have a borrow price set");

                    cartItem.FinalPrice = activeDiscount != null
                        ? book.BorrowPrice.Value * (1 - activeDiscount.DiscountAmount / 100)
                        : book.BorrowPrice.Value;
                }
                else
                {
                    if (!book.PurchasePrice.HasValue)
                        throw new InvalidOperationException("Book does not have a purchase price set");

                    cartItem.FinalPrice = activeDiscount != null
                        ? book.PurchasePrice.Value * (1 - activeDiscount.DiscountAmount / 100)
                        : book.PurchasePrice.Value;
                }

                cartItem.DiscountId = activeDiscount?.Id;

                // Ensure borrow items have quantity of 1
                if (cartItem.IsBorrow)
                {
                    cartItem.Quantity = 1;
                }

                _context.CartItems.Update(cartItem);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateCartItemPricesAsync: {ex.Message}");
                throw;
            }
        }


       public async Task<CartItemModel> AddItemAsync(CartItemModel item)
{
    try
    {
        // Validate book exists
        var book = await _context.Books
            .Include(b => b.Discounts)
            .FirstOrDefaultAsync(b => b.Id == item.BookId);

        if (book == null)
            throw new KeyNotFoundException($"Book with ID {item.BookId} not found");

        // Validate borrowing rules
        if (item.IsBorrow)
        {
            if (book.IsBuyOnly)
                throw new InvalidOperationException("This book is only available for purchase");

            // Check if user already has this book borrowed
            var hasBookBorrowed = await _context.Borrows
                .AnyAsync(b => b.UserId == item.ShoppingCart.UserId && 
                             b.BookId == item.BookId && 
                             !b.IsReturned);

            if (hasBookBorrowed)
                throw new InvalidOperationException("You already have this book borrowed");

            // Check if the book is already in cart for borrowing
            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.ShoppingCart.UserId == item.ShoppingCart.UserId && 
                                         ci.BookId == item.BookId && 
                                         ci.IsBorrow);

            if (existingCartItem != null)
                throw new InvalidOperationException("This book is already in your cart for borrowing");

            // Check if user has reached borrow limit (3 different books)
            var distinctBorrowedBooks = await _context.Borrows
                .Where(b => b.UserId == item.ShoppingCart.UserId && !b.IsReturned)
                .Select(b => b.BookId)
                .Distinct()
                .CountAsync();

            var distinctBorrowsInCart = await _context.CartItems
                .Where(ci => ci.ShoppingCart.UserId == item.ShoppingCart.UserId && 
                           ci.IsBorrow && 
                           ci.BookId != item.BookId)
                .Select(ci => ci.BookId)
                .Distinct()
                .CountAsync();

            if (distinctBorrowedBooks + distinctBorrowsInCart >= 3)
                throw new InvalidOperationException("You can only borrow up to 3 different books at a time");

            // Check if book has available copies
            var activeBorrows = await _context.Borrows
                .CountAsync(b => b.BookId == item.BookId && !b.IsReturned);

            if (activeBorrows >= book.TotalCopies)
                throw new InvalidOperationException("No copies available for borrowing");

            // Force quantity to 1 for borrow items
            item.Quantity = 1;
        }

        // Calculate final price with any active discounts
        var now = DateTime.UtcNow;
        var activeDiscount = book.Discounts
            .FirstOrDefault(d => d.IsActive && 
                               d.StartDate <= now && 
                               d.EndDate >= now);

        if (item.IsBorrow)
        {
            if (!book.BorrowPrice.HasValue)
                throw new InvalidOperationException("Book does not have a borrow price set");

            item.FinalPrice = activeDiscount != null
                ? book.BorrowPrice.Value * (1 - activeDiscount.DiscountAmount / 100)
                : book.BorrowPrice.Value;
        }
        else
        {
            if (!book.PurchasePrice.HasValue)
                throw new InvalidOperationException("Book does not have a purchase price set");

            item.FinalPrice = activeDiscount != null
                ? book.PurchasePrice.Value * (1 - activeDiscount.DiscountAmount / 100)
                : book.PurchasePrice.Value;
        }

        item.DiscountId = activeDiscount?.Id;

        await _context.CartItems.AddAsync(item);
        await _context.SaveChangesAsync();
        return item;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in AddItemAsync: {ex.Message}");
        throw;
    }
}

        public async Task<CartItemModel> GetByIdAsync(int id)
        {
            try
            {
                return await _context.CartItems
                    .Include(ci => ci.Book)
                        .ThenInclude(b => b.Discounts)
                    .Include(ci => ci.Discount)
                    .FirstOrDefaultAsync(ci => ci.Id == id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<CartItemModel>> GetByShoppingCartIdAsync(int shoppingCartId)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByShoppingCartIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<CartItemModel> UpdateAsync(CartItemModel item)
        {
            try
            {
                if (item.IsBorrow)
                {
                    // Ensure borrowed items maintain quantity of 1
                    item.Quantity = 1;

                    // Validate borrow limit when updating
                    var distinctBorrowedBooks = await _context.Borrows
                        .Where(b => b.UserId == item.ShoppingCart.UserId && !b.IsReturned)
                        .Select(b => b.BookId)
                        .Distinct()
                        .CountAsync();

                    var distinctBorrowsInCart = await _context.CartItems
                        .Where(ci => ci.ShoppingCart.UserId == item.ShoppingCart.UserId && 
                                   ci.IsBorrow && 
                                   ci.Id != item.Id)
                        .Select(ci => ci.BookId)
                        .Distinct()
                        .CountAsync();

                    if (distinctBorrowedBooks + distinctBorrowsInCart >= 3)
                        throw new InvalidOperationException("You can only borrow up to 3 different books at a time");
                }

                await UpdateCartItemPricesAsync(item);
                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var item = await _context.CartItems.FindAsync(id);
                if (item == null) return false;

                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<ShoppingCartModel> GetByUserIdAsync(int userId)
        {
            try
            {
                return await _context.ShoppingCarts
                    .Include(sc => sc.CartItems)
                        .ThenInclude(ci => ci.Book)
                            .ThenInclude(b => b.Discounts)
                    .Include(sc => sc.CartItems)
                        .ThenInclude(ci => ci.Discount)
                    .FirstOrDefaultAsync(c => c.UserId == userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByUserIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task CreateAsync(ShoppingCartModel cart)
        {
            try
            {
                await _context.ShoppingCarts.AddAsync(cart);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateAsync: {ex.Message}");
                throw;
            }
        }
    }
}