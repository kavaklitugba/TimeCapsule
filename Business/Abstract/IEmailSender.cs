using System.Threading.Tasks;

namespace Business.Abstract
{
    public interface IEmailSender
    {
        Task<bool> SendAsync(string toEmail, string subject, string body);

        // Yeni: Mail içine gömülü (inline) görsel ile gönderim
        Task<bool> SendAsync(string toEmail, string subject, string body, string inlineImageFullPath, string contentId);

        public class EmailAttachment
        {
            public string FileName { get; set; }
            public string ContentType { get; set; } // image/jpeg
            public byte[] Content { get; set; }
        }
    }
}