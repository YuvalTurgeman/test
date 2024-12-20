using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test.Data;
using test.Models;
using System.Linq;
using System.Threading.Tasks;

[Route("Books")]
public class BooksController : Controller
{
    private readonly ApplicationDbContext _context; // Using ApplicationDbContext directly

    public BooksController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("AdminBooks")]
    public async Task<IActionResult> AdminBooks()
    {
        var books = await _context.Books.ToListAsync(); // Fetch books directly from DbContext
        return View(books); // Render Razor view
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(); // Render the Create.cshtml view
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(BookModel book)
    {
        Console.WriteLine("trying to add book");
        if (ModelState.IsValid)
        {
            Console.WriteLine("function started");
            // Check if the book already exists
            if (_context.Books.Any(b => b.Title == book.Title && b.Author == book.Author))
            {
                Console.WriteLine("double book");
                ModelState.AddModelError("Title", "A book with the same title and author already exists.");
                return View(book);
            }
            Console.WriteLine("adding to db");
            // Add the book to the database
            _context.Books.Add(book);
            _context.SaveChanges();
            Console.WriteLine("saved to db");
            // Redirect to the AdminBooks action
            return RedirectToAction("AdminBooks");
        }

        // If validation fails, return the form with errors
        Console.WriteLine("book addition failed");
        return View(book);
    }
    
    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Fetch the book by ID
        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound(); // Return 404 if the book does not exist
        }

        return View(book); // Pass the book to the Delete view
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        Console.WriteLine("trying to delete a book");
        // Fetch the book by ID
        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound(); // Return 404 if the book does not exist
        }
        Console.WriteLine("preparing to remove");
        // Remove the book from the database
        _context.Books.Remove(book);
        await _context.SaveChangesAsync();
        Console.WriteLine("removed");
        // Redirect to AdminBooks after successful deletion
        return RedirectToAction("AdminBooks");
    }
    
    // GET: Edit Book
    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        // Fetch the book by ID
        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound(); // Return 404 if the book does not exist
        }

        return View(book); // Pass the book to the Edit view
    }
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BookModel book)
    {
        if (id != book.Id)
        {
            return BadRequest(); // Ensure the ID in the route matches the book's ID
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Update the book in the database
                _context.Books.Update(book);
                await _context.SaveChangesAsync();

                // Redirect to AdminBooks after successful update
                return RedirectToAction("AdminBooks", "Books");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Books.Any(b => b.Id == book.Id))
                {
                    return NotFound(); // Return 404 if the book does not exist
                }
                else
                {
                    throw;
                }
            }
        }

        // If validation fails, return the form with errors
        return View(book);
    }

}
