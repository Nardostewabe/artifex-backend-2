using Artifex_Backend_2.Data;
using Artifex_Backend_2.DTOs;
using Artifex_Backend_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class ProductsController : ControllerBase
    {
        private readonly ArtifexDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(ArtifexDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "2")] // or "Seller"
        [HttpPost("new-product")]
        public async Task<IActionResult> CreateProduct([FromForm] ProductCreateDto productDto)
        {
            try
            {
                // 1. Validation & User ID Logic
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var sellerIdString = User.FindFirstValue("sub") ?? User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(sellerIdString) || !Guid.TryParse(sellerIdString, out Guid sellerIdGuid))
                    return Unauthorized("User ID invalid.");

                var cloudName = "dara9iyzd";
                var apiKey = "878485448233523";
                var apiSecret = "niQwJswcShM7KM1G9vpPqhicfYA";

                Account account = new Account(cloudName, apiKey, apiSecret);
                Cloudinary cloudinary = new Cloudinary(account);
                cloudinary.Api.Secure = true;

                // 2. Create Product Entity
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
                    TutorialLink = productDto.TutorialLink,
                    CreatedAt = DateTime.UtcNow
                };

                // 3. SAFE FILE UPLOAD (Fixes 500 Error)
                if (productDto.Images != null && productDto.Images.Count > 0)
                {
                    foreach (var file in productDto.Images)
                    {
                        if (file.Length > 0)
                        {
                            // "OpenReadStream" reads the file from memory 
                            // It does NOT save to the disk, so Render won't crash!
                            using (var stream = file.OpenReadStream())
                            {
                                var uploadParams = new ImageUploadParams()
                                {
                                    File = new FileDescription(file.FileName, stream),
                                    // Optional: Crop huge images to 500px wide to save space
                                    Transformation = new Transformation().Width(500).Crop("limit")
                                };

                                // Send to Cloudinary
                                var uploadResult = await cloudinary.UploadAsync(uploadParams);

                                // Save the resulting URL (e.g., https://res.cloudinary.com/...)
                                product.Images.Add(new ProductImage
                                {
                                    Url = uploadResult.SecureUrl.ToString()
                                });
                            }
                        }
                    }
                }

                // 4. Save to DB
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                // Return detailed error if it still crashes
                return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
            }
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

