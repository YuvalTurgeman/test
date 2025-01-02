using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using test.Data;
using test.Models;

namespace test.Controllers
{
    [Authorize]
    public class ShoppingCartController : BaseController
    {
        private readonly ShoppingCartDAL _cartDAL;
        private readonly CartItemDAL _cartItemDAL;
        private readonly BookDAL _bookDAL;

        public ShoppingCartController(ShoppingCartDAL cartDAL, CartItemDAL cartItemDAL, BookDAL bookDAL)
        {
            _cartDAL = cartDAL;
            _cartItemDAL = cartItemDAL;
            _bookDAL = bookDAL;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var cart = await _cartDAL.GetOrCreateCartAsync(userId);
            return View(cart);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int bookId, bool isBorrow)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var cart = await _cartDAL.GetOrCreateCartAsync(userId);

                // Verify book exists
                var book = await _bookDAL.GetBookByIdAsync(bookId);
                if (book == null)
                {
                    TempData["Error"] = "Book not found.";
                    return RedirectToAction("UserHomePage", "Books");
                }

                var cartItem = new CartItemModel
                {
                    BookId = bookId,
                    ShoppingCartId = cart.Id,
                    IsBorrow = isBorrow,
                    DateAdded = DateTime.UtcNow,
                    FinalPrice = isBorrow ? book.BorrowPrice : book.PurchasePrice
                };

                await _cartItemDAL.AddItemAsync(cartItem);
                TempData["Success"] = "Item added to cart successfully.";
                return RedirectToAction("Index", "ShoppingCart");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to add item to cart: {ex.Message}";
                return RedirectToAction("UserHomePage", "Books");
            }
        }

        public class AddToCartRequest
        {
            public int BookId { get; set; }
            public bool IsBorrow { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int bookId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var cart = await _cartDAL.GetByUserIdAsync(userId);
                
                if (cart != null)
                {
                    var itemToRemove = cart.CartItems.FirstOrDefault(i => i.BookId == bookId);
                    if (itemToRemove != null)
                    {
                        await _cartItemDAL.DeleteAsync(itemToRemove.Id);
                        var updatedCart = await _cartDAL.GetByUserIdAsync(userId);
                        return Json(new { 
                            success = true, 
                            cartCount = updatedCart.CartItems.Count,
                            message = "Item removed from cart successfully." 
                        });
                    }
                }

                return Json(new { success = false, message = "Item not found in cart." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to remove item: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                await _cartDAL.ClearCartAsync(userId);
                return Json(new { success = true, message = "Cart cleared successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to clear cart: {ex.Message}" });
            }
        }

        public async Task<IActionResult> Checkout()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var summary = await _cartDAL.GetCartSummaryAsync(userId);
            
            if (summary.TotalItems == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(summary);
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { count = 0 });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var cart = await _cartDAL.GetByUserIdAsync(userId);
            var count = cart?.CartItems.Count ?? 0;

            return Json(new { count });
        }
    }
}