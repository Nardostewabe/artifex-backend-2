using MimeKit;
using MailKit.Net.Smtp;
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
                // 1. Get Config
                var host = _config["Smtp:Host"] ?? _config["Smtp__Host"];
                var portStr = _config["Smtp:Port"] ?? _config["Smtp__Port"];
                var username = _config["Smtp:Username"] ?? _config["Smtp__Username"];
                var password = _config["Smtp:Password"] ?? _config["Smtp__Password"];

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogError("❌ Email Config Missing in Render Environment.");
                    return;
                }

                var port = int.TryParse(portStr, out int p) ? p : 587;

                // 2. Create Message (MimeKit)
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("Artifex Support", username));
                emailMessage.To.Add(new MailboxAddress("", toEmail));
                emailMessage.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = body };
                emailMessage.Body = bodyBuilder.ToMessageBody();

                // 3. Send (MailKit SmtpClient)
                using var client = new SmtpClient();

                // Dangerous: Accept all SSL certs (Fixes handshake errors on Render)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                // Connect
                await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);

                // Authenticate
                await client.AuthenticateAsync(username, password);

                // Send
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"✅ Email successfully sent to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ MAILKIT FAILED: {ex.Message}");
            }
        }
    }
}