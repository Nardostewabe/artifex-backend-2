using Artifex_Backend_2.Data;
using Artifex_Backend_2.Models;
using Artifex_Backend_2.DTOs;
using Artifex_Backend_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "3")] // Content Admin Only
    public class ContentAdminController : ControllerBase
    {
        private readonly ArtifexDbContext _db;
        private readonly IEmailService _emailService;

        public ContentAdminController(ArtifexDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        // [POST] Delete Product & Notify Seller
        [HttpPost("delete-product/{productId}")]
        public async Task<IActionResult> DeleteProduct(int productId, [FromBody] string deletionReason)
        {
            // 1. Find the product and the seller
            var product = await _db.Products
                .Include(p => p.Seller)
                .ThenInclude(s => s.User) // Need User to get Email
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound("Product not found.");

            // 2. Capture details for email before deletion
            var sellerEmail = product.Seller.User.Email;
            var productName = product.Name;
            var shopName = product.Seller.ShopName;

            // 3. Delete the product
            _db.Products.Remove(product);

            // Also mark any reports related to this product as Resolved
            var relatedReports = await _db.Reports.Where(r => r.TargetProductId == productId).ToListAsync();
            foreach (var report in relatedReports)
            {
                report.Status = "Resolved";
                report.Description += " [Product Deleted by Admin]";
            }

            await _db.SaveChangesAsync();

            // 4. Send Email to Seller
            var emailSubject = "Product Removal Notification - Artifex";
            var emailBody = $@"
                <h3>Hello {shopName},</h3>
                <p>We are writing to inform you that your product <strong>{productName}</strong> has been removed from Artifex.</p>
                <p><strong>Reason:</strong> {deletionReason}</p>
                <p>Please review our content guidelines to prevent future violations.</p>
                <p>Regards,<br>Artifex Moderation Team</p>";

            await _emailService.SendEmailAsync(sellerEmail, emailSubject, emailBody);

            return Ok("Product deleted and seller notified.");
        }

        // [GET] View All Reports
        [HttpGet("reports")]
        public async Task<IActionResult> GetPendingReports()
        {
            var reports = await _db.Reports
                .Where(r => r.Status == "Pending")
                .Include(r => r.Reporter)
                .Include(r => r.Product)
                .Include(r => r.Seller)
                .ThenInclude(s => s.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reports);
        }

        // [POST] Escalate Report
        [HttpPost("escalate/{reportId}")]
        public async Task<IActionResult> EscalateReport(int reportId)
        {
            var report = await _db.Reports.FindAsync(reportId);
            if (report == null) return NotFound("Report not found.");

            report.Status = "Escalated";
            await _db.SaveChangesAsync();
            return Ok("Report escalated to Platform Admin.");
        }

        // [POST] Dismiss Report
        [HttpPost("dismiss/{reportId}")]
        public async Task<IActionResult> DismissReport(int reportId)
        {
            var report = await _db.Reports.FindAsync(reportId);
            if (report == null) return NotFound();

            report.Status = "Dismissed";
            await _db.SaveChangesAsync();
            return Ok("Report dismissed.");
        }

        // [GET] Dashboard Stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var topRejections = await _db.Products
                .Where(p => p.StockStatus == "Rejected")
                .GroupBy(p => p.Seller.ShopName)
                .OrderByDescending(g => g.Count())
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .FirstOrDefaultAsync();

            var flaggedCategory = await _db.Reports
                .Where(r => r.TargetType == "Product" && r.TargetProductId != null)
                .SelectMany(r => r.Product.Categories)
                .GroupBy(c => c.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                TopRejectedSeller = topRejections?.Name ?? "None",
                MostFlaggedCategory = flaggedCategory?.Name ?? "None",
                PendingReports = await _db.Reports.CountAsync(r => r.Status == "Pending")
            });
        }
    }
}