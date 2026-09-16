using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace GreenMart.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            IOptions<EmailSettings> settings,
            ILogger<SmtpEmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> SendDeliveryApprovalAsync(
            string recipientEmail,
            string recipientName)
        {
            if (string.IsNullOrWhiteSpace(_settings.SmtpHost) ||
                string.IsNullOrWhiteSpace(_settings.FromAddress))
            {
                _logger.LogWarning("Approval email was skipped because SMTP is not configured.");
                return false;
            }

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.FromAddress, _settings.FromName),
                    Subject = "Your GreenMart delivery account is approved",
                    Body = $"Hello {recipientName},\n\nYour delivery partner request has been approved. You can now log in to GreenMart and open your Delivery Dashboard.\n\nGreenMart",
                    IsBodyHtml = false
                };

                message.To.Add(recipientEmail);

                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = string.IsNullOrWhiteSpace(_settings.Username)
                        ? CredentialCache.DefaultNetworkCredentials
                        : new NetworkCredential(_settings.Username, _settings.Password)
                };

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not send delivery approval email.");
                return false;
            }
        }
    }
}
