using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Artifex_Backend_2.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // 1. Read Settings (Checks both Local ':' and Render '__')
                var host = _config["Smtp:Host"] ?? _config["Smtp__Host"];
                var portStr = _config["Smtp:Port"] ?? _config["Smtp__Port"];
                var email = _config["Smtp:Username"] ?? _config["Smtp__Username"];
                var password = _config["Smtp:Password"] ?? _config["Smtp__Password"];

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    _logger.LogError("❌ Email Config Missing. Check Render Environment Variables.");
                    return;
                }

                // Default to port 587 if missing (Best for Gmail)
                var port = int.TryParse(portStr, out int p) ? p : 587;

                // 2. Configure Client
                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(email, password),
                    EnableSsl = true
                };

                // 3. Create Message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(email, "Artifex Support"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                // 4. Send
                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"✅ Email sent to {toEmail}");
            }
            catch (Exception ex)
            {
                // ✅ CRITICAL FIX: We Log the error, but we DO NOT 'throw' it.
                // This stops the 500 Internal Server Error.
                _logger.LogError($"❌ FAILED TO SEND EMAIL: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"➡️ Inner Error: {ex.InnerException.Message}");
                }
            }
        }
    }
}