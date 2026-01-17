using Business.Abstract;
using Business.DTOs;
using DataAccess.Abstract;
using Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

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
        private readonly IWebHostEnvironment _env;

        private const int DueBatchSize = 100;

        public TimeCapsuleService(
            ITimeCapsuleMessageDal repo,
            ICryptoService crypto,
            IHashService hash,
            ISpamProtectionService spam,
            IEmailSender emailSender,
            ILogger<TimeCapsuleService> logger,
            IWebHostEnvironment env)
        {
            _repo = repo;
            _crypto = crypto;
            _hash = hash;
            _spam = spam;
            _emailSender = emailSender;
            _logger = logger;
            _env = env;
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

            var (senderCipher, senderIv) = _crypto.Encrypt(dto.SenderEmail);
            var (recipientCipher, recipientIv) = _crypto.Encrypt(dto.RecipientEmail);
            var (subjectCipher, subjectIv) = _crypto.Encrypt(dto.Subject ?? string.Empty);
            var (bodyCipher, bodyIv) = _crypto.Encrypt(dto.Body);

            var lookupId = LookupIdGenerator.Generate();

            var entity = new TimeCapsuleMessage
            {
                LookupId = lookupId,
                ImagePath = dto.ImagePath,

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

        public static class LookupIdGenerator
        {
            private static readonly char[] _chars =
                "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

            public static string Generate()
            {
                var random = RandomNumberGenerator.Create();
                var buffer = new byte[6];
                random.GetBytes(buffer);

                string part1 = $"{_chars[buffer[0] % _chars.Length]}{_chars[buffer[1] % _chars.Length]}{_chars[buffer[2] % _chars.Length]}{_chars[buffer[3] % _chars.Length]}";
                string part2 = $"{_chars[buffer[4] % _chars.Length]}{_chars[buffer[5] % _chars.Length]}{_chars[(buffer[0] + buffer[5]) % _chars.Length]}{_chars[(buffer[1] + buffer[4]) % _chars.Length]}";

                return $"TC-{part1}-{part2}";
            }
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
                Body = body,
                ImagePath = msg.ImagePath
            };
        }

        public async Task<bool> UpdateAsync(TimeCapsuleUpdateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.LookupId))
                return false;

            var msg = await _repo.GetByLookupIdAsync(dto.LookupId);
            if (msg == null || !msg.IsActive || msg.SentAtUtc.HasValue)
                return false;

            if (dto.SendAtLocal <= DateTime.Now)
                throw new ArgumentException("Gönderim tarihi gelecek bir zaman olmalıdır.");

            if (string.IsNullOrWhiteSpace(dto.SenderEmail) ||
                string.IsNullOrWhiteSpace(dto.RecipientEmail) ||
                string.IsNullOrWhiteSpace(dto.Body))
                throw new ArgumentException("Gönderen, alıcı ve mesaj boş olamaz.");

            await _spam.CheckOrThrowAsync(dto.SenderEmail);

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

            var normSender = dto.SenderEmail.Trim().ToLowerInvariant();
            var normRecipient = dto.RecipientEmail.Trim().ToLowerInvariant();

            msg.SenderEmailHash = _hash.ComputeHash(normSender);
            msg.RecipientEmailHash = _hash.ComputeHash(normRecipient);
            msg.SubjectHash = _hash.ComputeHash(dto.Subject ?? string.Empty);

            msg.SendAtUtc = dto.SendAtLocal.ToUniversalTime();

            // Controller dto.ImagePath'i 3 durumda set ediyor:
            // - Remove: null
            // - Yeni dosya: yeni path
            // - Değişmediyse: mevcut path
            msg.ImagePath = dto.ImagePath;

            await _repo.SaveChangesAsync();

            _logger.LogInformation("TimeCapsule updated. LookupId={LookupId}, ImagePath={ImagePath}",
                dto.LookupId, msg.ImagePath ?? "NULL");

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

                    // --- Mail HTML ---
                    // Görsel varsa CID ile mailin içine gömüyoruz (Gmail'de sorunsuz)
                    var cid = $"tcimg_{msg.LookupId}".Replace("-", "").ToLowerInvariant();
                    var hasImage = !string.IsNullOrWhiteSpace(msg.ImagePath) && File.Exists(GetFullImagePathFromWebRoot(msg.ImagePath));

                    var inlineImgHtml = string.Empty;
                    if (hasImage)
                    {
                        inlineImgHtml = $@"
                          <div style=""margin-top:16px;"">
                            <p style=""margin:0 0 8px; font-size:13px; color:#555;""><strong>Görsel:</strong></p>
                            <img src=""cid:{cid}""
                                 style=""display:block; max-width:420px; width:100%; height:auto; border-radius:12px; border:1px solid #e0e6ea;"" />
                          </div>";
                    }

                    var emailBody = $@"
<div style=""font-family:Arial, Helvetica, sans-serif; font-size:14px; line-height:1.5;"">
  <p style=""margin:0 0 10px;""><strong>Gönderen:</strong> {senderEmail}</p>
  <hr style=""border:none;border-top:1px solid #e6e6e6;margin:12px 0;"" />
  <div style=""white-space:pre-wrap;"">{bodyPlain}</div>
  {inlineImgHtml}
  <hr style=""border:none;border-top:1px solid #e6e6e6;margin:12px 0;"" />
  <p style=""font-size:12px;color:#777;margin:0;"">
    Bu e-posta <strong>Time Capsule</strong> sistemi üzerinden planlanmış bir geleceğe mektup teslimatıdır.
    Lütfen bu e-postaya yanıt vermeyin; bu adres yalnızca gönderim amaçlıdır ve yanıtlar takip edilmez.
  </p>
</div>";

                    bool success;

                    if (hasImage)
                    {
                        var fullPath = GetFullImagePathFromWebRoot(msg.ImagePath);
                        success = await _emailSender.SendAsync(recipientEmail, subject, emailBody, fullPath, cid);
                    }
                    else
                    {
                        success = await _emailSender.SendAsync(recipientEmail, subject, emailBody);
                    }

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

        private string GetFullImagePathFromWebRoot(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return null;

            var relative = imagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            return Path.Combine(_env.WebRootPath, relative);
        }

        // Bu interface'te var ama controller'da kullanmıyoruz; projende varsa bırak.
        public Task<TimeCapsuleManageViewModel> GetManageViewAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
