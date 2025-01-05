using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using test.Models;
using test.Data;
using test.ViewModels;
using System.Security.Claims;
using test.Services;


namespace test.Controllers
{
    [Authorize]
    public class BorrowController : Controller
    {
        private readonly BookDAL _bookDAL;
        private readonly BorrowDAL _borrowDAL;
        private readonly WaitingListDAL _waitingListDAL;
        private readonly CartItemDAL _cartItemDAL;
        private readonly ShoppingCartDAL _shoppingCartDAL;
        private readonly PurchaseDAL _purchaseDAL;
        private readonly EmailService _emailService;

        public BorrowController(
            BookDAL bookDAL,
            BorrowDAL borrowDAL,
            CartItemDAL cartItemDAL,
            WaitingListDAL waitingListDAL,
            ShoppingCartDAL shoppingCartDAL,
            PurchaseDAL purchaseDAL,
            EmailService emailService)
        {
            _bookDAL = bookDAL;
            _borrowDAL = borrowDAL;
            _cartItemDAL = cartItemDAL;
            _waitingListDAL = waitingListDAL;
            _shoppingCartDAL = shoppingCartDAL;
            _purchaseDAL = purchaseDAL;
            _emailService = emailService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> InitiateBorrow(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var book = await _bookDAL.GetBookByIdAsync(id);

                if (book == null)
                {
                    TempData["Error"] = "Book not found.";
                    return RedirectToAction("UserHomePage", "Books");
                }

                var availableCopies = await _bookDAL.GetAvailableCopiesAsync(id);

                if (availableCopies > 0)
                {
                    // Check if the book is already in cart
                    var exists = await _shoppingCartDAL.ItemExistsInCartAsync(userId, id);
                    if (exists)
                    {
                        TempData["Error"] = "This book is already in your cart.";
                        return RedirectToAction("Index", "ShoppingCart");
                    }

                    // Get or create cart
                    var cart = await _shoppingCartDAL.GetOrCreateCartAsync(userId);

                    // Create cart item
                    var cartItem = new CartItemModel
                    {
                        BookId = id,
                        ShoppingCartId = cart.Id,
                        IsBorrow = true,
                        Quantity = 1,
                        DateAdded = DateTime.UtcNow,
                        FinalPrice = book.BorrowPrice
                    };

                    // Add item to cart using the proper method
                    await _shoppingCartDAL.AddItemToCartAsync(cartItem);

                    TempData["Success"] = "Book added to cart for borrowing!";
                    return RedirectToAction("Index", "ShoppingCart");
                }

                // If no copies available, handle waiting list
                TempData["WaitingListBookId"] = id;
                TempData["WaitingListBookTitle"] = book.Title;
                return RedirectToAction("AskJoinWaitingList");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in InitiateBorrow: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Failed to process borrow request.";
                return RedirectToAction("UserHomePage", "Books");
            }
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AskJoinWaitingList()
        {
            var bookId = TempData["WaitingListBookId"] as int?;
            var bookTitle = TempData["WaitingListBookTitle"] as string;

            if (!bookId.HasValue || string.IsNullOrEmpty(bookTitle))
            {
                return RedirectToAction("UserHomePage", "Books");
            }

            // Get book details
            var book = await _bookDAL.GetBookByIdAsync(bookId.Value);
            if (book == null)
            {
                return RedirectToAction("UserHomePage", "Books");
            }

            // Get waiting list count
            var waitingList = await _waitingListDAL.GetBookWaitingListAsync(bookId.Value);
            var peopleInQueue = waitingList.Count;

            // Get current user's potential position
            var position = peopleInQueue + 1;

            // Get estimated availability date based on current active borrows
            var estimatedDate = await _waitingListDAL.GetEstimatedAvailabilityAsync(bookId.Value, position);

            // Calculate estimated wait days
            int estimatedWaitDays;
            if (estimatedDate.HasValue)
            {
                estimatedWaitDays = (int)Math.Ceiling((estimatedDate.Value - DateTime.UtcNow).TotalDays);
            }
            else
            {
                // If no active borrows, estimate based on average borrow duration (30 days) and position in queue
                estimatedWaitDays = (int)Math.Ceiling((double)position / book.TotalCopies * 30);
            }

            var viewModel = new WaitingListConfirmViewModel
            {
                BookId = bookId.Value,
                BookTitle = bookTitle,
                PeopleInQueue = peopleInQueue,
                EstimatedWaitDays = Math.Max(estimatedWaitDays, 1), // Ensure minimum 1 day
                TotalCopies = book.TotalCopies,
                EstimatedAvailabilityDate = estimatedDate
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> JoinWaitingList(int bookId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                if (await _waitingListDAL.IsUserInWaitingListAsync(userId, bookId))
                {
                    TempData["Error"] = "You are already in the waiting list for this book.";
                }
                else
                {
                    await _waitingListDAL.AddToWaitingListAsync(userId, bookId);
                    TempData["Success"] = "You have been added to the waiting list!";
                }
            }
            catch
            {
                TempData["Error"] = "Failed to join waiting list.";
            }

            return RedirectToAction("UserHomePage", "Books");
        }
        
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyBorrows()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                Console.WriteLine($"MyBorrows called for user ID: {userId}");

                // Get all purchases through PurchaseDAL
                var allPurchases = await _purchaseDAL.GetAllPurchasesAsync();
                Console.WriteLine($"Total purchases in database: {allPurchases.Count}");
        
                // Get user purchases
                var userPurchases = await _purchaseDAL.GetUserPurchasesAsync(userId);
                Console.WriteLine($"Purchases returned for view: {userPurchases.Count}");
        
                // Get borrows
                var borrows = await _borrowDAL.GetUserBorrowsAsync(userId);
                var activeBorrows = borrows.Where(b => !b.IsReturned && b.EndDate > DateTime.UtcNow);
                Console.WriteLine($"Active borrows: {activeBorrows.Count()}");

                ViewBag.PurchasedBooks = userPurchases;
                ViewBag.BorrowedBooks = activeBorrows;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MyBorrows: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Failed to load library.";
                return RedirectToAction("Index", "Home");
            }
        }
        
