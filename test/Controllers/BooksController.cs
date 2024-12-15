using Microsoft.AspNetCore.Mvc;
using test.Data;

[Route("Books")]
public class BooksController : Controller
{
    private readonly BookDAL _bookDal;

    public BooksController(BookDAL bookDal)
    {
        _bookDal = bookDal;
    }

    [HttpGet("AdminBooks")]
    public async Task<IActionResult> AdminBooks()
    {
        var books = await _bookDal.GetAllBooksAsync();
        return View(books); // Render Razor view
    }
    
    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(); // Render the Create.cshtml view
    }

}