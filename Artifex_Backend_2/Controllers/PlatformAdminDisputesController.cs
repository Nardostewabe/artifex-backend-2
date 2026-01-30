using Artifex_Backend_2.Data;
using Artifex_Backend_2.DTOs;
using Artifex_Backend_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "4")]
    public class PlatformAdminDisputesController : ControllerBase
    {
        private readonly ArtifexDbContext _db;

        public PlatformAdminDisputesController(ArtifexDbContext db)
        {
            _db = db;
        }

        // [GET] List all open disputes
        [HttpGet]
        public async Task<IActionResult> GetDisputes()
        {
            var disputes = await _db.Disputes
                .Include(d => d.Complainant)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new DisputeDto // ✅ Project to DTO
                {
                    Id = d.Id,
                    Reason = d.Reason,
                    Status = d.Status,
                    OrderId = d.OrderId,
                    // Handle potential null Complainant gracefully
                    ComplainantName = d.Complainant != null ? d.Complainant.Username : "Unknown",
                    CreatedAt = d.CreatedAt.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Ok(disputes);
        }

        // [POST] Resolve/Close Dispute
        [HttpPost("{id}/resolve")]
        public async Task<IActionResult> ResolveDispute(int id, [FromBody] string resolutionNotes)
        {
            var dispute = await _db.Disputes.FindAsync(id);
            if (dispute == null) return NotFound();

            dispute.Status = "Resolved";
            dispute.AdminResolution = resolutionNotes;

            await _db.SaveChangesAsync();
            return Ok("Dispute resolved.");
        }

        // [GET] View Escalated Reports (Sent by Content Admin)
        [HttpGet("escalated-reports")]
        public async Task<IActionResult> GetEscalatedReports()
        {
            var reports = await _db.Reports
                .Where(r => r.Status == "Escalated") // Filter for escalated only
                .Include(r => r.Reporter)
                .Include(r => r.Product)
                .Include(r => r.Seller)
                .ThenInclude(s => s.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reports);
        }

        // [POST] Resolve Report (Final Action)
        [HttpPost("resolve-report/{reportId}")]
        public async Task<IActionResult> ResolveReport(int reportId)
        {
            var report = await _db.Reports.FindAsync(reportId);
            if (report == null) return NotFound();

            // Example Action: You might suspend the seller here using logic from SellerController
            // For now, we just mark the report as closed.

            report.Status = "Resolved";
            await _db.SaveChangesAsync();

            return Ok("Report resolved and closed.");
        }
    }
}