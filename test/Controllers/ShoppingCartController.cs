using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Stripe.Checkout;
using test.Data;
using test.Models;
using test.Services;

namespace test.Controllers
{
    [Authorize]
    public class ShoppingCartController : BaseController
    {
        private readonly ShoppingCartDAL _cartDAL;
        private readonly CartItemDAL _cartItemDAL;
        private readonly BookDAL _bookDAL;
        private readonly PaymentService _paymentService;
        private readonly BorrowDAL _borrowDAL;
        private readonly WaitingListDAL _waitingListDAL;
        
        public ShoppingCartController(
            ShoppingCartDAL cartDAL,
            CartItemDAL cartItemDAL,
            BookDAL bookDAL,
            PaymentService paymentService,
            BorrowDAL borrowDAL,
            WaitingListDAL waitingListDAL) 
        {
            _cartDAL = cartDAL;
            _cartItemDAL = cartItemDAL;
            _bookDAL = bookDAL;
            _paymentService = paymentService;
            _borrowDAL = borrowDAL;
            _waitingListDAL = waitingListDAL;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var cart = await _cartDAL.GetOrCreateCartAsync(userId);
            return View(cart);
        }

        [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int bookId, bool isBorrow)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var cart = await _cartDAL.GetOrCreateCartAsync(userId);
            var book = await _bookDAL.GetBookByIdAsync(bookId);

            if (book == null)
            {
                TempData["Error"] = "Book not found.";
                return RedirectToAction("Index", "Book");
            }

            if (isBorrow)
            {
                // Check if book is available for borrowing
                var availableCopies = await _bookDAL.GetAvailableCopiesAsync(bookId);
                var currentBorrows = await _borrowDAL.GetActiveUserBorrowsAsync(userId);
                var cartBorrows = cart.CartItems.Where(i => i.IsBorrow).Sum(i => i.Quantity);

                if (currentBorrows.Count + cartBorrows >= 3)
                {
                    TempData["Error"] = "You can only borrow up to 3 books at a time.";
                    return RedirectToAction("Index", "Book");
                }

                if (availableCopies <= 0)
                {
                    // Add to waiting list
                    await _waitingListDAL.AddToWaitingListAsync(userId, bookId);
                    TempData["Success"] = "Added to waiting list as the book is currently unavailable.";
                    return RedirectToAction("Index", "Book");
                }
            }

