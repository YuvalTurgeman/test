using Microsoft.AspNetCore.Mvc;
using test.Models;
using test.Data;

[ApiController]
[Route("api/[controller]")]
public class BooksApiController : ControllerBase
{
    private readonly BookDAL _bookDal;

    public BooksApiController(BookDAL bookDal)
    {
        _bookDal = bookDal;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetBooks()
    {
        var books = await _bookDal.GetAllBooksAsync();
        return Ok(books);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBookById(int id)
    {
        var book = await _bookDal.GetBookByIdAsync(id);
        if (book == null) return NotFound();
        return Ok(book);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBook([FromBody] BookModel book)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _bookDal.AddBookAsync(book);
        return CreatedAtAction(nameof(GetBookById), new { id = book.Id }, book);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateBook(int id, [FromBody] BookModel book)
    {
        if (id != book.Id) return BadRequest();

        var success = await _bookDal.UpdateBookAsync(book);
        if (!success) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var success = await _bookDal.DeleteBookAsync(id);
        if (!success) return NotFound();

        return NoContent();
    }
}