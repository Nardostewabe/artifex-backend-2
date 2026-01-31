using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;

namespace Artifex_Backend_2.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IResend resend, ILogger<EmailService> logger)
        {
            _resend = resend;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var message = new EmailMessage();
              
                message.From = "nardostewabe@gmail.com";
                message.To.Add(toEmail);
                message.Subject = subject;
                message.HtmlBody = body;

                await _resend.EmailSendAsync(message);

                _logger.LogInformation($"✅ Resend Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                _logger.LogError($"❌ Resend Failed: {ex.Message}");
            }
        }
    }
}