using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Data
{
    public class ReviewDAL
    {
        private readonly ApplicationDbContext _context;

        public ReviewDAL(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get a review by UserId
        public async Task<ReviewModel> GetReviewByUserIdAsync(int userId)
        {
            return await _context.WebsiteReviews.FirstOrDefaultAsync(r => r.UserId == userId);
        }

        // Get all reviews
        public async Task<List<ReviewModel>> GetAllReviewsAsync()
        {
            return await _context.WebsiteReviews.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }

        // Add a new review
        public async Task AddReviewAsync(ReviewModel review)
        {
            await _context.WebsiteReviews.AddAsync(review);
            await _context.SaveChangesAsync();
        }

        // Update an existing review
        public async Task UpdateReviewAsync(ReviewModel review)
        {
            _context.WebsiteReviews.Update(review);
            await _context.SaveChangesAsync();
        }

        // Delete a review by Id
        public async Task DeleteReviewAsync(int id)
        {
            var review = await _context.WebsiteReviews.FindAsync(id);
            if (review != null)
            {
                _context.WebsiteReviews.Remove(review);
                await _context.SaveChangesAsync();
            }
        }
    }
}