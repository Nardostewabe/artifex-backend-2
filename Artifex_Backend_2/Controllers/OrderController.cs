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

        // 1. GET: View all orders for the logged-in Seller
        [Authorize(Roles = "2")] // Role 2 = Seller
        [HttpGet("seller-orders")]
        public async Task<ActionResult<IEnumerable<SellerOrderDto>>> GetSellerOrders()
        {
            var userIdString = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            // 1. Find the Seller ID associated with this User
            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (seller == null) return BadRequest("Seller profile not found.");

            // 2. Fetch Orders where the Product belongs to THIS Seller
            var orders = await _context.Orders
                .Include(o => o.Product)
                    .ThenInclude(p => p.Images) // To show product thumbnail
                .Include(o => o.Buyer) // To show Customer Name/Address
                .Where(o => o.Product.SellerId == seller.Id)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new SellerOrderDto
                {
                    OrderId = o.Id,
                    ProductName = o.Product.Name,
                    ProductImage = o.Product.Images.FirstOrDefault().Url, // Get first image
                    Quantity = o.Quantity,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status,
                    OrderDate = o.OrderDate,
                    BuyerName = o.Buyer.FullName ?? "Guest",
                    ShippingAddress = o.Buyer.ShippingAddress ?? "No address provided"
                })
                .ToListAsync();

            return Ok(orders);
        }

        // 2. PUT: Update Order Status (e.g. "Shipped")
        [Authorize(Roles = "2")]
        [HttpPut("{orderId}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusDto dto)
        {
            var userIdString = User.FindFirstValue("id");
            var userId = Guid.Parse(userIdString);

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (seller == null) return Unauthorized();

            // Find the order and include product to verify ownership
            var order = await _context.Orders
                .Include(o => o.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound("Order not found.");

            // SECURITY CHECK: Ensure this order is for a product owned by this seller
            if (order.Product.SellerId != seller.Id)
            {
                return Forbid("You are not authorized to manage this order.");
            }

            order.Status = dto.NewStatus;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Order status updated to {dto.NewStatus}" });
        }
        // 3. GET: View all orders for the logged-in Customer
        [Authorize(Roles = "1")] // Role 1 = Customer
        [HttpGet("customer-orders")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetCustomerOrders()
        {
            var userIdString = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return BadRequest("Customer profile not found.");

            var orders = await _context.Orders
                .Include(o => o.Product)
                    .ThenInclude(p => p.Images)
                .Where(o => o.BuyerId == customer.Id)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new 
                {
                    OrderId = o.Id,
                    ProductName = o.Product.Name,
                    ProductImage = o.Product.Images.FirstOrDefault().Url,
                    Quantity = o.Quantity,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status,
                    OrderDate = o.OrderDate,
                    SellerName = o.Product.Seller.ShopName
                })
                .ToListAsync();

            return Ok(orders);
        }
    }
}
