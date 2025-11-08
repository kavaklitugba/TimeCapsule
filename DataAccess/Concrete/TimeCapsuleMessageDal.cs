using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities;
using DataAccess.Abstract;

namespace DataAccess.Concrete
{
    public class TimeCapsuleMessageDal : ITimeCapsuleMessageDal
    {
        private readonly AppDbContext _context;

        public TimeCapsuleMessageDal(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TimeCapsuleMessage entity)
        {
            await _context.TimeCapsuleMessages.AddAsync(entity);
        }

        public Task<TimeCapsuleMessage> GetByPreviewTokenHashAsync(byte[] previewTokenHash)
        {
            return _context.TimeCapsuleMessages
                .FirstOrDefaultAsync(x => x.PreviewTokenHash == previewTokenHash && x.IsActive);
        }

        public Task<TimeCapsuleMessage> GetByCancelTokenHashAsync(byte[] cancelTokenHash)
        {
            return _context.TimeCapsuleMessages
                .FirstOrDefaultAsync(x => x.CancelTokenHash == cancelTokenHash && x.IsActive);
        }

        public Task<List<TimeCapsuleMessage>> GetDueMessagesAsync(DateTime utcNow, int take)
        {
            return _context.TimeCapsuleMessages
                .Where(x => x.IsActive && x.SendAtUtc <= utcNow && x.SentAtUtc == null)
                .OrderBy(x => x.SendAtUtc)
                .Take(take)
                .ToListAsync();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }

}
