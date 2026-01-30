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
    public class ProductsController : ControllerBase
    {
        private readonly ArtifexDbContext _context;
        private readonly Cloudinary _cloudinary;

        public ProductsController(ArtifexDbContext context, IConfiguration config)
        {
            _context = context;
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        }

        // [POST] Create Product
        [HttpPost("new-product")]
        public async Task<IActionResult> CreateProduct([FromForm] ProductCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdString = User.FindFirstValue("id");
            if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized("Invalid token.");

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (seller == null || !seller.IsApproved || seller.IsSuspended)
                return Forbid("Seller account issue.");

            var product = new Product
            {
                SellerId = seller.Id,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Categories = new List<Category>(),
                StockQuantity = dto.StockQuantity,
                StockStatus = dto.StockStatus,
                Tags = dto.Tags,
                TutorialLink = dto.TutorialLink,
                CreatedAt = DateTime.UtcNow,
                Images = new List<ProductImage>()
            };

            if (dto.CategoryIds != null && dto.CategoryIds.Any())
            {
                var categories = await _context.Categories
                    .Where(c => dto.CategoryIds.Contains(c.Id))
                    .ToListAsync();

                product.Categories = categories;
            }

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
                    product.Images.Add(new ProductImage { Url = uploadResult.SecureUrl.ToString() });
                }
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product created successfully", productId = product.Id });
        }

        // [GET] My Products (Seller Only)
        [HttpGet("my-products")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "2")]
        public async Task<IActionResult> GetSellerProducts()
        {
            var userIdString = User.FindFirstValue("id");
            if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized("Invalid token.");

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (seller == null) return NotFound("Seller profile not found.");

            var products = await _context.Products
                .Where(p => p.SellerId == seller.Id)
                .Include(p => p.Images)
                .Include(p => p.Categories)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(products);
        }

        // [DELETE] Delete Product (Seller only)
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "2")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var userIdString = User.FindFirstValue("id");
            if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (seller == null) return Forbid();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (product.SellerId != seller.Id) return Forbid("You do not own this product.");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // [PUT] Update Product
        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "2")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductUpdateDto dto)
        {
            var userIdString = User.FindFirstValue("id");
            if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (seller == null) return Forbid();

            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound("Product not found.");
            if (product.SellerId != seller.Id) return Forbid("You do not own this product.");

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.StockStatus = dto.StockStatus;
            product.Tags = dto.Tags;
            product.TutorialLink = dto.TutorialLink;

            if (dto.CategoryIds != null)
            {
                product.Categories.Clear();
                var newCategories = await _context.Categories
                    .Where(c => dto.CategoryIds.Contains(c.Id))
                    .ToListAsync();
                foreach (var cat in newCategories) product.Categories.Add(cat);
            }

            var keepIds = dto.KeepImageIds ?? new List<int>();
            var imagesToDelete = product.Images.Where(img => !keepIds.Contains(img.Id)).ToList();
            foreach (var img in imagesToDelete) _context.ProductImages.Remove(img);

            if (dto.NewImages != null)
            {
                foreach (var file in dto.NewImages)
                {
                    if (file.Length <= 0) continue;
                    using var stream = file.OpenReadStream();
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Transformation = new Transformation().Width(800).Crop("limit")
                    };
                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    product.Images.Add(new ProductImage
                    {
                        Url = uploadResult.SecureUrl.ToString(),
                        ProductId = product.Id
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(product);
        }

        // [GET] Public Product Details
        // ✅ Ensure anyone can view details
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Categories)
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound("Product not found.");
            return Ok(product);
        }

        
        // [POST] Buy Product (Customer Only)
        [HttpPost("{id}/buy")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "1")]
        public async Task<IActionResult> BuyProduct(int id)
        {
            // 1. Get User ID from Token
            var userIdString = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized("Token is missing ID.");

            var userId = Guid.Parse(userIdString);

            // 2. ✅ SECURITY CHECK: Does this User actually exist in the main Users table?
            // (This fixes the crash if you are using an old token)
            var mainUser = await _context.Users.FindAsync(userId); // Assuming you used IdentityUser or AppUser
            if (mainUser == null)
            {
                return Unauthorized("User account no longer exists. Please Log Out and Log In again.");
            }

            // 3. Ensure Customer Profile Exists
            var customer = await _context.Customers.FindAsync(userId);
            if (customer == null)
            {
                customer = new Customer
                {
                    Id = userId,
                    UserId = userId
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync(); // This creates the customer profile
            }

            // 4. Find Product and Create Order
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Product not found.");
            if (product.StockQuantity < 1) return BadRequest("Out of stock.");

            var newOrder = new Order
            {
                BuyerId = userId,
                ProductId = product.Id,
                Quantity = 1,
                TotalPrice = product.Price,
                OrderDate = DateTime.UtcNow,
                Status = "Pending"
            };

            product.StockQuantity--;
            product.OrderCount++;
            if (product.OrderCount >= 5) product.IsTrending = true;

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order placed successfully!" });
        }

        // [GET] Trending
        [HttpGet("trending")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTrendingProducts()
        {
            var trendingProducts = await _context.Products
    
                .Where(p => p.IsTrending)
                .Include(p => p.Images) // Include images for UI
                .ToListAsync();

            return Ok(trendingProducts);
        }

        // [GET] All Products (Shop Page)
        [HttpGet]
        [AllowAnonymous] // ✅ FIX: Added this so Shop page loads without login
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Categories)
                .ToListAsync();
        }

        // [GET] Products by Specific Seller (Public Shop Page)
        [HttpGet("seller/{sellerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductsBySeller(Guid sellerId)
        {
            var products = await _context.Products
                .Where(p => p.SellerId == sellerId)
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(products);
        }
    }
}