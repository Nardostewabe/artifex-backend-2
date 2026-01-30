using Artifex_Backend_2.Services;
using Microsoft.AspNetCore.Mvc;

namespace Artifex_Backend_2.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestEmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public TestEmailController(IEmailService emailService, IConfiguration config)
        {
            _emailService = emailService;
            _config = config;
        }

        [HttpGet("send-email")]
        public async Task<IActionResult> TestEmail()
        {
            // 1. Check if config is actually being read
            var user = _config["Smtp:Username"] ?? _config["Smtp__Username"];
            var hasPassword = !string.IsNullOrEmpty(_config["Smtp:Password"] ?? _config["Smtp__Password"]);

            if (string.IsNullOrEmpty(user))
                return BadRequest("❌ Error: Smtp:Username is null. Check Render Environment Variables.");

            if (!hasPassword)
                return BadRequest("❌ Error: Smtp:Password is missing. Check Render Environment Variables.");

            try
            {
                // 2. Try to send
                await _emailService.SendEmailAsync(user, "Test from Render", "If you see this, email is working!");
                return Ok($"✅ Success! Email sent to {user}. Config loaded correctly.");
            }
            catch (Exception ex)
            {
                // 3. Return the exact error
                return StatusCode(500, $"❌ Sending Failed: {ex.Message} | Inner: {ex.InnerException?.Message}");
            }
        }
    }
}