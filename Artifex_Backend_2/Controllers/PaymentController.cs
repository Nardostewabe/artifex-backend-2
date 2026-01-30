using Artifex_Backend_2.Data;
using Artifex_Backend_2.DTOs;
using Artifex_Backend_2.Models;
using Artifex_Backend_2.Services; // ✅ Required for IInvoiceService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly ArtifexDbContext _db;
        private readonly IChapaService _chapaService;
        private readonly IInvoiceService _invoiceService; // ✅ 1. Add Service Field

        // ✅ 2. Inject IInvoiceService in Constructor
        public PaymentController(ArtifexDbContext db, IChapaService chapaService, IInvoiceService invoiceService)
        {
            _db = db;
            _chapaService = chapaService;
            _invoiceService = invoiceService;
        }

        // 1. Initialize Payment (Kept exactly as you had it)
        [HttpPost("initialize")]
        public async Task<IActionResult> InitializePayment([FromBody] InitializePaymentDto dto)
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value);
            var txRef = "TX-" + Guid.NewGuid().ToString().Substring(0, 8);

            try
            {
                var checkoutUrl = await _chapaService.InitializeTransaction(
                    txRef, dto.Amount, dto.Email, dto.FirstName, dto.LastName
                );

                var payment = new Payment
                {
                    UserId = userId,
                    TxRef = txRef,
                    Amount = dto.Amount,
                    Email = dto.Email,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _db.Payments.Add(payment);
                await _db.SaveChangesAsync();

                return Ok(new { checkoutUrl, txRef });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 2. Verify Payment (Kept exactly as you had it)
        [HttpGet("verify/{txRef}")]
        public async Task<IActionResult> VerifyPayment(string txRef)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.TxRef == txRef);
            if (payment == null) return NotFound("Transaction not found");

            bool isSuccessful = await _chapaService.VerifyTransaction(txRef);

            if (isSuccessful)
            {
                payment.Status = "Success";
            }
            else
            {
                payment.Status = "Failed";
            }

            await _db.SaveChangesAsync();
            return Ok(new { status = payment.Status });
        }

        // ✅ 3. NEW: Download Invoice Endpoint
        [HttpGet("{txRef}/invoice")]
        public async Task<IActionResult> DownloadInvoice(string txRef)
        {
            // We include the User to get their name for the "Bill To" section
            var payment = await _db.Payments
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.TxRef == txRef);

            if (payment == null) return NotFound("Payment not found.");

            // Optional: Only allow downloading if payment was successful
            if (payment.Status != "Success") return BadRequest("Cannot generate invoice for unpaid transaction.");

            // Generate the PDF bytes
            var pdfBytes = _invoiceService.GenerateInvoice(payment, payment.User);

            // Return as a downloadable file
            return File(pdfBytes, "application/pdf", $"Invoice-{txRef}.pdf");
        }
    }
}