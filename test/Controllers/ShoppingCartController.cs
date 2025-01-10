using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Stripe.Checkout;
using test.Data;
using test.Models;
using test.Services;

namespace test.Controllers
{
    public class TempPurchaseInfo
    {
        public int BookId { get; set; }
        public int UserId { get; set; }
        public int Quantity { get; set; }
        public bool IsBuyNow { get; set; }
        public decimal Price { get; set; }
    }

    [Authorize(Roles = "Customer")]
    public class ShoppingCartController : BaseController
    {
        private readonly ShoppingCartDAL _cartDAL;
        private readonly CartItemDAL _cartItemDAL;
        private readonly BookDAL _bookDAL;
        private readonly PaymentService _paymentService;
        private readonly BorrowDAL _borrowDAL;
        private readonly WaitingListDAL _waitingListDAL;
        private readonly PurchaseDAL _purchaseDAL;
        private readonly EmailService _emailService;

        private const string TempPurchaseKey = "TempPurchaseInfo";

        public ShoppingCartController(
            ShoppingCartDAL cartDAL,
            CartItemDAL cartItemDAL,
            BookDAL bookDAL,
            PaymentService paymentService,
            BorrowDAL borrowDAL,
            WaitingListDAL waitingListDAL,
            PurchaseDAL purchaseDAL,
            EmailService emailService)
        {
            _cartDAL = cartDAL;
            _cartItemDAL = cartItemDAL;
            _bookDAL = bookDAL;
            _paymentService = paymentService;
            _borrowDAL = borrowDAL;
            _waitingListDAL = waitingListDAL;
            _purchaseDAL = purchaseDAL;
            _emailService = emailService;
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var cart = await _cartDAL.GetOrCreateCartAsync(userId);
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> BuyNow(int bookId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var book = await _bookDAL.GetBookByIdAsync(bookId);

                if (book == null)
                {
                    TempData["Error"] = "Book not found.";
                    return RedirectToAction("UserHomePage", "Books");
                }

                var purchaseInfo = new TempPurchaseInfo
                {
                    BookId = bookId,
                    UserId = userId,
                    Quantity = 1,
                    IsBuyNow = true,
                    Price = book.PurchasePrice ?? 0m
                };

                TempData[TempPurchaseKey] = JsonSerializer.Serialize(purchaseInfo);

                var lineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = book.Title
                            },
                            UnitAmount = Convert.ToInt64(book.PurchasePrice * 100)
                        },
                        Quantity = 1
                    }
                };

                var sessionUrl = await _paymentService.CreateCheckoutSessionAsync(
                    lineItems,
                    Url.Action("Success", "ShoppingCart", null, Request.Scheme),
                    Url.Action("Cancel", "ShoppingCart", null, Request.Scheme)
                );

                return Redirect(sessionUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BuyNow: {ex.Message}");
                TempData["Error"] = "Failed to initiate the purchase.";
                return RedirectToAction("UserHomePage", "Books");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddToCart(int bookId, bool isBorrow)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var book = await _bookDAL.GetBookByIdAsync(bookId);

                if (book == null)
                {
                    TempData["Error"] = "Book not found.";
                    return RedirectToAction("Index", "Book");
                }

                if (isBorrow)
                {
                    // Check if user already has this book borrowed
                    if (await _borrowDAL.HasUserBorrowedBookAsync(userId, bookId))
                    {
                        TempData["Error"] = "You already have this book borrowed.";
                        return RedirectToAction("MyBorrows", "Borrow");
                    }

                    // Check if user has reached borrow limit
                    if (await _borrowDAL.HasReachedBorrowLimitAsync(userId))
                    {
                        TempData["Error"] = "You can only borrow up to 3 different books at a time.";
                        return RedirectToAction("MyBorrows", "Borrow");
                    }

                    var availableCopies = await _bookDAL.GetAvailableCopiesAsync(bookId);
                    if (availableCopies <= 0)
                    {
                        await _waitingListDAL.AddToWaitingListAsync(userId, bookId);
                        TempData["Success"] = "Added to waiting list as the book is currently unavailable.";
                        return RedirectToAction("Index", "Book");
                    }
                }

                var cart = await _cartDAL.GetOrCreateCartAsync(userId);

                var existingItem = cart.CartItems.FirstOrDefault(i => i.BookId == bookId);
                if (existingItem != null)
                {
                    if (isBorrow)
                    {
                        existingItem.Quantity = 1; // Force quantity to 1 for borrow items
                    }

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
                        Quantity = isBorrow ? 1 : 1 // Default quantity, force 1 for borrow
                    };
                    await _cartItemDAL.AddItemAsync(cartItem);
                    TempData["Success"] = $"Book added to cart for {(isBorrow ? "borrowing" : "purchasing")}.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Book");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RemoveFromCart(int id)
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

                var cartItem = cart.CartItems.FirstOrDefault(i => i.Id == id);
                if (cartItem == null)
                {
                    TempData["Error"] = "Item not found in cart.";
                    return RedirectToAction("Index");
                }

                await _cartDAL.RemoveItemFromCartAsync(cart.Id, id);

                TempData["Success"] = "Item removed from cart.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to remove item from cart.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
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

                    // Check if switching to borrow
                    if (update.IsBorrow && !item.IsBorrow)
                    {
                        // Check book availability
                        var availableCopies = await _bookDAL.GetAvailableCopiesAsync(item.BookId);
                        if (availableCopies <= 0)
                        {
                            // Redirect to waiting list
                            TempData["WaitingListBookId"] = item.BookId;
                            TempData["WaitingListBookTitle"] = book.Title;
                            return RedirectToAction("AskJoinWaitingList", "Borrow");
                        }

                        // Check if user already has this book borrowed
                        if (await _borrowDAL.HasUserBorrowedBookAsync(userId, item.BookId))
                        {
                            TempData["Error"] = "You already have this book borrowed.";
                            return RedirectToAction("Index");
                        }

                        // Check borrow limit
                        var distinctBorrowedBooks = await _borrowDAL.GetDistinctBorrowedBooksCountAsync(userId);
                        var distinctBorrowsInCart = cart.CartItems
                            .Where(i => i.IsBorrow && i.Id != item.Id)
                            .Select(i => i.BookId)
                            .Distinct()
                            .Count();

                        if (distinctBorrowedBooks + distinctBorrowsInCart >= 3)
                        {
                            TempData["Error"] = "You can only borrow up to 3 different books at a time.";
                            return RedirectToAction("Index");
                        }
                    }

                    item.IsBorrow = update.IsBorrow;
                    if (item.IsBorrow)
                    {
                        item.Quantity = 1; // Force quantity to 1 for borrow items
                    }
                    else
                    {
                        item.Quantity = update.Quantity;
                    }
                    item.FinalPrice = update.IsBorrow ? book.BorrowPrice : book.PurchasePrice;

                    await _cartItemDAL.UpdateAsync(item);
                }
            }

            TempData["Success"] = "Cart updated successfully.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index");
        }
    }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
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

                // Validate borrow limits
                if (cart.CartItems.Any(i => i.IsBorrow))
                {
                    var distinctBorrowedBooks = await _borrowDAL.GetDistinctBorrowedBooksCountAsync(userId);
                    var distinctBorrowsInCart = cart.CartItems
                        .Where(i => i.IsBorrow)
                        .Select(i => i.BookId)
                        .Distinct()
                        .Count();

                    if (distinctBorrowedBooks + distinctBorrowsInCart > 3)
                    {
                        return BadRequest(new { message = "You can only borrow up to 3 different books at a time." });
                    }
                }

                var lineItems = new List<SessionLineItemOptions>();
                foreach (var item in cart.CartItems)
                {
                    var book = await _bookDAL.GetBookByIdAsync(item.BookId);
                    lineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"{book.Title} ({(item.IsBorrow ? "Borrow" : "Purchase")})",
                                Description = $"by {book.Author}"
                            },
                            UnitAmount = Convert.ToInt64(item.FinalPrice * 100)
                        },
                        Quantity = item.Quantity
                    });
                }

                var sessionUrl = await _paymentService.CreateCheckoutSessionAsync(
                    lineItems,
                    Url.Action("Success", "ShoppingCart", null, Request.Scheme),
                    Url.Action("Cancel", "ShoppingCart", null, Request.Scheme)
                );

                return Ok(new { url = sessionUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateCheckoutSession: {ex.Message}");
                return BadRequest(new { message = "Failed to create checkout session." });
            }
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Success()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var purchasedItems = new List<(string Title, decimal Price)>();
                var borrowedItems = new List<(string Title, decimal Price, DateTime EndDate)>();

                if (TempData[TempPurchaseKey] is string tempPurchaseJson)
                {
                    var purchaseInfo = JsonSerializer.Deserialize<TempPurchaseInfo>(tempPurchaseJson);
                    if (purchaseInfo != null && purchaseInfo.IsBuyNow)
                    {
                        var book = await _bookDAL.GetBookByIdAsync(purchaseInfo.BookId);

                        if (book != null)
                        {
                            var purchase = new PurchaseModel
                            {
                                BookId = purchaseInfo.BookId,
                                UserId = userId,
                                PurchaseDate = DateTime.UtcNow,
                                FinalPrice = purchaseInfo.Price,
                                Quantity = 1,
                                IsHidden = false
                            };

                            var createdPurchase = await _purchaseDAL.CreatePurchaseAsync(purchase);
                            purchasedItems.Add((book.Title, purchaseInfo.Price));
                        }
                    }
                }
                else
                {
                    var cart = await _cartDAL.GetByUserIdAsync(userId);
                    if (cart?.CartItems?.Any() == true)
                    {
                        foreach (var item in cart.CartItems)
                        {
                            try
                            {
                                var book = await _bookDAL.GetBookByIdAsync(item.BookId);

                                if (item.IsBorrow)
                                {
                                    // Verify borrow limit hasn't been exceeded
                                    var currentBorrows = await _borrowDAL.GetDistinctBorrowedBooksCountAsync(userId);
                                    if (currentBorrows >= 3)
                                    {
                                        throw new InvalidOperationException("Borrow limit exceeded");
                                    }

                                    // Create borrow record (only one copy per book)
                                    var borrow = new BorrowModel
                                    {
                                        BookId = item.BookId,
                                        UserId = userId,
                                        StartDate = DateTime.UtcNow,
                                        EndDate = DateTime.UtcNow.AddDays(30),
                                        BorrowPrice = item.FinalPrice ?? 0,
                                        IsReturned = false
                                    };

                                    var createdBorrow = await _borrowDAL.CreateBorrowAsync(borrow);

                                    // Update available copies
                                    await _bookDAL.UpdateAvailableCopiesAsync(item.BookId);

                                    borrowedItems.Add((book.Title, item.FinalPrice ?? 0, borrow.EndDate));
                                }
                                else
                                {
                                    // Create purchase records
                                    for (int i = 0; i < item.Quantity; i++)
                                    {
                                        var purchase = new PurchaseModel
                                        {
                                            BookId = item.BookId,
                                            UserId = userId,
                                            PurchaseDate = DateTime.UtcNow,
                                            FinalPrice = item.FinalPrice ?? 0,
                                            DiscountId = item.DiscountId,
                                            IsHidden = false
                                        };

                                        var createdPurchase = await _purchaseDAL.CreatePurchaseAsync(purchase);
                                        purchasedItems.Add((book.Title, item.FinalPrice ?? 0));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing item {item.Id}: {ex.Message}");
                                throw;
                            }
                        }

                        // Clear the cart after successful processing
                        await _cartDAL.ClearCartAsync(userId);
                    }
                }

                // Send confirmation email if there are any processed items
                if ((purchasedItems.Any() || borrowedItems.Any()) && !string.IsNullOrEmpty(userEmail))
                {
                    try
                    {
                        var subject = "DigiReads - Order Confirmation";
                        var emailBody = BuildEmailBody(purchasedItems, borrowedItems,
                            User.Identity.Name ?? "Valued Customer");
                        await _emailService.SendEmailAsync(userEmail, subject, emailBody);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Email sending failed: {ex.Message}");
                        // Proceed without throwing to ensure transaction completion
                    }
                }

                ViewBag.Message = purchasedItems.Any() || borrowedItems.Any()
                    ? "Your transaction was successful! A confirmation email has been sent to your inbox."
                    : "No items to process.";

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Transaction Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ViewBag.Message = "There was an error processing your transaction.";
                return View();
            }
        }

        private string BuildEmailBody(
            List<(string Title, decimal Price)> purchasedItems,
            List<(string Title, decimal Price, DateTime EndDate)> borrowedItems,
            string userName)
        {
            var emailBuilder = new StringBuilder();
            emailBuilder.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>");
            emailBuilder.AppendLine("<h2 style='color: #2c3e50;'>Thank you for your order!</h2>");
            emailBuilder.AppendLine($"<p>Dear {userName},</p>");
            emailBuilder.AppendLine("<p>Here's a summary of your order:</p>");

            if (purchasedItems.Any())
            {
                emailBuilder.AppendLine("<h3 style='color: #3498db;'>Purchased Books:</h3><ul>");
                foreach (var (title, price) in purchasedItems)
                {
                    emailBuilder.AppendLine($"<li>{title} - ${price:F2}</li>");
                }

                emailBuilder.AppendLine("</ul>");
            }

            if (borrowedItems.Any())
            {
                emailBuilder.AppendLine("<h3 style='color: #3498db;'>Borrowed Books:</h3><ul>");
                foreach (var (title, price, endDate) in borrowedItems)
                {
                    emailBuilder.AppendLine($"<li>{title} - ${price:F2} (Due: {endDate:MMM dd, yyyy})</li>");
                }

                emailBuilder.AppendLine("</ul>");
            }

            emailBuilder.AppendLine("<p>Thank you for using DigiReads!</p>");
            emailBuilder.AppendLine("</div>");
            return emailBuilder.ToString();
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Cancel()
        {
            ViewBag.Message = "Your transaction was canceled.";
            return View();
        }
    }
}