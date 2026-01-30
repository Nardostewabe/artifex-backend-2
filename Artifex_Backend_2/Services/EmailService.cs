using System.Net;
using System.Net.Mail;

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

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // 1. Read Settings
                var host = _config["Smtp:Host"];
                var port = int.Parse(_config["Smtp:Port"]);
                var email = _config["Smtp:Username"];
                var password = _config["Smtp:Password"];

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
                // This prints the error to your console if it fails
                Console.WriteLine($"[EmailService] Error: {ex.Message}");
            }
        }
    }
}