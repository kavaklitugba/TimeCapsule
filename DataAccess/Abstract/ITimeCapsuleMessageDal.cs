using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities;

namespace DataAccess.Abstract
{
    public interface ITimeCapsuleMessageDal
    {
        Task AddAsync(TimeCapsuleMessage entity);
        Task<TimeCapsuleMessage> GetByPreviewTokenHashAsync(byte[] previewTokenHash);
        Task<TimeCapsuleMessage> GetByCancelTokenHashAsync(byte[] cancelTokenHash);
        Task<List<TimeCapsuleMessage>> GetDueMessagesAsync(DateTime utcNow, int take);
        Task SaveChangesAsync();
    }

}
