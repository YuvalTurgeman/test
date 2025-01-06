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

    public BooksController(BookDAL bookDAL, PurchaseDAL purchaseDAL, BorrowDAL borrowDAL)
    {
        _bookDAL = bookDAL;
        _purchaseDAL = purchaseDAL;
        _borrowDAL = borrowDAL;
    }

    [HttpGet("AdminBooks")]
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
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
    public async Task<IActionResult> UserHomePage(
        string searchTitle = null,
        string searchAuthor = null,
        int? searchYear = null,
        bool? discountedOnly = null,
        Genre? genre = null, // Add the genre filter
        string sortBy = null,
        string sortOrder = "asc") // Default to ascending order
    {
        // Determine the sorting direction
        bool ascending = sortOrder?.ToLower() == "asc";
        


        // Fetch books with filtering and sorting
        var books = await _bookDAL.GetBooksAsync(
            searchTitle: searchTitle,
            searchAuthor: searchAuthor,
            searchYear: searchYear,
            discountedOnly: discountedOnly,
            genre: genre, // Pass the genre filter
            sortBy: sortBy,
            ascending: ascending
        );

        // Pass filter and sort parameters back to the view
        ViewData["SearchTitle"] = searchTitle;
        ViewData["SearchAuthor"] = searchAuthor;
        ViewData["Year"] = searchYear;
        ViewData["Discounted"] = discountedOnly;
        ViewData["Genre"] = genre; // Add genre to ViewData
        ViewData["SortBy"] = sortBy;
        ViewData["SortOrder"] = sortOrder;

        return View(books);
    }
}