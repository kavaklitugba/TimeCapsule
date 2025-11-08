using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Business.Abstract;
using Business.DTOs;
using DataAccess.Abstract;
using Entities;

namespace Business.Concrete
{
    public class TimeCapsuleService : ITimeCapsuleService
    {
        private readonly ITimeCapsuleMessageDal _repo;
        private readonly ICryptoService _crypto;
        private readonly IEmailSender _emailSender;
        private readonly ISpamProtectionService _spam;
        private readonly ILogger<TimeCapsuleService> _logger;

        public TimeCapsuleService(
            ITimeCapsuleMessageDal repo,
            ICryptoService crypto,
            IEmailSender emailSender,
            ISpamProtectionService spam,
            ILogger<TimeCapsuleService> logger)
        {
            _repo = repo;
            _crypto = crypto;
            _emailSender = emailSender;
            _spam = spam;
            _logger = logger;
        }

        public async Task<(string previewUrl, string cancelUrl)> CreateAsync(TimeCapsuleCreateDto dto, string baseUrl)
        {
            dto.SenderEmail = dto.SenderEmail.Trim();
            dto.RecipientEmail = dto.RecipientEmail.Trim();

            await _spam.CheckOrThrowAsync(dto.SenderEmail);

            if (!dto.RecipientEmail.Contains("@"))
                throw new InvalidOperationException("Geçerli bir alıcı e-posta giriniz.");

            var sendAtUtc = DateTime.SpecifyKind(dto.SendAtLocal, DateTimeKind.Local).ToUniversalTime();

            var (cipher, iv) = _crypto.Encrypt(dto.Body);

            var previewToken = Guid.NewGuid().ToString("N");
            var cancelToken = Guid.NewGuid().ToString("N");

            var entity = new TimeCapsuleMessage
            {
                SenderEmail = dto.SenderEmail,
                RecipientEmail = dto.RecipientEmail,
                Subject = dto.Subject,
                EncryptedBody = cipher,
                Iv = iv,
                SendAtUtc = sendAtUtc,
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true,
                PreviewTokenHash = _crypto.HashToken(previewToken),
                CancelTokenHash = _crypto.HashToken(cancelToken)
            };

            await _repo.AddAsync(entity);
            await _repo.SaveChangesAsync();

            var previewUrl = $"{baseUrl}/TimeCapsule/Preview?token={previewToken}";
            var cancelUrl = $"{baseUrl}/TimeCapsule/Cancel?token={cancelToken}";

            return (previewUrl, cancelUrl);
        }

        public async Task<(string subject, string body)> GetPreviewAsync(string previewToken)
        {
            var hash = _crypto.HashToken(previewToken);

            var msg = await _repo.GetByPreviewTokenHashAsync(hash);
            if (msg == null || !msg.IsActive)
                throw new InvalidOperationException("Mesaj bulunamadı veya iptal edilmiş.");

            var body = _crypto.Decrypt(msg.EncryptedBody, msg.Iv);
            return (msg.Subject, body);
        }

        public async Task<bool> CancelAsync(string cancelToken)
        {
            var hash = _crypto.HashToken(cancelToken);

            var msg = await _repo.GetByCancelTokenHashAsync(hash);
            if (msg == null || !msg.IsActive)
                return false;

            msg.IsActive = false;
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task ProcessDueMessagesAsync()
        {
            var nowUtc = DateTime.UtcNow;
            var dueMessages = await _repo.GetDueMessagesAsync(nowUtc, 100);

            if (dueMessages == null || dueMessages.Count == 0)
                return;

            foreach (var msg in dueMessages)
            {
                try
                {
                    var body = _crypto.Decrypt(msg.EncryptedBody, msg.Iv);

                    var success = await _emailSender.SendAsync(
                        msg.RecipientEmail,
                        msg.Subject ?? "Time Capsule Message",
                        body
                    );

                    if (success)
                    {
                        msg.SentAtUtc = DateTime.UtcNow;
                        msg.IsActive = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "TimeCapsule Id={Id} gönderiminde hata", msg.Id);
                }
            }

            await _repo.SaveChangesAsync();
        }
    }
}
