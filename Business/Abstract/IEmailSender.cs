using System.Threading.Tasks;

namespace Business.Abstract
{
    public interface IEmailSender
    {
        Task<bool> SendAsync(string toEmail, string subject, string body);
    }
}