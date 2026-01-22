using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Artifex_Backend_2.Data;
using Artifex_Backend_2.Models;
using Artifex_Backend_2.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly ArtifexDbContext _context;

        public ReviewsController(ArtifexDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/Reviews/{productId}
        [HttpGet("{productId}")]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetProductReviews(int productId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Customer) // Load customer name
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserName = r.Customer.FullName ?? "Anonymous",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    Date = r.CreatedAt.ToString("MM/dd/yyyy")
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // 2. GET: api/Reviews/can-review/{productId}
        // Checks if the logged-in user bought the item
        [Authorize]
        [HttpGet("can-review/{productId}")]
        public async Task<ActionResult<bool>> CheckIfUserCanReview(int productId)
        {
            var userIdString = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            // Find the Customer Profile for this User
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return Ok(false);

            // Check if they have an order for this product
            bool hasPurchased = await _context.Orders.AnyAsync(o =>
                o.BuyerId == customer.Id && o.ProductId == productId);

            return Ok(hasPurchased);
        }

        // 3. POST: api/Reviews
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PostReview([FromBody] CreateReviewDto dto)
        {
            var userIdString = User.FindFirstValue("id");
            var userId = Guid.Parse(userIdString);

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return BadRequest("You must have a customer profile to review.");

            // Verification: Did they actually buy it?
            bool hasPurchased = await _context.Orders.AnyAsync(o =>
                o.BuyerId == customer.Id && o.ProductId == dto.ProductId);

            if (!hasPurchased)
            {
                return BadRequest("You can only review products you have purchased.");
            }

            // Prevent duplicate reviews? (Optional)
            bool alreadyReviewed = await _context.Reviews.AnyAsync(r =>
                r.CustomerId == customer.Id && r.ProductId == dto.ProductId);

            if (alreadyReviewed) return BadRequest("You have already reviewed this product.");

            var review = new Review
            {
                ProductId = dto.ProductId,
                CustomerId = customer.Id,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Review submitted successfully!" });
        }
    }
}
