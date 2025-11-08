using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Business.DTOs;

namespace Business.Abstract
{
    public interface ITimeCapsuleService
    {
        Task<(string previewUrl, string cancelUrl)> CreateAsync(TimeCapsuleCreateDto dto, string baseUrl);
        Task<(string subject, string body)> GetPreviewAsync(string previewToken);
        Task<bool> CancelAsync(string cancelToken);
        Task ProcessDueMessagesAsync();
    }
}
