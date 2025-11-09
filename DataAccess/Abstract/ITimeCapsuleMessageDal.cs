using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Entities;

namespace DataAccess.Abstract
{
    public interface ITimeCapsuleMessageDal
    {
        Task AddAsync(TimeCapsuleMessage entity);
        Task SaveChangesAsync();

        Task<List<TimeCapsuleMessage>> GetDueMessagesAsync(DateTime utcNow, int take);
        Task<TimeCapsuleMessage> GetByLookupIdAsync(string lookupId);
    }
}
