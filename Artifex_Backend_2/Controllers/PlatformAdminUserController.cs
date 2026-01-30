using Artifex_Backend_2.Data;
using Artifex_Backend_2.Models;
using Artifex_Backend_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "4")] // Platform Admin Only
    public class PlatformAdminUsersController : ControllerBase
    {
        private readonly ArtifexDbContext _db;
        private readonly IEmailService _emailService;

        public PlatformAdminUsersController(ArtifexDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        // [GET] List All Users (For the main table)
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _db.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    Role = u.Role.ToString(),
                    JoinedDate = u.CreatedAt
                })
                .OrderByDescending(u => u.JoinedDate)
                .ToListAsync();

            return Ok(users);
        }

        // [GET] Single User Details (For the Modal)
        // [GET] Single User Details (For the Modal)
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserDetail(Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            // 1. Calculate Order Count (Dynamic)
            var orderCount = await _db.Orders.CountAsync(o => o.BuyerId == userId);

            // 2. Get Recent Orders (Top 5)
            var recentOrders = await _db.Orders
                .Where(o => o.BuyerId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new
                {
                    o.Id,
                    Total = o.TotalPrice,
                    o.Status,
                    Date = o.OrderDate
                })
                .ToListAsync();

            // 3. Calculate Report Count (Logic: Is this user a Seller?)
            int reportCount = 0;

            // Find if this user owns a shop
            var sellerProfile = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);

            if (sellerProfile != null)
            {
                // Count reports filed against their Shop (using TargetSellerId)
                reportCount = await _db.Reports.CountAsync(r => r.TargetSellerId == sellerProfile.Id);
            }

            // 4. Determine Account Status (Updated)
            // Priority: User Suspension > Seller Suspension > Active
            string status = "Active";

            if (user.IsSuspended)
            {
                status = "Suspended"; // ✅ User is banned globally
            }
            else if (sellerProfile != null && (sellerProfile.IsSuspended || sellerProfile.IsDeactivated))
            {
                status = "Suspended (Shop)"; // ✅ User is okay, but Shop is closed
            }

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                Role = user.Role.ToString(), // Return string name (e.g., "Customer")
                JoinedDate = user.CreatedAt,
                Status = status,
                OrderCount = orderCount,
                ReportCount = reportCount,
                RecentOrders = recentOrders
            });
        }
        [HttpPost("warn/{userId}")]
        public async Task<IActionResult> WarnUser(Guid userId, [FromBody] string message)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            // ✅ Send Email
            string subject = "Official Warning - Artifex Moderation";
            string body = $@"
                <h3>Hello {user.Username},</h3>
                <p>You have received an official warning from the Artifex administration team.</p>
                <div style='background-color: #fff3cd; padding: 15px; border-left: 5px solid #ffc107;'>
                    <strong>Reason:</strong> {message}
                </div>
                <p>Please review our community guidelines. Further violations may result in account suspension.</p>
                <p>Regards,<br>Artifex Safety Team</p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            return Ok(new { message = $"Warning sent to {user.Username}" });
        }
    }
}