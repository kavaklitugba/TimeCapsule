using Business.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class SpamProtectionService : ISpamProtectionService
    {
        private readonly AppDbContext _context;
        private const int DailyLimitPerSender = 10; // isteğe göre

        public SpamProtectionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CheckOrThrowAsync(string senderEmail)
        {
            var todayUtc = DateTime.UtcNow.Date;
            var tomorrowUtc = todayUtc.AddDays(1);

            var count = await _context.TimeCapsuleMessages
                .CountAsync(x =>
                    x.SenderEmail == senderEmail &&
                    x.CreatedAtUtc >= todayUtc &&
                    x.CreatedAtUtc < tomorrowUtc);

            if (count >= DailyLimitPerSender)
            {
                throw new InvalidOperationException("Bugün için limitinizi doldurdunuz. Lütfen yarın tekrar deneyin.");
            }
        }
    }

}
