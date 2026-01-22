using Artifex_Backend_2.Data;
using Artifex_Backend_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Artifex_Backend_2.Controllers
{   
        [Route("api/[controller]")]
        [ApiController]
        public class CategoriesController : ControllerBase
        {
            private readonly ArtifexDbContext _context;

            public CategoriesController(ArtifexDbContext context)
            {
                _context = context;
            }

            [HttpGet]
            [AllowAnonymous] // Shoppers don't need to login to see categories
            public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
            {
                return await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            }

            [HttpPost]
            [Authorize] // Only logged-in users can add categories
            public async Task<ActionResult<Category>> PostCategory(Category category)
            {
                // Prevent duplicates
                if (_context.Categories.Any(c => c.Name == category.Name))
                {
                    return Conflict("Category already exists.");
                }

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetCategories", new { id = category.Id }, category);
            }
        }

    }
