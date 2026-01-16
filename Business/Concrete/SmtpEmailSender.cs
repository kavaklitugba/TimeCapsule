using Business.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Business.Concrete
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpSection = _configuration.GetSection("Smtp");
                var host = smtpSection["Host"];
                var port = int.Parse(smtpSection["Port"]);
                var enableSsl = bool.Parse(smtpSection["EnableSsl"]);
                var username = smtpSection["Username"];
                var password = smtpSection["Password"];

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(username, password)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(username, "Time Capsule"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.ReplyToList.Clear();
                message.To.Add(toEmail);

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP mail gönderimi sırasında hata oluştu. To={To}", toEmail);
                return false;
            }
        }

        // ✅ Inline görsel (CID) ile gönderim
        public async Task<bool> SendAsync(string toEmail, string subject, string body, string inlineImageFullPath, string contentId)
        {
            try
            {
                var smtpSection = _configuration.GetSection("Smtp");
                var host = smtpSection["Host"];
                var port = int.Parse(smtpSection["Port"]);
                var enableSsl = bool.Parse(smtpSection["EnableSsl"]);
                var username = smtpSection["Username"];
                var password = smtpSection["Password"];

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(username, password)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(username, "Time Capsule"),
                    Subject = subject,
                    IsBodyHtml = true
                };

                message.ReplyToList.Clear();
                message.To.Add(toEmail);

                // HTML body + LinkedResource (CID)
                var htmlView = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);

                if (!string.IsNullOrWhiteSpace(inlineImageFullPath) && System.IO.File.Exists(inlineImageFullPath))
                {
                    var resource = new LinkedResource(inlineImageFullPath)
                    {
                        ContentId = contentId,
                        TransferEncoding = TransferEncoding.Base64
                    };

                    htmlView.LinkedResources.Add(resource);
                }
                else
                {
                    _logger.LogWarning("Inline image bulunamadı. Path={Path}", inlineImageFullPath);
                }

                message.AlternateViews.Add(htmlView);

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP inline mail gönderimi sırasında hata oluştu. To={To}", toEmail);
                return false;
            }
        }
    }
}
