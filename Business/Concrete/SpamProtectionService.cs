using System;
using System.Linq;
using System.Threading.Tasks;
using Business.Abstract;
using DataAccess.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class SpamProtectionService : ISpamProtectionService
    {
        private readonly AppDbContext _context;
        private readonly IHashService _hash;

        //günlük limit
        private const int DailyLimitPerSender = 10;

        public SpamProtectionService(AppDbContext context, IHashService hash)
        {
            _context = context;
            _hash = hash;
        }

        public async Task CheckOrThrowAsync(string senderEmail)
        {
            if (string.IsNullOrWhiteSpace(senderEmail))
                throw new ArgumentException("Gönderen e-posta zorunludur.", nameof(senderEmail));

            // Gönderenin hash'i
            var senderHash = _hash.ComputeHash(senderEmail.Trim().ToLowerInvariant());

            var todayUtc = DateTime.UtcNow.Date;
            var tomorrowUtc = todayUtc.AddDays(1);

            
            var count = await _context.TimeCapsuleMessages
                .CountAsync(x =>
                    x.SenderEmailHash != null &&
                    x.SenderEmailHash.SequenceEqual(senderHash) &&
                    x.CreatedAtUtc >= todayUtc &&
                    x.CreatedAtUtc < tomorrowUtc);

            if (count >= DailyLimitPerSender)
            {
                throw new InvalidOperationException(
                    "Bugün için zaman kapsülü gönderim limitinizi doldurdunuz. Lütfen yarın tekrar deneyin.");
            }
        }
    }
}
