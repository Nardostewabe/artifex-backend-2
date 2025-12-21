using Artifex_Backend_2.Data;
using Artifex_Backend_2.DTOs;
using Artifex_Backend_2.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Artifex_Backend_2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ProductsController : ControllerBase
    {
        private readonly ArtifexDbContext _context;
        private readonly Cloudinary _cloudinary;

        public ProductsController(ArtifexDbContext context, IConfiguration config)
        {
            _context = context;

            // Cloudinary config (Render-safe)
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account)
            {
                Api = { Secure = true }
            };
        }

        // ===========================
        // CREATE PRODUCT (SELLER ONLY)
        // ===========================
        [HttpPost("new-product")]
        public async Task<IActionResult> CreateProduct([FromForm] ProductCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ✅ 1. Extract USER ID from JWT (CORRECT CLAIM)
            var userIdString = User.FindFirstValue("id");
            if (!Guid.TryParse(userIdString, out var userId))
                return Unauthorized("Invalid token.");

            // ✅ 2. Get SELLER profile
            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
                return Forbid("You are not registered as a seller.");

            if (!seller.IsApproved)
                return Forbid("Seller account not approved.");

            if (seller.IsSuspended)
                return Forbid("Seller account is suspended.");

            // ✅ 3. Create Product
            var product = new Product
            {
                SellerId = seller.Id, // 🔥 CORRECT FK
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Category = dto.Category,
                StockQuantity = dto.StockQuantity,
                StockStatus = dto.StockStatus,
                Tags = dto.Tags,
                TutorialLink = dto.TutorialLink,
                CreatedAt = DateTime.UtcNow,
                Images = new List<ProductImage>() // 🔥 PREVENT NULL CRASH
            };

            // ✅ 4. Upload Images to Cloudinary (NO local disk usage)
            if (dto.Images != null && dto.Images.Count > 0)
            {
                foreach (var file in dto.Images)
                {
                    if (file.Length <= 0) continue;

                    using var stream = file.OpenReadStream();

                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Transformation = new Transformation().Width(800).Crop("limit")
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                        return StatusCode(500, "Image upload failed.");

                    product.Images.Add(new ProductImage
                    {
                        Url = uploadResult.SecureUrl.ToString()
                    });
                }
            }

            // ✅ 5. Save
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductById),
                new { id = product.Id },
                product);
        }

        // ===========================
        // GET PRODUCT BY ID
        // ===========================
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound("Product not found.");

            return Ok(product);
        }
    }
}







                var cloudName = "dara9iyzd";
                var apiKey = "878485448233523";
                var apiSecret = "niQwJswcShM7KM1G9vpPqhicfYA";

             

