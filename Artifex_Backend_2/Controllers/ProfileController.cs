using Microsoft.AspNetCore.Mvc;
using Artifex_Backend_2.Data;
using Artifex_Backend_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Artifex_Backend_2.DTOs;

namespace Artifex_Backend_2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ProfileController : ControllerBase
    {
        private readonly ArtifexDbContext _db;

        public ProfileController(ArtifexDbContext db)
        {
            _db = db;
        }

        // ---------------- Customer Profile ----------------

        [HttpPost("customer")]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerProfileDto dto)
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Check if profile already exists
            if (await _db.Customers.AnyAsync(c => c.UserId == Guid.Parse(userId)))
                return Conflict("Customer profile already exists.");

            var customer = new Customer
            {
                UserId = Guid.Parse(userId),
                FullName = dto.FullName,
                ProfilePictureUrl = dto.ProfilePictureUrl,
                ShippingAddress = dto.ShippingAddress,
                PhoneNumber = dto.PhoneNumber
            };

            _db.Customers.Add(customer);

            // Update user's role
            var user = await _db.Users.FindAsync(Guid.Parse(userId));
            user.Role = UserRole.Customer;

            await _db.SaveChangesAsync();

            return Ok("Customer profile created successfully.");
        }

        // ---------------- Seller Profile ----------------

        [HttpPost("seller")]
        public async Task<IActionResult> CreateSeller([FromBody] SellerProfileDto dto)
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Check if profile already exists
            if (await _db.Sellers.AnyAsync(s => s.UserId == Guid.Parse(userId)))
                return Conflict("Seller profile already exists.");

            var seller = new Seller
            {
                UserId = Guid.Parse(userId),
                ShopName = dto.ShopName,
                Description = dto.Description,
                ShopLogo = dto.ShopLogo,
                Category = dto.Category,
                ContactNumber = dto.ContactNumber,
                Address = dto.Address,
                IsApproved = false,
                IsSuspended = false
            };

            _db.Sellers.Add(seller);

            // Update user's role
            var user = await _db.Users.FindAsync(Guid.Parse(userId));
            user.Role = UserRole.Seller;

            await _db.SaveChangesAsync();

            return Ok("Seller profile created successfully.");
        }
    }
}
