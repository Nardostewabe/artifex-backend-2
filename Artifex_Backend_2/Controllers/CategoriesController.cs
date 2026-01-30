using Artifex_Backend_2.Data;
using Artifex_Backend_2.DTOs;
using Artifex_Backend_2.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ArtifexDbContext _context;
        private readonly Cloudinary _cloudinary;

        public CategoriesController(ArtifexDbContext context, IConfiguration config)
        {
            _context = context;

            // Initialize Cloudinary
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        }

        // [GET] Public - Anyone can see categories
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        }

        // [POST] Content Admin Only (Role "3")
        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "3")]
        public async Task<ActionResult<Category>> PostCategory([FromForm] CategoryCreateDto dto)
        {
            // 1. Check for Duplicates
            if (await _context.Categories.AnyAsync(c => c.Name == dto.Name))
            {
                return Conflict("Category already exists.");
            }

            // 2. Upload Image to Cloudinary
            string imageUrl = null;
            if (dto.Image != null && dto.Image.Length > 0)
            {
                using var stream = dto.Image.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(dto.Image.FileName, stream),
                    // Resize to a standard box size (e.g., 400x400) for consistency
                    Transformation = new Transformation().Width(400).Height(400).Crop("fill")
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                imageUrl = uploadResult.SecureUrl.ToString();
            }

            // 3. Save to Database
            var category = new Category
            {
                Name = dto.Name,
                ImageUrl = imageUrl
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCategories", new { id = category.Id }, category);
        }

        // [DELETE] Content Admin Only
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "3")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok("Category deleted.");
        }
    }
}