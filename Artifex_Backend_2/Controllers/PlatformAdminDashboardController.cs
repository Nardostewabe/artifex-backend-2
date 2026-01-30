using Artifex_Backend_2.Data;
using Artifex_Backend_2.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "4")] // Admin Only
    public class PlatformAdminDashboardController : ControllerBase
    {
        private readonly ArtifexDbContext _db;

        public PlatformAdminDashboardController(ArtifexDbContext db)
        {
            _db = db;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<AdminDashboardStatsDto>> GetDashboardStats()
        {
            var today = DateTime.UtcNow;
            var sevenDaysAgo = today.AddDays(-7);

            // 1. Basic Counters
            var totalUsers = await _db.Users.CountAsync();
            var totalSellers = await _db.Sellers.CountAsync();
            var pendingDisputes = await _db.Disputes.CountAsync(d => d.Status == "Open");

            // 2. Revenue (Sum of all completed orders)
            var revenue = await _db.Orders
                .Where(o => o.Status != "Cancelled")
                .SumAsync(o => o.TotalPrice);

            // 3. Chart Data: New Users per day (Last 7 Days)
            var newUsersChart = await _db.Users
                .Where(u => u.CreatedAt >= sevenDaysAgo)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new ChartDataDto
                {
                    Date = g.Key.ToString("MM-dd"),
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(new AdminDashboardStatsDto
            {
                TotalUsers = totalUsers,
                TotalSellers = totalSellers,
                TotalRevenue = revenue,
                PendingDisputes = pendingDisputes,
                NewUsersLast7Days = newUsersChart
            });
        }
    }
}