            // Check if item already exists in cart
            var existingItem = cart.CartItems.FirstOrDefault(i => i.BookId == bookId);
            if (existingItem != null)
            {
                existingItem.IsBorrow = isBorrow;
                existingItem.FinalPrice = isBorrow ? book.BorrowPrice : book.PurchasePrice;
                await _cartItemDAL.UpdateAsync(existingItem);
                TempData["Success"] = $"Cart updated for {(isBorrow ? "borrowing" : "purchasing")}.";
            }
            else
            {
                var cartItem = new CartItemModel
                {
                    BookId = bookId,
                    ShoppingCartId = cart.Id,
                    IsBorrow = isBorrow,
                    DateAdded = DateTime.UtcNow,
                    FinalPrice = isBorrow ? book.BorrowPrice : book.PurchasePrice,
                    Quantity = 1
                };
                await _cartItemDAL.AddItemAsync(cartItem);
                TempData["Success"] = $"Book added to cart for {(isBorrow ? "borrowing" : "purchasing")}.";
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Failed to add item to cart.";
            return RedirectToAction("Index", "Book");
        }
    }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            try
            {
                await _cartItemDAL.DeleteAsync(id);
                TempData["Success"] = "Item removed from cart.";
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["Error"] = "Failed to remove item from cart.";
                return RedirectToAction("Index");
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCart(Dictionary<int, CartItemModel> updates)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var cart = await _cartDAL.GetByUserIdAsync(userId);

                if (cart == null)
                {
                    TempData["Error"] = "Cart not found.";
                    return RedirectToAction("Index");
                }

                foreach (var item in cart.CartItems)
                {
                    if (updates.TryGetValue(item.Id, out var update))
                    {
                        var book = await _bookDAL.GetBookByIdAsync(item.BookId);
                
                        // Update the item properties
                        item.IsBorrow = update.IsBorrow;
                        item.Quantity = update.Quantity;
                        item.FinalPrice = update.IsBorrow ? book.BorrowPrice : book.PurchasePrice;

                        // If changing to borrow, validate borrow limit
                        if (update.IsBorrow)
                        {
                            var currentBorrows = await _borrowDAL.GetActiveUserBorrowsAsync(userId);
                            var cartBorrows = cart.CartItems
                                .Where(i => i.IsBorrow && i.Id != item.Id)
                                .Sum(i => i.Quantity);

                            if (currentBorrows.Count + cartBorrows + update.Quantity > 3)
                            {
                                TempData["Error"] = "You can only borrow up to 3 books at a time.";
                                return RedirectToAction("Index");
                            }
                        }

                        await _cartItemDAL.UpdateAsync(item);
                    }
                }

                TempData["Success"] = "Cart updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to update cart.";
                return RedirectToAction("Index");
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var cartItem = await _cartItemDAL.GetByIdAsync(id);
        
                if (cartItem == null || cartItem.ShoppingCart.UserId != userId)
                {
                    TempData["Error"] = "Invalid item";
                    return RedirectToAction("Index");
                }

                // Check borrow limit
                if (cartItem.IsBorrow)
                {
                    var currentBorrows = await _borrowDAL.GetActiveUserBorrowsAsync(userId);
                    var cartBorrows = (await _cartDAL.GetByUserIdAsync(userId))
                        .CartItems
                        .Where(i => i.IsBorrow && i.Id != id)
                        .Sum(i => i.Quantity);

                    if (currentBorrows.Count + cartBorrows + quantity > 3)
                    {
                        TempData["Error"] = "You can only borrow up to 3 books at a time.";
                        return RedirectToAction("Index");
                    }
                }

                cartItem.Quantity = quantity;
                await _cartItemDAL.UpdateAsync(cartItem);
        
                TempData["Success"] = "Quantity updated successfully.";
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["Error"] = "Failed to update quantity.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var cart = await _cartDAL.GetByUserIdAsync(userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    return BadRequest(new { message = "Cart is empty." });
                }

                // Validate borrow limit
                if (cart.CartItems.Count(i => i.IsBorrow) > 0)
                {
                    var currentBorrows = await _borrowDAL.GetActiveUserBorrowsAsync(userId);
                    if (currentBorrows.Count + cart.CartItems.Count(i => i.IsBorrow) > 3)
                    {
                        return BadRequest(new { message = "You can only borrow up to 3 books at a time." });
                    }
                }

                var lineItems = new List<SessionLineItemOptions>();
                foreach (var item in cart.CartItems)
                {
                    lineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Digireads eBooks",
                            },
                            UnitAmount = Convert.ToInt64(item.FinalPrice * 100)
                        },
                        Quantity = 1
                    });
                }

                var sessionUrl = await _paymentService.CreateCheckoutSessionAsync(
                    lineItems,
                    "https://localhost:7151/ShoppingCart/Success",
                    "https://localhost:7151/ShoppingCart/Cancel"
                );

                return Ok(new { url = sessionUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to create checkout session." });
            }
        }

        public async Task<IActionResult> Success()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var cart = await _cartDAL.GetByUserIdAsync(userId);

                // Process each cart item
                foreach (var item in cart.CartItems)
                {
                    if (item.IsBorrow)
                    {
                        var borrow = new BorrowModel
                        {
                            BookId = item.BookId,
                            UserId = userId,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddDays(30),
                            BorrowPrice = item.FinalPrice ?? 0
                        };
                        await _borrowDAL.CreateBorrowAsync(borrow);
                        await _bookDAL.UpdateAvailableCopiesAsync(item.BookId);
                    }
                    // Handle purchases if needed
                }

                // Clear the cart
                await _cartDAL.ClearCartAsync(userId);
                
                ViewBag.Message = "Your transaction was successful!";
                return View();
            }
            catch (Exception)
            {
                ViewBag.Message = "There was an error processing your transaction.";
                return View();
            }
        }

        public IActionResult Cancel()
        {
            ViewBag.Message = "Your transaction was canceled.";
            return View();
        }
    }
}