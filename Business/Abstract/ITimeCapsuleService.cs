using System;
using System.Threading.Tasks;
using Business.DTOs;

namespace Business.Abstract
{
    public interface ITimeCapsuleService
    {
        Task<string> CreateAsync(TimeCapsuleCreateDto dto);
        Task ProcessDueMessagesAsync();

        Task<TimeCapsuleManageViewModel?> GetManageInfoAsync(string lookupId);
        Task<bool> CancelAsync(string lookupId);
        Task<bool> UpdateScheduleAsync(string lookupId, DateTime newSendAtLocal);
        Task<bool> UpdateAsync(TimeCapsuleUpdateDto dto);

        Task<TimeCapsuleManageViewModel> GetManageViewAsync(int id);
    }
}
