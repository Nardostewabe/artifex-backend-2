using Artifex_Backend_2.Data;
using Artifex_Backend_2.DTOs;
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
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly ArtifexDbContext _db;
        private readonly IChapaService _chapaService;

        public PaymentController(ArtifexDbContext db, IChapaService chapaService)
        {
            _db = db;
            _chapaService = chapaService;
        }

        // 1. Initialize Payment
        [HttpPost("initialize")]
        public async Task<IActionResult> InitializePayment([FromBody] InitializePaymentDto dto)
        {
            var userId = Guid.Parse(User.FindFirst("id")?.Value);
            var txRef = "TX-" + Guid.NewGuid().ToString().Substring(0, 8); // Unique Ref

            try
            {
                // Call Chapa API
                var checkoutUrl = await _chapaService.InitializeTransaction(
                    txRef, dto.Amount, dto.Email, dto.FirstName, dto.LastName
                );

                // Save Pending Payment to DB
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

        // 2. Verify Payment (Called by Frontend after redirection)
        [HttpGet("verify/{txRef}")]
        public async Task<IActionResult> VerifyPayment(string txRef)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.TxRef == txRef);
            if (payment == null) return NotFound("Transaction not found");

            // Check status with Chapa
            bool isSuccessful = await _chapaService.VerifyTransaction(txRef);

            if (isSuccessful)
            {
                payment.Status = "Success";
                // TODO: Add logic here to enable features (e.g., Approve Subscription)
            }
            else
            {
                payment.Status = "Failed";
            }

            await _db.SaveChangesAsync();
            return Ok(new { status = payment.Status });
        }
    }
}