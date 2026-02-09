using Business.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
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
                var settings = GetSmtpSettings();

                using var client = new SmtpClient(settings.Host, settings.Port)
                {
                    EnableSsl = settings.EnableSsl,
                    Credentials = new NetworkCredential(settings.Username, settings.Password)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(settings.Username, "Time Capsule"),
                    Subject = subject ?? string.Empty,
                    Body = body ?? string.Empty,
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

        // Inline görsel (CID) ile gönderim
        public async Task<bool> SendAsync(string toEmail, string subject, string body, string inlineImageFullPath, string contentId)
        {
            try
            {
                var settings = GetSmtpSettings();

                using var client = new SmtpClient(settings.Host, settings.Port)
                {
                    EnableSsl = settings.EnableSsl,
                    Credentials = new NetworkCredential(settings.Username, settings.Password)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(settings.Username, "Time Capsule"),
                    Subject = subject ?? string.Empty,
                    IsBodyHtml = true
                };

                message.ReplyToList.Clear();
                message.To.Add(toEmail);

                // HTML body + LinkedResource (CID)
                var htmlView = AlternateView.CreateAlternateViewFromString(body ?? string.Empty, null, MediaTypeNames.Text.Html);

                if (!string.IsNullOrWhiteSpace(inlineImageFullPath) && File.Exists(inlineImageFullPath))
                {
                    if (string.IsNullOrWhiteSpace(contentId))
                        contentId = "tcimg";

                    var ext = Path.GetExtension(inlineImageFullPath)?.ToLowerInvariant();
                    var mime = ext switch
                    {
                        ".jpg" => MediaTypeNames.Image.Jpeg,
                        ".jpeg" => MediaTypeNames.Image.Jpeg,
                        ".png" => "image/png",
                        ".gif" => MediaTypeNames.Image.Gif,
                        ".webp" => "image/webp",
                        _ => MediaTypeNames.Application.Octet
                    };

                    var fileName = "timecapsule-image" + (string.IsNullOrWhiteSpace(ext) ? ".jpg" : ext);

                    // LinkedResource için ContentType üzerinden isim veriyoruz (noname azalır)
                    var resource = new LinkedResource(inlineImageFullPath, new ContentType(mime))
                    {
                        ContentId = contentId,
                        TransferEncoding = TransferEncoding.Base64
                    };

                    resource.ContentType.Name = fileName;

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

        private (string Host, int Port, bool EnableSsl, string Username, string Password) GetSmtpSettings()
        {
            var smtpSection = _configuration.GetSection("Smtp");

            var host = smtpSection["Host"];
            var portStr = smtpSection["Port"];
            var sslStr = smtpSection["EnableSsl"];
            var username = smtpSection["Username"];
            var password = smtpSection["Password"];

            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("Smtp:Host bulunamadı.");
            if (!int.TryParse(portStr, out var port))
                throw new InvalidOperationException("Smtp:Port geçersiz.");
            if (!bool.TryParse(sslStr, out var enableSsl))
                enableSsl = true;

            if (string.IsNullOrWhiteSpace(username))
                throw new InvalidOperationException("Smtp:Username bulunamadı.");
            if (password == null)
                password = string.Empty;

            return (host, port, enableSsl, username, password);
        }
    }
}
