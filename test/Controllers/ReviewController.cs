using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using test.Data;
using test.Models;

namespace test.Controllers
{
    [Authorize]
    public class ReviewsController : BaseController
    {
        private readonly ReviewDAL _reviewDAL;

        public ReviewsController(ReviewDAL reviewDAL)
        {
            _reviewDAL = reviewDAL;
        }

        // GET: WriteReview
        [Authorize (Roles = "Customer")]
        public async Task<IActionResult> WriteReview()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var existingReview = await _reviewDAL.GetReviewByUserIdAsync(userId);

            return View(existingReview ?? new ReviewModel());
        }

        // POST: WriteReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize (Roles = "Customer")]
        public async Task<IActionResult> WriteReview(ReviewModel review)
        {
            Console.WriteLine("1");
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var existingReview = await _reviewDAL.GetReviewByUserIdAsync(userId);
            
            Console.WriteLine(userId);
            Console.WriteLine(existingReview);
            
            Console.WriteLine("2");
            // if (!ModelState.IsValid)
            // {
            //     Console.WriteLine("3");
            //     return View(review);
            // }
            
            Console.WriteLine("4");
            if (existingReview == null)
            {
                Console.WriteLine("5");
                review.UserId = userId;
                await _reviewDAL.AddReviewAsync(review);
                TempData["Success"] = "Thank you for your review!";
            }
            else
            {
                Console.WriteLine("6");
                existingReview.Rating = review.Rating;
                existingReview.Comment = review.Comment;
                await _reviewDAL.UpdateReviewAsync(existingReview);
                TempData["Success"] = "Your review has been updated!";
            }
            Console.WriteLine("7");
            return RedirectToAction("ShowcaseReviews");
        }

        // GET: ShowcaseReviews
        [AllowAnonymous]
        public async Task<IActionResult> ShowcaseReviews()
        {
            var reviews = await _reviewDAL.GetAllReviewsAsync();
            return View(reviews);
        }
    }
}
