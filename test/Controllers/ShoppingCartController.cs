using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text;
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
        private readonly PurchaseDAL _purchaseDAL;
        private readonly EmailService _emailService;

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
                        await _waitingListDAL.AddToWaitingListAsync(userId, bookId);
                        TempData["Success"] = "Added to waiting list as the book is currently unavailable.";
                        return RedirectToAction("Index", "Book");
                    }
                }

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
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                
                Console.WriteLine($"Processing success for user ID: {userId}, Email: {userEmail}");

                var cart = await _cartDAL.GetByUserIdAsync(userId);
                if (cart == null || !cart.CartItems.Any())
                {
                    Console.WriteLine("Cart is empty or not found");
                    ViewBag.Message = "No items to process.";
                    return View();
                }

                var purchasedItems = new List<(string Title, decimal Price)>();
                var borrowedItems = new List<(string Title, decimal Price, DateTime EndDate)>();

                foreach (var item in cart.CartItems)
                {
                    try
                    {
                        var book = await _bookDAL.GetBookByIdAsync(item.BookId);
                        Console.WriteLine($"Processing book: {book.Title}");

                        if (item.IsBorrow)
                        {
                            var borrow = new BorrowModel
                            {
                                BookId = item.BookId,
                                UserId = userId,
                                StartDate = DateTime.UtcNow,
                                EndDate = DateTime.UtcNow.AddDays(30),
                                BorrowPrice = item.FinalPrice ?? 0,
                                IsReturned = false
                            };
                            await _borrowDAL.CreateBorrowAsync(borrow);
                            await _bookDAL.UpdateAvailableCopiesAsync(item.BookId);
                            borrowedItems.Add((book.Title, item.FinalPrice ?? 0, borrow.EndDate));

                            // Notify waiting list users
                            var waitingListItems = await _waitingListDAL.GetBookWaitingListAsync(item.BookId);
                            foreach (var waitingItem in waitingListItems.Take(3))
                            {
                                try
                                {
                                    var emailBody = $@"
                                        <h2>Book Available Update</h2>
                                        <p>The book '{book.Title}' will be available in 30 days.</p>
                                        <p>You are currently in position {waitingItem.Position} in the waiting list.</p>
                                        <p>We'll notify you again when the book becomes available.</p>";

                                    await _emailService.SendEmailAsync(
                                        waitingItem.User.Email,
                                        "Book Availability Update",
                                        emailBody
                                    );
                                }
                                catch (Exception emailEx)
                                {
                                    Console.WriteLine($"Error sending waiting list notification: {emailEx.Message}");
                                }
                            }
                        }
                        else
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
                            Console.WriteLine($"Created purchase record: {createdPurchase.Id} for book {book.Title}");
                            purchasedItems.Add((book.Title, item.FinalPrice ?? 0));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing item {item.Id}: {ex.Message}");
                        throw;
                    }
                }

                if (!string.IsNullOrEmpty(userEmail))
                {
                    try
                    {
                        Console.WriteLine("Sending confirmation email...");
                        var subject = "DigiReads - Order Confirmation";
                        var emailBody = BuildEmailBody(purchasedItems, borrowedItems, User.Identity.Name ?? "Valued Customer");
                        await _emailService.SendEmailAsync(userEmail, subject, emailBody);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Email sending failed: {ex.Message}");
                    }
                }

                await _cartDAL.ClearCartAsync(userId);
                ViewBag.Message = "Your transaction was successful! A confirmation email has been sent to your inbox.";
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

        public IActionResult Cancel()
        {
            ViewBag.Message = "Your transaction was canceled.";
            return View();
        }
    }
}