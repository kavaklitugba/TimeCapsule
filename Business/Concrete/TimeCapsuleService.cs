using System;
using System.Threading.Tasks;
using Business.Abstract;
using Business.DTOs;
using DataAccess.Abstract;
using Entities;
using Microsoft.Extensions.Logging;

namespace Business.Concrete
{
    public class TimeCapsuleService : ITimeCapsuleService
    {
        private readonly ITimeCapsuleMessageDal _repo;
        private readonly ICryptoService _crypto;
        private readonly IHashService _hash;
        private readonly ISpamProtectionService _spam;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<TimeCapsuleService> _logger;

        private const int DueBatchSize = 100;

        public TimeCapsuleService(
            ITimeCapsuleMessageDal repo,
            ICryptoService crypto,
            IHashService hash,
            ISpamProtectionService spam,
            IEmailSender emailSender,
            ILogger<TimeCapsuleService> logger)
        {
            _repo = repo;
            _crypto = crypto;
            _hash = hash;
            _spam = spam;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<string> CreateAsync(TimeCapsuleCreateDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.SenderEmail))
                throw new ArgumentException("Gönderen e-posta zorunludur.");
            if (string.IsNullOrWhiteSpace(dto.RecipientEmail))
                throw new ArgumentException("Alıcı e-posta zorunludur.");
            if (string.IsNullOrWhiteSpace(dto.Body))
                throw new ArgumentException("Mesaj içeriği zorunludur.");
            if (dto.SendAtLocal <= DateTime.Now)
                throw new ArgumentException("Gönderim tarihi gelecek bir zaman olmalıdır.");

            await _spam.CheckOrThrowAsync(dto.SenderEmail);

            var sendAtUtc = dto.SendAtLocal.ToUniversalTime();

            // Encrypt
            var (senderCipher, senderIv) = _crypto.Encrypt(dto.SenderEmail);
            var (recipientCipher, recipientIv) = _crypto.Encrypt(dto.RecipientEmail);
            var (subjectCipher, subjectIv) = _crypto.Encrypt(dto.Subject ?? string.Empty);
            var (bodyCipher, bodyIv) = _crypto.Encrypt(dto.Body);

            var lookupId = Guid.NewGuid().ToString("N");

            var entity = new TimeCapsuleMessage
            {
                LookupId = lookupId,

                SenderEmailEncrypted = senderCipher,
                SenderEmailIv = senderIv,

                RecipientEmailEncrypted = recipientCipher,
                RecipientEmailIv = recipientIv,

                SubjectEncrypted = subjectCipher,
                SubjectIv = subjectIv,

                EncryptedBody = bodyCipher,
                BodyIv = bodyIv,

                SenderEmailHash = _hash.ComputeHash(dto.SenderEmail),
                RecipientEmailHash = _hash.ComputeHash(dto.RecipientEmail),
                SubjectHash = _hash.ComputeHash(dto.Subject ?? string.Empty),

                CreatedAtUtc = DateTime.UtcNow,
                SendAtUtc = sendAtUtc,
                SentAtUtc = null,
                IsActive = true
            };

            await _repo.AddAsync(entity);
            await _repo.SaveChangesAsync();

            _logger.LogInformation("TimeCapsule created. LookupId={LookupId}", lookupId);

            return lookupId;
        }

