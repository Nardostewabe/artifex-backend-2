using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Artifex_Backend_2.Data;
using Artifex_Backend_2.Models;
using Artifex_Backend_2.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class OrderController : ControllerBase
    {
        private readonly ArtifexDbContext _context;

        public OrderController(ArtifexDbContext context)
        {
            _context = context;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            try
            {
                // 1. Validate Input
                if (dto.Items == null || !dto.Items.Any())
                    return BadRequest("Cart is empty.");

                var userIdString = User.FindFirstValue("id");
                if (string.IsNullOrEmpty(userIdString)) return Unauthorized("User ID missing from token.");

                var userId = Guid.Parse(userIdString);

                // 2. Find Customer Profile (Using UserId, NOT Primary Key)
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    return BadRequest("Customer profile not found. Please create a profile first.");
                }

                // 3. Process Items
                var orders = new List<Order>();
                decimal totalTransactionValue = 0;

                foreach (var item in dto.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);

                    if (product == null)
                        return BadRequest($"Product {item.ProductId} not found.");

                    if (product.StockQuantity < item.Quantity)
                        return BadRequest($"Insufficient stock for {product.Name}.");

                    // Update Stock
                    product.StockQuantity -= item.Quantity;
                    product.OrderCount += item.Quantity;
                    if (product.OrderCount >= 5) product.IsTrending = true;

                    // Create Order
                    var order = new Order
                    {
                        BuyerId = customer.Id, // Link to Customer Table
                        ProductId = product.Id, // Link to Product Table
                        Quantity = item.Quantity,
                        TotalPrice = product.Price * item.Quantity,
                        OrderDate = DateTime.UtcNow,
                        Status = "Processing"
                    };

                    orders.Add(order);
                    totalTransactionValue += order.TotalPrice;
                }

                // 4. Save to Database
                _context.Orders.AddRange(orders);
                await _context.SaveChangesAsync(); // <--- Crash usually happens here

                return Ok(new { message = "Checkout successful!", total = totalTransactionValue });
            }
            catch (Exception ex)
            {
                // THIS WILL SHOW THE REAL ERROR IN YOUR CONSOLE
                Console.WriteLine($"--------------- CHECKOUT ERROR ---------------");
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
                Console.WriteLine("----------------------------------------------");

                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
    }
}
