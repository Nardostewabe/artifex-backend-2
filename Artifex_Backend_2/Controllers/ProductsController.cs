using Artifex_Backend_2.Data;
using Artifex_Backend_2.DTOs;
using Artifex_Backend_2.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "2")]
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

        // [GET] All Products (Shop Page) - UPDATED FOR SELLER NAME
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Categories)
                .Include(p => p.Seller)
                    .ThenInclude(s => s.User) // ✅ Get the name from the User table
                .ToListAsync();

            var result = products.Select(p => new {
                p.Id,
                p.Name,
                p.Price,
                p.Description,
                Images = p.Images.Select(img => img.Url),
                SellerName = p.Seller?.ShopName ?? p.Seller?.User?.Username ?? "Unknown Shop",
                Categories = p.Categories.Select(c => c.Name)
            });

            return Ok(result);
        }

        // [GET] Public Product Details - UPDATED FOR MODAL
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Categories)
                .Include(p => p.Seller)
                    .ThenInclude(s => s.User) // ✅ JOIN logic for name
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound("Product not found.");

            return Ok(new
            {
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.StockQuantity,
                product.StockStatus,
                product.TutorialLink,
                product.Tags,
                Images = product.Images.Select(img => img.Url),
                Categories = product.Categories.Select(c => c.Name),
                SellerName = product.Seller?.ShopName ?? product.Seller?.User?.Username ?? "Unknown Shop",
                SellerId = product.SellerId
            });
        }

        // [GET] My Products (Seller Dashboard)
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

        // [POST] Buy Product (Customer Only)
        [HttpPost("{id}/buy")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "1")]
        public async Task<IActionResult> BuyProduct(int id)
        {
            var userIdString = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized("Token is missing ID.");

            var userId = Guid.Parse(userIdString);
            var mainUser = await _context.Users.FindAsync(userId);
            if (mainUser == null) return Unauthorized("User account no longer exists.");

            var customer = await _context.Customers.FindAsync(userId);
            if (customer == null)
            {
                customer = new Customer { Id = userId, UserId = userId };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

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
                .Include(p => p.Images)
                .Include(p => p.Seller)
                    .ThenInclude(s => s.User)
                .ToListAsync();

            var result = trendingProducts.Select(p => new {
                p.Id,
                p.Name,
                p.Price,
                Thumbnail = p.Images.FirstOrDefault()?.Url,
                SellerName = p.Seller?.ShopName ?? p.Seller?.User?.Username ?? "Unknown Shop"
            });

            return Ok(result);
        }

        // [DELETE] Delete Product
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
    }
}
