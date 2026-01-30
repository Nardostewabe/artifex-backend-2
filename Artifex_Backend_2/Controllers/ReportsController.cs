using Artifex_Backend_2.Data;
using Artifex_Backend_2.DTOs;
using Artifex_Backend_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Must be logged in to report
    public class ReportsController : ControllerBase
    {
        private readonly ArtifexDbContext _db;

        public ReportsController(ArtifexDbContext db)
        {
            _db = db;
        }

        // [POST] Report a Product
        [HttpPost("product/{productId}")]
        public async Task<IActionResult> ReportProduct(int productId, [FromBody] CreateReportDto dto)
        {
            var userIdString = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            var product = await _db.Products.FindAsync(productId);
            if (product == null) return NotFound("Product not found.");

            var report = new Report
            {
                ReporterId = userId,
                TargetType = "Product",
                TargetProductId = productId,
                Reason = dto.Reason,
                Description = dto.Description,
                Status = "Pending"
            };

            _db.Reports.Add(report);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Product reported. Admins will review it." });
        }

        // [POST] Report a Seller
        [HttpPost("seller/{sellerId}")]
        public async Task<IActionResult> ReportSeller(Guid sellerId, [FromBody] CreateReportDto dto)
        {
            var userIdString = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            var seller = await _db.Sellers.FindAsync(sellerId);
            if (seller == null) return NotFound("Seller not found.");

            var report = new Report
            {
                ReporterId = userId,
                TargetType = "Seller",
                TargetSellerId = sellerId,
                Reason = dto.Reason,
                Description = dto.Description,
                Status = "Pending"
            };

            _db.Reports.Add(report);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Seller reported." });
        }
    }
}