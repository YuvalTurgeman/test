using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using test.Data;
using test.Models;

[Route("Books")]
public class BooksController : BaseController
{
    private readonly BookDAL _bookDAL;
    private readonly PurchaseDAL _purchaseDAL;
    private readonly BorrowDAL _borrowDAL;

    public BooksController(BookDAL bookDAL, PurchaseDAL purchaseDAL, BorrowDAL borrowDAL)
    {
        _bookDAL = bookDAL;
        _purchaseDAL = purchaseDAL;
        _borrowDAL = borrowDAL;
    }

    [HttpGet("AdminBooks")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminBooks()
    {
        var books = await _bookDAL.GetAllBooksAsync();
        return View(books);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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

    [HttpGet("UserHomePage")]
    public async Task<IActionResult> UserHomePage(string genre = null, decimal? minPrice = null, decimal? maxPrice = null)
    {
        var books = await _bookDAL.GetFilteredBooksAsync(genre, minPrice, maxPrice);
        return View(books);
    }

    [HttpPost("Purchase/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Purchase(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var book = await _bookDAL.GetBookByIdAsync(id);
        if (book == null)
        {
            return NotFound();
        }

        var purchase = new PurchaseModel
        {
            BookId = book.Id,
            UserId = userId.Value,
            PurchaseDate = DateTime.UtcNow,
            FinalPrice = book.PurchasePrice ?? 0
        };

        await _purchaseDAL.CreatePurchaseAsync(purchase);
        return RedirectToAction("UserHomePage");
    }

    [HttpPost("Borrow/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrow(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var book = await _bookDAL.GetBookByIdAsync(id);
        if (book == null)
        {
            return NotFound();
        }

        var borrow = new BorrowModel
        {
            BookId = book.Id,
            UserId = userId.Value,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30), // Default borrow period of 14 day
            BorrowPrice = book.BorrowPrice ?? 0
        };

        await _borrowDAL.CreateBorrowAsync(borrow);
        return RedirectToAction("UserHomePage");
    }
}