        public async Task<TimeCapsuleManageViewModel?> GetManageInfoAsync(string lookupId)
        {
            if (string.IsNullOrWhiteSpace(lookupId))
                return null;

            var msg = await _repo.GetByLookupIdAsync(lookupId);
            if (msg == null)
                return null;

            var sender = _crypto.Decrypt(msg.SenderEmailEncrypted, msg.SenderEmailIv);
            var recipient = _crypto.Decrypt(msg.RecipientEmailEncrypted, msg.RecipientEmailIv);
            var subject = _crypto.Decrypt(msg.SubjectEncrypted, msg.SubjectIv);
            var body = _crypto.Decrypt(msg.EncryptedBody, msg.BodyIv);

            return new TimeCapsuleManageViewModel
            {
                LookupId = msg.LookupId,
                CreatedAtLocal = msg.CreatedAtUtc.ToLocalTime(),
                SendAtLocal = msg.SendAtUtc.ToLocalTime(),
                SentAtLocal = msg.SentAtUtc?.ToLocalTime(),
                IsActive = msg.IsActive,
                SenderEmail = sender,
                RecipientEmail = recipient,
                Subject = subject,
                Body = body
            };
        }
        public async Task<bool> UpdateAsync(TimeCapsuleUpdateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.LookupId))
                return false;

            var msg = await _repo.GetByLookupIdAsync(dto.LookupId);
            if (msg == null || !msg.IsActive || msg.SentAtUtc.HasValue)
                return false; // gönderilmiş/iptal kapsül düzenlenmez

            if (dto.SendAtLocal <= DateTime.Now)
                throw new ArgumentException("Gönderim tarihi gelecek bir zaman olmalıdır.");

            if (string.IsNullOrWhiteSpace(dto.SenderEmail) ||
                string.IsNullOrWhiteSpace(dto.RecipientEmail) ||
                string.IsNullOrWhiteSpace(dto.Body))
                throw new ArgumentException("Gönderen, alıcı ve mesaj boş olamaz.");

            // Güncel gönderen için spam limiti
            await _spam.CheckOrThrowAsync(dto.SenderEmail);

            // Şifreleme
            var (senderCipher, senderIv) = _crypto.Encrypt(dto.SenderEmail.Trim());
            var (recipientCipher, recipientIv) = _crypto.Encrypt(dto.RecipientEmail.Trim());
            var (subjectCipher, subjectIv) = _crypto.Encrypt(dto.Subject ?? string.Empty);
            var (bodyCipher, bodyIv) = _crypto.Encrypt(dto.Body);

            msg.SenderEmailEncrypted = senderCipher;
            msg.SenderEmailIv = senderIv;
            msg.RecipientEmailEncrypted = recipientCipher;
            msg.RecipientEmailIv = recipientIv;
            msg.SubjectEncrypted = subjectCipher;
            msg.SubjectIv = subjectIv;
            msg.EncryptedBody = bodyCipher;
            msg.BodyIv = bodyIv;

            // Hashler
            var normSender = dto.SenderEmail.Trim().ToLowerInvariant();
            var normRecipient = dto.RecipientEmail.Trim().ToLowerInvariant();

            msg.SenderEmailHash = _hash.ComputeHash(normSender);
            msg.RecipientEmailHash = _hash.ComputeHash(normRecipient);
            msg.SubjectHash = _hash.ComputeHash(dto.Subject ?? string.Empty);

            // Tarih
            msg.SendAtUtc = dto.SendAtLocal.ToUniversalTime();

            await _repo.SaveChangesAsync();

            _logger.LogInformation("TimeCapsule updated. LookupId={LookupId}", dto.LookupId);
            return true;
        }


        public async Task<bool> CancelAsync(string lookupId)
        {
            if (string.IsNullOrWhiteSpace(lookupId))
                return false;

            var msg = await _repo.GetByLookupIdAsync(lookupId);
            if (msg == null)
                return false;

            if (!msg.IsActive || msg.SentAtUtc.HasValue)
                return false;

            msg.IsActive = false;
            await _repo.SaveChangesAsync();

            _logger.LogInformation("TimeCapsule cancelled. LookupId={LookupId}", lookupId);
            return true;
        }

        public async Task<bool> UpdateScheduleAsync(string lookupId, DateTime newSendAtLocal)
        {
            if (string.IsNullOrWhiteSpace(lookupId))
                return false;

            if (newSendAtLocal <= DateTime.Now)
                throw new ArgumentException("Yeni gönderim tarihi gelecek bir zaman olmalıdır.");

            var msg = await _repo.GetByLookupIdAsync(lookupId);
            if (msg == null)
                return false;

            if (!msg.IsActive || msg.SentAtUtc.HasValue)
                return false;

            msg.SendAtUtc = newSendAtLocal.ToUniversalTime();
            await _repo.SaveChangesAsync();

            _logger.LogInformation("TimeCapsule schedule updated. LookupId={LookupId}, NewDate={NewDate}",
                lookupId, msg.SendAtUtc);

            return true;
        }

        public async Task ProcessDueMessagesAsync()
        {
            var nowUtc = DateTime.UtcNow;

            var dueMessages = await _repo.GetDueMessagesAsync(nowUtc, DueBatchSize);
            if (dueMessages == null || dueMessages.Count == 0)
                return;

            _logger.LogInformation("Processing {Count} due time capsules...", dueMessages.Count);

            foreach (var msg in dueMessages)
            {
                try
                {
                    if (!msg.IsActive || msg.SentAtUtc.HasValue)
                        continue;

                    var senderEmail = _crypto.Decrypt(msg.SenderEmailEncrypted, msg.SenderEmailIv);
                    var recipientEmail = _crypto.Decrypt(msg.RecipientEmailEncrypted, msg.RecipientEmailIv);
                    var subject = _crypto.Decrypt(msg.SubjectEncrypted, msg.SubjectIv);
                    if (string.IsNullOrWhiteSpace(subject))
                        subject = "Time Capsule Mesajın";

                    var bodyPlain = _crypto.Decrypt(msg.EncryptedBody, msg.BodyIv);

                    var emailBody = $@"
                        <p><strong>Gönderen:</strong> {senderEmail}</p>
                        <hr />
                        {bodyPlain}
                        <hr />
                        <p style=""font-size:12px;color:#777;"">
                        Bu e-posta <strong>Time Capsule</strong> sistemi üzerinden planlanmış bir geleceğe mektup teslimatıdır.
                        Lütfen bu e-postaya yanıt vermeyin; bu adres yalnızca gönderim amaçlıdır ve yanıtlar takip edilmez.
                        </p>";

                    var success = await _emailSender.SendAsync(
                        recipientEmail,
                        subject,
                        emailBody
                    );

                    if (success)
                    {
                        msg.SentAtUtc = DateTime.UtcNow;
                        msg.IsActive = false;
                        _logger.LogInformation("TimeCapsule sent. LookupId={LookupId}", msg.LookupId);
                    }
                    else
                    {
                        _logger.LogWarning("TimeCapsule send failed (SendAsync=false). LookupId={LookupId}", msg.LookupId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while sending TimeCapsule. LookupId={LookupId}", msg.LookupId);
                }
            }

            await _repo.SaveChangesAsync();
        }
    }
}
