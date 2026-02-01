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

        // [POST] Create Product (With Customization)
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
                Images = new List<ProductImage>(),

                // ✅ Customization Logic
                AllowColorCustomization = dto.StockStatus == "Made to Order" && dto.AllowColorCustomization,
                ColorOptions = (dto.StockStatus == "Made to Order" && dto.ColorOptions != null)
                               ? dto.ColorOptions
                               : new List<string>(),

                AllowSizeCustomization = dto.StockStatus == "Made to Order" && dto.AllowSizeCustomization,
                SizeOptions = (dto.StockStatus == "Made to Order" && dto.SizeOptions != null)
                              ? dto.SizeOptions
                              : new List<string>()
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

        // [GET] My Products
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
                .Include(p => p.Seller) // ✅ Include Seller
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(products);
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

        // [PUT] Update Product (With Customization)
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

            // ✅ Update Customization
            if (dto.StockStatus == "Made to Order")
            {
                product.AllowColorCustomization = dto.AllowColorCustomization;
                product.ColorOptions = dto.ColorOptions ?? new List<string>();

                product.AllowSizeCustomization = dto.AllowSizeCustomization;
                product.SizeOptions = dto.SizeOptions ?? new List<string>();
            }
            else
            {
                product.AllowColorCustomization = false;
                product.ColorOptions = new List<string>();
                product.AllowSizeCustomization = false;
                product.SizeOptions = new List<string>();
            }

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
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Categories)
                .Include(p => p.Seller) // ✅ Show Seller
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound("Product not found.");
            return Ok(product);
        }

        // [GET] All Products (Shop Page)
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Categories)
                .Include(p => p.Seller) // ✅ Show Seller
                .ToListAsync();
        }

        // [GET] Public: Get products by Seller (Safe for GUIDs)
        [HttpGet("seller/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSellerPublicProducts(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("ID is required.");

            if (!Guid.TryParse(id, out var inputGuid))
            {
                return BadRequest("Invalid ID format. Expected a GUID.");
            }

            Guid databaseSellerId;

            // Try UserId first
            var sellerByUserId = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == inputGuid);
            if (sellerByUserId != null)
            {
                databaseSellerId = sellerByUserId.Id;
            }
            else
            {
                // Try SellerId
                var sellerDirect = await _context.Sellers.FirstOrDefaultAsync(s => s.Id == inputGuid);
                if (sellerDirect != null)
                {
                    databaseSellerId = sellerDirect.Id;
                }
                else
                {
                    return NotFound("Seller profile not found.");
                }
            }

            var products = await _context.Products
                .Where(p => p.SellerId == databaseSellerId)
                .Include(p => p.Images)
                .Include(p => p.Categories)
                .Include(p => p.Seller) // ✅ Show Seller
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(products);
        }

        // [GET] Trending
        [HttpGet("trending")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTrendingProducts()
        {
            var trendingProducts = await _context.Products
                .Where(p => p.IsTrending)
                .Include(p => p.Images)
                .ToListAsync();

            return Ok(trendingProducts);
        }
    }
}