        [HttpPost]


    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> ReturnBook(int id)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var borrow = await _borrowDAL.GetBorrowByIdAsync(id);

            if (borrow == null || borrow.UserId != userId)
            {
                TempData["Error"] = "Invalid book return request.";
                return RedirectToAction(nameof(MyBorrows));
            }

            // Process the return
            await _borrowDAL.ReturnBookAsync(id);

            // Update available copies
            await _bookDAL.UpdateAvailableCopiesAsync(borrow.BookId);

            // Get top 3 people in waiting list
            var topThreeInLine = await _waitingListDAL.GetTopWaitingListPositionsAsync(borrow.BookId, 3);
            
            if (topThreeInLine.Any())
            {
                foreach (var waitingUser in topThreeInLine)
                {
                    try
                    {
                        var emailBody = $@"
                            <h2>Good News!</h2>
                            <p>The book '{borrow.Book.Title}' is now available for borrowing.</p>
                            <p>You are position {waitingUser.Position} in line.</p>
                            <p>Please log in to your account to borrow the book. The first person to borrow the book will get it.</p>
                            <p>If you don't borrow the book soon, it may be borrowed by another user in the waiting list.</p>";

                        await _emailService.SendEmailAsync(
                            waitingUser.User.Email,
                            "Book Available for Borrowing",
                            emailBody
                        );

                        // Mark as notified
                        waitingUser.IsNotified = true;
                        await _waitingListDAL.UpdateWaitingListItemAsync(waitingUser);
                    }
                    catch (Exception emailEx)
                    {
                        // Log the email error but continue processing
                        Console.WriteLine($"Error sending notification email to user {waitingUser.UserId}: {emailEx.Message}");
                    }
                }
                TempData["Success"] = "Book returned successfully and waiting list users notified.";
            }
            else
            {
                TempData["Success"] = "Book returned successfully.";
            }

            return RedirectToAction(nameof(MyBorrows));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error returning book: {ex.Message}");
            TempData["Error"] = "An error occurred while returning the book.";
            return RedirectToAction(nameof(MyBorrows));
        }
    }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HidePurchasedBook(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var purchase = await _purchaseDAL.GetPurchaseByIdAsync(id);

                if (purchase == null || purchase.UserId != userId)
                {
                    TempData["Error"] = "Invalid request.";
                    return RedirectToAction(nameof(MyBorrows));
                }

                purchase.IsHidden = true;
                await _purchaseDAL.UpdatePurchaseAsync(purchase);

                TempData["Success"] = "Book removed from your library.";
                return RedirectToAction(nameof(MyBorrows));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to remove book from library.";
                return RedirectToAction(nameof(MyBorrows));
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePurchasedBook(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var purchase = await _purchaseDAL.GetPurchaseByIdAsync(id);

                if (purchase == null || purchase.UserId != userId)
                {
                    TempData["Error"] = "Invalid request.";
                    return RedirectToAction(nameof(MyBorrows));
                }

                await _purchaseDAL.DeletePurchaseAsync(id);

                TempData["Success"] = "Book has been permanently deleted from your library.";
                return RedirectToAction(nameof(MyBorrows));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to delete book from library.";
                return RedirectToAction(nameof(MyBorrows));
            }
        }
    }
}