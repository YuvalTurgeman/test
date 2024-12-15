using Microsoft.AspNetCore.Mvc;
using test.Data;

[Route("Reviews")]
public class ReviewsController : Controller
{
    private readonly ReviewDAL _reviewDal;

    public ReviewsController(ReviewDAL reviewDal)
    {
        _reviewDal = reviewDal;
    }

    [HttpGet("AdminReviews")]
    public async Task<IActionResult> AdminReviews()
    {
        var reviews = await _reviewDal.GetAllReviewsAsync();
        return View(reviews); // Render Razor view
    }
}