using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security; // Needed for SecureSocketOptions
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

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
                var host = "smtp.gmail.com";
                var port = 465; // ✅ SWITCH TO PORT 465 (SSL)

                var username = _config["Smtp:Username"] ?? _config["Smtp__Username"];
                var password = _config["Smtp:Password"] ?? _config["Smtp__Password"];

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogError("❌ Email Credentials Missing.");
                    return;
                }

                // Create Message
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("Artifex Support", username));
                emailMessage.To.Add(new MailboxAddress("", toEmail));
                emailMessage.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = body };
                emailMessage.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                // Dangerous: Accept all SSL certs (Needed for some cloud containers)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                // ✅ CRITICAL CHANGE: Use SslOnConnect for Port 465
                await client.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);

                // Authenticate
                await client.AuthenticateAsync(username, password);

                // Send
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"✅ Email successfully sent to {toEmail}");
            }
            catch (Exception ex)
            {
                // This logs the error but keeps your site alive
                _logger.LogError($"❌ MAILKIT FAILED (Port 465): {ex.Message}");
            }
        }
    }
}