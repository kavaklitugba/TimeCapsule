using System;
using System.Threading.Tasks;
using Business.Abstract;
using Business.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TimeCapsule.Controllers
{
    public class TimeCapsuleController : Controller
    {
        private readonly ITimeCapsuleService _service;
        private readonly ILogger<TimeCapsuleController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TimeCapsuleController(
            ITimeCapsuleService service,
            ILogger<TimeCapsuleController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _service = service;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public IActionResult Create()
        {
            var now = DateTime.Now.AddMinutes(1);
            // saniye ve saliseyi sıfırla
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

            var model = new TimeCapsuleCreateDto
            {
                SendAtLocal = now
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TimeCapsuleCreateDto model, string? presetMonths)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Eğer 6 ay / 12 ay preset seçilmişse:
                if (!string.IsNullOrEmpty(presetMonths) && int.TryParse(presetMonths, out var m) && m > 0)
                {
                    model.SendAtLocal = DateTime.Now.AddMonths(m);
                }

                // değilse, model.SendAtLocal zaten takvimden geliyor olmalı
                // (dto'da [Required] ise kullanıcı tarih seçmek zorunda)

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var (previewUrl, cancelUrl) = await _service.CreateAsync(model, baseUrl);

                ViewBag.PreviewUrl = previewUrl;
                ViewBag.CancelUrl = cancelUrl;
                ViewBag.Success = "Mektubun zaman kapsülüne kaydedildi.";

                return View(new TimeCapsuleCreateDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TimeCapsule oluşturulurken hata");
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Preview(string token)
        {
            try
            {
                var (subject, body) = await _service.GetPreviewAsync(token);
                ViewBag.Subject = subject;
                ViewBag.Body = body;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Preview token geçersiz");
                return View("Error", "Önizleme linki geçersiz veya mesaj iptal edilmiş.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Cancel(string token)
        {
            var success = await _service.CancelAsync(token);
            return View(success);
        }
    }

}
