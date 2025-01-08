using System.Security.Claims;
using EllipticCurve.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using test.Data;
using test.Enums;
using test.Models;

[Route("Books")]
public class BooksController : Controller
{
    private readonly BookDAL _bookDAL;
    private readonly PurchaseDAL _purchaseDAL;
    private readonly BorrowDAL _borrowDAL;
    private readonly RatingDAL _ratingDAL;

    public BooksController(BookDAL bookDAL, PurchaseDAL purchaseDAL, 
        BorrowDAL borrowDAL, RatingDAL ratingDAL)
    {
        _bookDAL = bookDAL;
        _purchaseDAL = purchaseDAL;
        _borrowDAL = borrowDAL;
        _ratingDAL = ratingDAL;
    }
    
    [HttpGet("AdminBooks")]
    [Authorize (Roles = "Admin")]
    public async Task<IActionResult> AdminBooks()
    {
        var books = await _bookDAL.GetAllBooksAsync();
        foreach (var book in books)
        {
            var activeDiscount = book.Discounts?
                .FirstOrDefault(d => d.IsActive && 
                                     d.StartDate.ToUniversalTime() <= DateTime.UtcNow && 
                                     d.EndDate.ToUniversalTime() > DateTime.UtcNow);
        
            if (activeDiscount != null)
            {
                Console.WriteLine($"Book {book.Title} has active discount: {activeDiscount.DiscountAmount}%");
                Console.WriteLine($"StartDate: {activeDiscount.StartDate}, EndDate: {activeDiscount.EndDate}");
            }
        }
        return View(books);
    }

    [HttpGet]
    [Authorize (Roles = "Admin")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize (Roles = "Admin")]
    public async Task<IActionResult> Create(BookModel book)
    {
        Console.WriteLine($"Starting book creation process... Title: {book.Title}"); // Debug log

        if (ModelState.IsValid)
        {
            Console.WriteLine("Model state is valid"); // Debug log
            try
            {
                // Check if the book already exists
                var exists = await _bookDAL.ExistsByTitleAndAuthorAsync(book.Title, book.Author);
                Console.WriteLine($"Book exists check: {exists}"); // Debug log

                if (exists)
                {
                    Console.WriteLine("Book already exists"); // Debug log
                    ModelState.AddModelError("Title", "A book with the same title and author already exists.");
                    return View(book);
                }

                Console.WriteLine("Attempting to add book to database..."); // Debug log
                var createdBook = await _bookDAL.CreateBookAsync(book);
                Console.WriteLine($"Book created successfully with ID: {createdBook.Id}"); // Debug log

                return RedirectToAction("AdminBooks");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating book: {ex.Message}"); // Debug log
                Console.WriteLine($"Stack trace: {ex.StackTrace}"); // Debug log
                ModelState.AddModelError("", "An error occurred while creating the book.");
                return View(book);
            }
        }

        Console.WriteLine("Model state is invalid. Errors:"); // Debug log
        foreach (var modelState in ModelState.Values)
        {
            foreach (var error in modelState.Errors)
            {
                Console.WriteLine($"- {error.ErrorMessage}");
            }
        }
        return View(book);
    }

    [HttpGet("Delete/{id:int}")]
    [Authorize (Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _bookDAL.GetBookByIdAsync(id);
        if (book == null)
        {
            return NotFound();
        }
        return View(book);
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize (Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        Console.WriteLine("trying to delete a book");
        var success = await _bookDAL.DeleteBookAsync(id);
        if (!success)
        {
            return NotFound();
        }
        Console.WriteLine("removed");
        return RedirectToAction("AdminBooks");
    }

    [HttpGet("Edit/{id:int}")]
    [Authorize (Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var book = await _bookDAL.GetBookByIdAsync(id);
        if (book == null)
        {
            return NotFound();
        }
        return View(book);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize (Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, BookModel book)
    {
        if (id != book.Id)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _bookDAL.UpdateBookAsync(book);
                return RedirectToAction("AdminBooks");
            }
            catch (DbUpdateConcurrencyException)
            {
                var existingBook = await _bookDAL.GetBookByIdAsync(id);
                if (existingBook == null)
                {
                    return NotFound();
                }
                throw;
            }
        }
        return View(book);
    }

    [HttpPost("Rate/{bookId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RateBook(int bookId, [FromForm] int rating)
    {
        // Get user ID from claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return BadRequest("User not found");
        }

        Console.WriteLine($"User {userId} attempting to rate book {bookId}");

        // Check if user can rate the book
        if (!await _ratingDAL.CanUserRateBook(userId, bookId))
        {
            return BadRequest("You can only rate books that you have purchased or borrowed.");
        }

        // Check if user has already rated this book
        if (await _ratingDAL.HasUserRatedBook(userId, bookId))
        {
            return BadRequest("You have already rated this book.");
        }

        var ratingModel = new RatingModel
        {
            BookId = bookId,
            UserId = userId,
            Value = rating,
            CreatedAt = DateTime.UtcNow
        };

        await _ratingDAL.AddRating(ratingModel);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true });
        }

        return RedirectToAction("MyBorrows", "Borrow");
    }

    [HttpGet("Ratings/{bookId}")]
    public async Task<IActionResult> GetBookRatings(int bookId)
    {
        var ratings = await _ratingDAL.GetBookRatings(bookId);
        return Json(ratings);
    }
    
    [HttpGet("UserHomePage")]
    public async Task<IActionResult> UserHomePage(
        string searchTitle = null,
        string searchAuthor = null,
        int? searchYear = null,
        bool? discountedOnly = null,
        Genre? genre = null,
        string sortBy = null,
        string sortOrder = "asc")
    {
        bool ascending = sortOrder?.ToLower() == "asc";

        var books = await _bookDAL.GetBooksAsync(
            searchTitle: searchTitle,
            searchAuthor: searchAuthor,
            searchYear: searchYear,
            discountedOnly: discountedOnly,
            genre: genre,
            sortBy: sortBy,
            ascending: ascending
        );
        
        
        // Calculate the effective price for each book
        var discountMap = new Dictionary<int, Tuple<decimal?, decimal?>>();

        foreach (var book in books)
        {
            // Get the effective prices
            var updatedPurchasePrice = await _bookDAL.GetEffectivePurchasePriceAsync(book.Id);
            var updatedBorrowPrice = await _bookDAL.GetEffectiveBorrowPriceAsync(book.Id);

            // Add to dictionary
            discountMap.Add(book.Id, Tuple.Create(updatedPurchasePrice, updatedBorrowPrice));
        }

        ViewData["SearchTitle"] = searchTitle;
        ViewData["SearchAuthor"] = searchAuthor;
        ViewData["Year"] = searchYear;
        ViewData["Discounted"] = discountedOnly;
        ViewData["Genre"] = genre;
        ViewData["SortBy"] = sortBy;
        ViewData["SortOrder"] = sortOrder;

        var model = (books, discountMap);
        return View(model);
    }
}