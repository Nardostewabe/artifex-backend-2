using Artifex_Backend_2.Data;
using Artifex_Backend_2.DTOs;
using Artifex_Backend_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize (AuthenticationSchemes = "Bearer", Roles = "2" )]
    public class ProductsController : ControllerBase
    {
        private readonly ArtifexDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(ArtifexDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpPost("new-product")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "2")]
        public async Task<IActionResult> CreateProduct([FromForm] ProductCreateDto productDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1. Get current logged-in user ID (Seller)
            var sellerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (sellerIdString == null) return Unauthorized();

            if (!Guid.TryParse(sellerIdString, out Guid sellerIdGuid))
            {
                return BadRequest("Invalid User ID format");
            }

            // 2. Map DTO to Entity
            var product = new Product
            {
                SellerId = sellerIdGuid,
                Name = productDto.Name,
                Description = productDto.Description,
                Price = productDto.Price,
                Category = productDto.Category,
                StockQuantity = productDto.StockQuantity,
                StockStatus = productDto.StockStatus,
                Tags = productDto.Tags,
                TutorialLink = productDto.TutorialLink
            };

            // 3. Handle Image Uploads
            // In a real app, upload to Azure Blob Storage / AWS S3 / Cloudinary here.
            // For this example, we save to the local 'wwwroot/images' folder.
            if (productDto.Images != null && productDto.Images.Count > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                foreach (var file in productDto.Images)
                {
                    if (file.Length > 0)
                    {
                        // Generate unique filename
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Save file to disk
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        // Add to Product Images List
                        product.Images.Add(new ProductImage
                        {
                            Url = $"/images/products/{uniqueFileName}"
                        });
                    }
                }
            }

            // 4. Save to Database
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }
    }
}
