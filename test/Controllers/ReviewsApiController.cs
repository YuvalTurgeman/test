using Microsoft.AspNetCore.Mvc;
using test.Data;
using test.Models;

[ApiController]
[Route("api/[controller]")]
public class ReviewsApiController : ControllerBase
{
    private readonly ReviewDAL _reviewDal;

    public ReviewsApiController(ReviewDAL reviewDal)
    {
        _reviewDal = reviewDal;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllReviews()
    {
        var reviews = await _reviewDal.GetAllReviewsAsync();
        return Ok(reviews);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReviewById(int id)
    {
        var review = await _reviewDal.GetReviewByIdAsync(id);
        if (review == null) return NotFound();
        return Ok(review);
    }

    [HttpPost]
    public async Task<IActionResult> AddReview([FromBody] ReviewModel review)
    {
        await _reviewDal.AddReviewAsync(review);
        return CreatedAtAction(nameof(GetReviewById), new { id = review.Id }, review);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReview(int id, [FromBody] ReviewModel review)
    {
        if (id != review.Id) return BadRequest("ID mismatch.");

        var existingReview = await _reviewDal.GetReviewByIdAsync(id);
        if (existingReview == null) return NotFound();

        await _reviewDal.UpdateReviewAsync(review);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var existingReview = await _reviewDal.GetReviewByIdAsync(id);
        if (existingReview == null) return NotFound();

        await _reviewDal.DeleteReviewAsync(id);
        return NoContent();
    }
}