using Artifex_Backend_2.Data;
using Artifex_Backend_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "4")]
    public class PlatformAdminSellerController : ControllerBase
    {
        private readonly ArtifexDbContext _db;
        public PlatformAdminSellerController(ArtifexDbContext db)
        {
            _db = db;
        }

        [HttpGet("pending-sellers")]
        public async Task<IActionResult> GetPendingSellers()
        {
            var pending = await _db.Sellers
                .Where(s => s.IsApproved == false)
                .ToListAsync();

            return Ok(pending);
        }

        [HttpPost("approve/{sellerId}")]
        public async Task<IActionResult> ApproveSeller(Guid sellerId)
        {
            var seller = await _db.Sellers.FindAsync(sellerId);
            if (seller == null)
                return NotFound("Seller not found.");

            seller.IsApproved = true;

            var user = await _db.Users.FindAsync(seller.UserId);
            if (user != null)
                user.Role = UserRole.Seller;

            await _db.SaveChangesAsync();
            return Ok("Seller approved.");
        }

        [HttpPost("suspend-seller/{sellerId}")]
        public async Task<IActionResult> SuspendSeller(Guid sellerId)
        {
            var seller = await _db.Sellers.FindAsync(sellerId);
            if (seller == null)
                return NotFound("Seller not found.");

            seller.IsSuspended = true;

            await _db.SaveChangesAsync();
            return Ok("Seller suspended.");
        }

        [HttpPost("unsuspend-seller/{sellerId}")]
        public async Task<IActionResult> UnsuspendSeller(Guid sellerId)
        {
            var seller = await _db.Sellers.FindAsync(sellerId);
            if (seller == null)
                return NotFound("Seller not found.");

            seller.IsSuspended = false;

            await _db.SaveChangesAsync();
            return Ok("Seller unsuspended.");
        }

        [HttpDelete("deactivate-seller/{sellerId}")]
        public async Task<IActionResult> DeleteSeller(Guid sellerId)
        {
            var seller = await _db.Sellers.FindAsync(sellerId);
            if (seller == null)
                return NotFound("Seller not found.");

            var user = await _db.Users.FindAsync(seller.UserId);

            _db.Sellers.Remove(seller);

            if (user != null)
            {
                // You can delete the user or downgrade back to normal user
                user.Role = UserRole.User;
            }

            await _db.SaveChangesAsync();
            return Ok("Seller profile deleted.");
        }
    }
}
