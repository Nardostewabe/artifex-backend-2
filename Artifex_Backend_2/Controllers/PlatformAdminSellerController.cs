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
        private readonly Services.IEmailService _emailService;

        public PlatformAdminSellerController(ArtifexDbContext db, Services.IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        [HttpGet("all-sellers")]
        public async Task<IActionResult> GetAllSellers()
        {
            var sellers = await _db.Sellers
                .Include(s => s.User) // Include User to see owner's name/email
                .Select(s => new
                {
                    s.Id,
                    s.ShopName,
                    s.Description,
                    s.IsApproved,
                    s.IsSuspended,
                    s.IsDeactivated,
                    OwnerName = s.User.Username,
                    OwnerEmail = s.User.Email,
                    JoinedDate = s.User.CreatedAt
                })
                .ToListAsync();

            return Ok(sellers);
        }

        [HttpGet("pending-sellers")]
        public async Task<IActionResult> GetPendingSellers()
        {
            var pending = await _db.Sellers
                .Where(s => s.IsApproved == false && s.IsDeactivated == false) // Exclude rejected/deactivated
                .ToListAsync();

            return Ok(pending);
        }

        [HttpPost("approve/{sellerId}")]
        public async Task<IActionResult> ApproveSeller(Guid sellerId)
        {
            var seller = await _db.Sellers.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == sellerId);
            if (seller == null)
                return NotFound("Seller not found.");

            seller.IsApproved = true;

            var user = seller.User;
            if (user != null)
            {
                user.Role = UserRole.Seller;
                await _emailService.SendEmailAsync(user.Email, "Your Artifex Seller Profile is Approved!", 
                    $"<h1>Congratulations, {seller.ShopName}!</h1><p>Your seller profile has been approved. You can now log in and start selling.</p>");
            }

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

            bool wasPending = !seller.IsApproved;

            seller.IsDeactivated = true;
            seller.IsApproved = false;

            if (seller.User != null)
            {
                seller.User.Role = UserRole.User; // Downgrade access

                // Send Email
                string subject = wasPending ? "Artifex Seller Application Rejected" : "Artifex Seller Account Deactivated";
                string body = wasPending 
                    ? $"<p>We regret to inform you that your application for shop <strong>{seller.ShopName}</strong> was not approved at this time.</p>"
                    : $"<p>Your seller account for <strong>{seller.ShopName}</strong> has been deactivated.</p>";

                await _emailService.SendEmailAsync(seller.User.Email, subject, body);
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
