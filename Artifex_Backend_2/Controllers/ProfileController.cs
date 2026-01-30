using Microsoft.AspNetCore.Mvc;
using Artifex_Backend_2.Data;
using Artifex_Backend_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Artifex_Backend_2.DTOs;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Artifex_Backend_2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ProfileController : ControllerBase
    {
        private readonly ArtifexDbContext _db;
        private readonly Cloudinary _cloudinary;

        // ✅ ONLY ONE CONSTRUCTOR ALLOWED
        public ProfileController(ArtifexDbContext db, IConfiguration config)
        {
            _db = db;

            // Initialize Cloudinary
            // MAKE SURE these keys exist in your appsettings.json
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        }

        // ---------------- CUSTOMER ENDPOINTS ----------------

        [HttpPost("customer")]
        public async Task<IActionResult> CreateCustomer([FromForm] CustomerProfileDto dto)
        {
            var userIdString = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            if (await _db.Customers.AnyAsync(c => c.UserId == userId))
                return Conflict("Customer profile already exists.");

            // Upload Image
            string profilePicUrl = null;
            if (dto.ProfilePicture != null && dto.ProfilePicture.Length > 0)
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(dto.ProfilePicture.FileName, dto.ProfilePicture.OpenReadStream()),
                    Transformation = new Transformation().Width(500).Height(500).Crop("fill")
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                profilePicUrl = uploadResult.SecureUrl.ToString();
            }

            var customer = new Customer
            {
                UserId = userId,
                FullName = dto.FullName,
                ProfilePictureUrl = profilePicUrl,
                ShippingAddress = dto.ShippingAddress,
                PhoneNumber = dto.PhoneNumber
            };

            _db.Customers.Add(customer);

            var user = await _db.Users.FindAsync(userId);
            if (user != null) user.Role = UserRole.Customer;

            await _db.SaveChangesAsync();
            return Ok("Customer profile created.");
        }

        [HttpGet("customer")]
        public async Task<IActionResult> GetMyCustomerProfile()
        {
            var userIdString = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return NotFound("Profile not found.");

            // Return DTO to prevent JSON loops
            return Ok(new
            {
                customer.FullName,
                customer.ProfilePictureUrl,
                customer.ShippingAddress,
                customer.PhoneNumber
            });
        }


        // ---------------- UPDATE CUSTOMER ----------------

        [HttpPut("customer")]
        public async Task<IActionResult> UpdateCustomer([FromForm] CustomerProfileDto dto)
        {
            var userIdString = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return NotFound("Profile not found.");

            // Update text fields
            customer.FullName = dto.FullName;
            customer.ShippingAddress = dto.ShippingAddress;
            customer.PhoneNumber = dto.PhoneNumber;

            // Update Image ONLY if a new one is uploaded
            if (dto.ProfilePicture != null && dto.ProfilePicture.Length > 0)
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(dto.ProfilePicture.FileName, dto.ProfilePicture.OpenReadStream()),
                    Transformation = new Transformation().Width(500).Height(500).Crop("fill")
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                customer.ProfilePictureUrl = uploadResult.SecureUrl.ToString();
            }

            await _db.SaveChangesAsync();
            return Ok("Profile updated successfully.");
        }

        // [GET] Public Shop Profile (For Customers to view a Shop)
        // [GET] Public Shop Profile
        // Fixed: Now accepts the SellerId (which is what the frontend sends)
        [HttpGet("seller/{sellerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSellerShopProfile(Guid sellerId)
        {
            // 1. Try to find by Seller ID (The most likely case)
            var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.Id == sellerId);

            // 2. Fallback: If not found, check if the frontend sent a User ID by mistake
            if (seller == null)
            {
                seller = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == sellerId);
            }

            if (seller == null) return NotFound("Shop not found.");

            return Ok(new
            {
                seller.Id,
                seller.UserId, // Return both just in case
                seller.ShopName,
                seller.Description,
                seller.ShopLogo,
                seller.Category,
                seller.Address,
                seller.ContactNumber,
                seller.IsApproved
            });
        }
        // ---------------- SELLER ENDPOINTS ----------------

        [HttpPost("seller")]
        public async Task<IActionResult> CreateSeller([FromForm] SellerProfileDto dto)
        {
            var userIdString = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            if (await _db.Sellers.AnyAsync(s => s.UserId == userId))
                return Conflict("Seller profile already exists.");

            // Upload Logo
            string logoUrl = null;
            if (dto.ShopLogo != null && dto.ShopLogo.Length > 0)
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(dto.ShopLogo.FileName, dto.ShopLogo.OpenReadStream()),
                    Transformation = new Transformation().Width(500).Height(500).Crop("fill")
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                logoUrl = uploadResult.SecureUrl.ToString();
            }

            var seller = new Seller
            {
                UserId = userId,
                ShopName = dto.ShopName,
                Description = dto.Description,
                ShopLogo = logoUrl,
                Category = dto.Category,
                ContactNumber = dto.ContactNumber,
                Address = dto.Address,
                IsApproved = false,
                IsSuspended = false,
                IsDeactivated = false
            };

            _db.Sellers.Add(seller);

            var user = await _db.Users.FindAsync(userId);
            if (user != null) user.Role = UserRole.Seller;

            await _db.SaveChangesAsync();
            return Ok("Seller profile created.");
        }

        [HttpGet("seller")]
        public async Task<IActionResult> GetMySellerProfile()
        {
            var userIdString = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (seller == null) return NotFound("Shop not found.");

            // ✅ FIX: Add ContactNumber and Address here so the frontend can receive them
            return Ok(new
            {
                seller.ShopName,
                seller.ShopLogo,
                seller.Description,
                seller.Category,
                seller.IsApproved,
                seller.ContactNumber, 
                seller.Address        
            });
        }


        // ---------------- UPDATE SELLER ----------------

        [HttpPut("seller")]
        public async Task<IActionResult> UpdateSeller([FromForm] SellerProfileDto dto)
        {
            var userIdString = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (seller == null) return NotFound("Shop not found.");

            // Update text fields
            seller.ShopName = dto.ShopName;
            seller.Description = dto.Description;
            seller.Category = dto.Category;
            seller.ContactNumber = dto.ContactNumber;
            seller.Address = dto.Address;

            // Update Logo ONLY if a new one is uploaded
            if (dto.ShopLogo != null && dto.ShopLogo.Length > 0)
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(dto.ShopLogo.FileName, dto.ShopLogo.OpenReadStream()),
                    Transformation = new Transformation().Width(500).Height(500).Crop("fill")
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                seller.ShopLogo = uploadResult.SecureUrl.ToString();
            }

            await _db.SaveChangesAsync();
            return Ok("Shop updated successfully.");
        }
    }
}