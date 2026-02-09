using DataAccess.Concrete;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using TimeCapsule.Models;

namespace TimeCapsule.Controllers
{
    public class PagesController : Controller
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;

        public PagesController(IConfiguration config, AppDbContext db)
        {
            _config = config;
            _db = db;
        }

        [HttpGet("/hakkimizda")]
        public IActionResult About() => View();

        [HttpGet("/gizlilik-politikasi")]
        public IActionResult Privacy() => View();

        [HttpGet("/iletisim")]
        public IActionResult Contact() => View(new ContactFormViewModel());

        [HttpPost("/iletisim")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Kime gidecek?
            var to = _config["Contact:ToEmail"] ?? "capsuleoffuture@gmail.com";

            // Senin appsettings anahtarların:
            var host = _config["Smtp:Host"];
            var port = _config.GetValue<int?>("Smtp:Port") ?? 587;
            var enableSsl = _config.GetValue<bool?>("Smtp:EnableSsl") ?? true;
            var user = _config["Smtp:Username"];
            var pass = _config["Smtp:Password"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                ModelState.AddModelError("", "E-posta ayarları eksik. Lütfen SMTP ayarlarınızı kontrol edin.");
                return View(model);
            }

            var body = new StringBuilder();
            body.AppendLine("TimeCapsule İletişim Formu");
            body.AppendLine("----------------------------------------");
            body.AppendLine($"Ad Soyad: {model.Name}");
            body.AppendLine($"E-posta: {model.Email}");
            body.AppendLine($"Konu: {model.Subject}");
            body.AppendLine("----------------------------------------");
            body.AppendLine(model.Message);

            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(user);
                message.To.Add(to);
                message.Subject = $"[İletişim] {model.Subject}";
                message.Body = body.ToString();
                message.IsBodyHtml = false;

                // kullanıcıya yanıtlamak kolay olsun
                message.ReplyToList.Add(new MailAddress(model.Email));

                using var client = new SmtpClient(host, port);
                client.EnableSsl = enableSsl;
                client.Credentials = new NetworkCredential(user, pass);

                await client.SendMailAsync(message);

                TempData["ContactSuccess"] = "Mesajınız başarıyla gönderildi. En kısa sürede dönüş yapacağız.";
                return RedirectToAction(nameof(Contact));
            }
            catch
            {
                ModelState.AddModelError("", "Mesaj gönderilemedi. Lütfen biraz sonra tekrar deneyin.");
                return View(model);
            }
        }

        // Menüde göstermeyeceğiz: /analytics
        [HttpGet("/analytics")]
        public async Task<IActionResult> Analytics([FromQuery] string? key)
        {
            // (İstersen key kontrolü kalsın, menüde yoksa da yeterli olur)
            var requiredKey = _config["Analytics:Key"];
            if (!string.IsNullOrWhiteSpace(requiredKey))
            {
                if (string.IsNullOrWhiteSpace(key) || key != requiredKey)
                    return NotFound();
            }

            // TR saat dilimi
            var trTz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, trTz);

            // local gün başlangıcını UTC’ye çevir
            DateTime StartOfDayUtc(DateTime local)
            {
                var startLocal = new DateTime(local.Year, local.Month, local.Day, 0, 0, 0, DateTimeKind.Unspecified);
                return TimeZoneInfo.ConvertTimeToUtc(startLocal, trTz);
            }

            var todayStartUtc = StartOfDayUtc(nowLocal);
            var weekStartUtc = todayStartUtc.AddDays(-7);
            var monthStartUtc = todayStartUtc.AddDays(-30);

            // DbSet adını senin DbContext’e göre düzenle:
            var q = _db.TimeCapsuleMessages.AsNoTracking();

            var vm = new DbDashboardVm
            {
                GeneratedAtLocal = nowLocal,

                // ekstra: görselli toplam
                TotalWithImage = await q.CountAsync(x => x.ImagePath != null && x.ImagePath != ""),

                Today = new PeriodVm
                {
                    LettersSaved = await q.CountAsync(x => x.CreatedAtUtc >= todayStartUtc),
                    LettersSent = await q.CountAsync(x => x.SentAtUtc != null && x.SentAtUtc >= todayStartUtc)
                },
                Week = new PeriodVm
                {
                    LettersSaved = await q.CountAsync(x => x.CreatedAtUtc >= weekStartUtc),
                    LettersSent = await q.CountAsync(x => x.SentAtUtc != null && x.SentAtUtc >= weekStartUtc)
                },
                Month = new PeriodVm
                {
                    LettersSaved = await q.CountAsync(x => x.CreatedAtUtc >= monthStartUtc),
                    LettersSent = await q.CountAsync(x => x.SentAtUtc != null && x.SentAtUtc >= monthStartUtc)
                },
                All = new PeriodVm
                {
                    LettersSaved = await q.CountAsync(),
                    LettersSent = await q.CountAsync(x => x.SentAtUtc != null)
                }
            };

            return View(vm);
        }
    }
}
