using Artifex_Backend_2.Data;
using Artifex_Backend_2.Models;
using Artifex_Backend_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ArtifexDbContext _db;
        private readonly IChapaService _chapaService;
        private readonly IInvoiceService _invoiceService;

        public PaymentController(ArtifexDbContext db, IChapaService chapaService, IInvoiceService invoiceService)
        {
            _db = db;
            _chapaService = chapaService;
            _invoiceService = invoiceService;
        }

        // [POST] Initialize Payment
        // ✅ NOW ACCEPTS 'Amount' FROM FRONTEND
        [HttpPost("initialize")]
        [Authorize]
        public async Task<IActionResult> InitializePayment([FromBody] PaymentRequest request)
        {
            try
            {
                // 1. Get User Info from Token
                var userIdString = User.FindFirstValue("id");
                var email = User.FindFirstValue(ClaimTypes.Email);
                var username = User.FindFirstValue(ClaimTypes.Name);

                if (string.IsNullOrEmpty(userIdString) || string.IsNullOrEmpty(email))
                {
                    return Unauthorized("User details missing in token.");
                }

                var userId = Guid.Parse(userIdString);

                // 2. Use the Amount sent from Frontend
                // We use 'request.Amount' instead of hardcoded 1000
                if (request.Amount <= 0) return BadRequest("Invalid amount.");

                var txRef = "TX-" + Guid.NewGuid().ToString().Substring(0, 8);

                var names = (username ?? "Guest User").Split(' ');
                var fName = names[0];
                var lName = names.Length > 1 ? names[1] : "User";

                // 3. Call Chapa API
                var checkoutUrl = await _chapaService.InitializeTransaction(
                    txRef, request.Amount, email, fName, lName
                );

                // 4. Save to Database
                try
                {
                    var payment = new Payment
                    {
                        UserId = userId,
                        TxRef = txRef,
                        Amount = request.Amount,
                        Email = email,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Payments.Add(payment);
                    await _db.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"DB Error: {dbEx.Message}");
                }

                // Return camelCase 'checkoutUrl' for frontend
                return Ok(new { checkoutUrl = checkoutUrl, txRef = txRef });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("verify/{txRef}")]
        public async Task<IActionResult> VerifyPayment(string txRef)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.TxRef == txRef);
            bool isSuccessful = await _chapaService.VerifyTransaction(txRef);

            if (payment != null)
            {
                payment.Status = isSuccessful ? "Success" : "Failed";
                await _db.SaveChangesAsync();
            }

            return Ok(new { status = isSuccessful ? "Success" : "Failed" });
        }

        [HttpGet("{txRef}/invoice")]
        public async Task<IActionResult> DownloadInvoice(string txRef)
        {
            var payment = await _db.Payments
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.TxRef == txRef);

            if (payment == null || payment.Status != "Success") return BadRequest("Invoice not available.");

            var pdfBytes = _invoiceService.GenerateInvoice(payment, payment.User);
            return File(pdfBytes, "application/pdf", $"Invoice-{txRef}.pdf");
        }
    }

    // ✅ Simple DTO to accept just the Amount
    public class PaymentRequest
    {
        public decimal Amount { get; set; }
    }
}