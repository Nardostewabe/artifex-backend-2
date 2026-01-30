using Artifex_Backend_2.Data;
using Artifex_Backend_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class UserDisputesController : ControllerBase
    {
        private readonly ArtifexDbContext _db;

        public UserDisputesController(ArtifexDbContext db)
        {
            _db = db;
        }

        // [POST] Report a dispute on an order
        [HttpPost]
        public async Task<IActionResult> CreateDispute([FromBody] DisputeCreateDto dto)
        {
            var userIdString = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            // Verify the order exists and belongs to the user (as buyer or seller)
            var order = await _db.Orders
                .Include(o => o.Product)
                .Include(o => o.Buyer)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

            if (order == null) return NotFound("Order not found.");

            // Check if user is either the buyer or the seller of this order
            var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.Id == order.Product.SellerId);
            
            bool isBuyer = order.Buyer.UserId == userId;
            bool isSeller = seller != null && seller.UserId == userId;

            if (!isBuyer && !isSeller)
            {
                return Forbid("You are not authorized to open a dispute for this order.");
            }

            // Check if a dispute already exists for this order
            var existingDispute = await _db.Disputes.FirstOrDefaultAsync(d => d.OrderId == dto.OrderId && d.Status == "Open");
            if (existingDispute != null)
            {
                return BadRequest("An open dispute already exists for this order.");
            }

            var dispute = new Dispute
            {
                OrderId = dto.OrderId,
                ComplainantId = userId,
                Reason = dto.Reason,
                Description = dto.Description,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            _db.Disputes.Add(dispute);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Dispute opened successfully.", disputeId = dispute.Id });
        }
    }

    public class DisputeCreateDto
    {
        public int OrderId { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }
    }
}
