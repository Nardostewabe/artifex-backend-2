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

        [HttpPatch("deactivate-seller/{sellerId}")]
        public async Task<IActionResult> DeactivateSeller(Guid sellerId)
        {
            var seller = await _db.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sellerId);

            if (seller == null)
                return NotFound("Seller not found.");

            seller.IsDeactivated = true;
            seller.IsApproved = false;

            if (seller.User != null)
            {
                seller.User.Role = UserRole.User; // Downgrade access
            }

            await _db.SaveChangesAsync();
            return Ok("Seller deactivated successfully.");
        }

        [HttpPatch("reactivate-seller/{sellerId}")]
        public async Task<IActionResult> ReactivateSeller(Guid sellerId)
        {
            var seller = await _db.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sellerId);

            if (seller == null)
                return NotFound();

            seller.IsDeactivated = false;
            seller.IsApproved = true;

            seller.User.Role = UserRole.Seller;

            await _db.SaveChangesAsync();
            return Ok("Seller reactivated.");
        }

    }
}
