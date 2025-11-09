using Business.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
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

                // Yanıt adresi temizlemek için
                message.ReplyToList.Clear(); // reply direkt göndericiye gider ama uyarı koyuyoruz

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

    }
}
