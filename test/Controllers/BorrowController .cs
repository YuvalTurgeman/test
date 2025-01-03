using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using test.Models;
using test.Data;
using test.ViewModels;
using System.Security.Claims;

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

        public BorrowController(
            BookDAL bookDAL,
            BorrowDAL borrowDAL,
            CartItemDAL cartItemDAL,
            WaitingListDAL waitingListDAL,
            ShoppingCartDAL shoppingCartDAL)
        {
            _bookDAL = bookDAL;
            _borrowDAL = borrowDAL;
            _cartItemDAL = cartItemDAL;
            _waitingListDAL = waitingListDAL;
            _shoppingCartDAL = shoppingCartDAL;
        }

            [HttpPost]
    [ValidateAntiForgeryToken]
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

        public IActionResult AskJoinWaitingList()
        {
            var bookId = TempData["WaitingListBookId"] as int?;
            var bookTitle = TempData["WaitingListBookTitle"] as string;

            if (!bookId.HasValue || string.IsNullOrEmpty(bookTitle))
            {
                return RedirectToAction("UserHomePage", "Books");
            }

            var viewModel = new WaitingListConfirmViewModel
            {
                BookId = bookId.Value,
                BookTitle = bookTitle
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

        public async Task<IActionResult> MyBorrows()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var borrows = await _borrowDAL.GetUserBorrowsAsync(userId);
            return View(borrows);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBook(int id)
        {
            try
            {
                var borrow = await _borrowDAL.GetBorrowByIdAsync(id);
                if (borrow == null)
                    return NotFound();

                await _borrowDAL.ReturnBookAsync(id);
                await _bookDAL.UpdateAvailableCopiesAsync(borrow.BookId);

                TempData["Success"] = "Book returned successfully!";
                return RedirectToAction(nameof(MyBorrows));
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while returning the book.";
                return RedirectToAction(nameof(MyBorrows));
            }
        }
    }
}