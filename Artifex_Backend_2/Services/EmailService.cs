using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

namespace Artifex_Backend_2.Services
{
    // Interface
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

    // Implementation
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        public EmailService(IConfiguration config,ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // 1. Read Settings
                
      
                var host = _config["Smtp:Host"] ?? _config["Smtp__Host"];
                var port = int.TryParse(_config["Smtp:Port"] ?? _config["Smtp__Port"], out int p) ? p : 587;
                var email = _config["Smtp:Username"] ?? _config["Smtp__Username"];
                var password = _config["Smtp:Password"] ?? _config["Smtp__Password"];

                // 2. Configure Client
                var client = new SmtpClient(host, port)
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
                Console.WriteLine($"[EmailService] Sent to {toEmail}");
            }
            catch (Exception ex)
            {
                // 👇 THIS IS THE CRITICAL CHANGE 👇
                _logger.LogError($"❌ EMAIL FAILED to {toEmail}");
                _logger.LogError($"➡️ Main Error: {ex.Message}");

                if (ex.InnerException != null)
                {
                    _logger.LogError($"➡️ 🕵️ INNER EXCEPTION (The Real Cause): {ex.InnerException.Message}");
                    _logger.LogError($"➡️ Inner Stack: {ex.InnerException.StackTrace}");
                }

                // Don't crash the app, just log it
                throw;
            }
        }
    }
}