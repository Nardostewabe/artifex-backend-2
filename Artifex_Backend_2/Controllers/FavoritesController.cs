using Artifex_Backend_2.Data;
using Artifex_Backend_2.DTOs;
using Artifex_Backend_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All endpoints require login
    public class FavoritesController : ControllerBase
    {
        private readonly ArtifexDbContext _context;

        public FavoritesController(ArtifexDbContext context)
        {
            _context = context;
        }

        // 1. [GET] Get all favorited products for the logged-in user
        // Used for the "My Favorites" page
        [HttpGet]
        public async Task<IActionResult> GetMyFavorites()
        {
            var userIdString = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)             // Load Product details
                .ThenInclude(p => p.Images)          // Load Images (for the card)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => f.Product)              // Return the actual Product objects
                .ToListAsync();

            return Ok(favorites);
        }

        // 2. [GET] Check if specific product is favorited
        // Used by the Heart Icon on the product card
        [HttpGet("check/{productId}")]
        public async Task<IActionResult> CheckFavorite(int productId)
        {
            var userIdString = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userIdString)) return Ok(false); // Not logged in = not favorite
            var userId = Guid.Parse(userIdString);

            bool isFavorited = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.ProductId == productId);

            return Ok(isFavorited);
        }

        // 3. [POST] Add to Favorites
        [HttpPost]
        public async Task<IActionResult> AddFavorite([FromBody] FavoriteDto dto)
        {
            var userIdString = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            // Prevent duplicates
            if (await _context.Favorites.AnyAsync(f => f.UserId == userId && f.ProductId == dto.ProductId))
                return Conflict("Product already in favorites.");

            var favorite = new Favorite
            {
                UserId = userId,
                ProductId = dto.ProductId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return Ok("Added to favorites.");
        }

        // 4. [DELETE] Remove from Favorites
        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFavorite(int productId)
        {
            var userIdString = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (favorite == null) return NotFound("Favorite not found.");

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return Ok("Removed from favorites.");
        }
    }
}