using System;
using System.Threading.Tasks;
using Business.Abstract;
using Business.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace TimeCapsule.Controllers
{
    public class TimeCapsuleController : Controller
    {
        private readonly ITimeCapsuleService _service;

        public TimeCapsuleController(ITimeCapsuleService service)
        {
            _service = service;
        }

        // ===================== CREATE =====================

        [HttpGet]
        public IActionResult Create()
        {
            // Varsayılan olarak şimdi +1 dakika, saniye 0
            var now = DateTime.Now.AddMinutes(1);
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
                // Eğer hazır süre seçildiyse, tarihi onunla override et
                if (!string.IsNullOrEmpty(presetMonths) &&
                    int.TryParse(presetMonths, out var m) && m > 0)
                {
                    var baseTime = DateTime.Now;
                    baseTime = new DateTime(baseTime.Year, baseTime.Month, baseTime.Day,
                                            baseTime.Hour, baseTime.Minute, 0);

                    model.SendAtLocal = baseTime.AddMonths(m);
                }

                var lookupId = await _service.CreateAsync(model);

                ViewBag.Success = "Mektubun zaman kapsülüne kaydedildi.";
                ViewBag.LookupId = lookupId;

                // Formu temizleyip tekrar başlangıç tarihiyle dön
                var now = DateTime.Now.AddMinutes(1);
                now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

                var emptyModel = new TimeCapsuleCreateDto
                {
                    SendAtLocal = now
                };

                ModelState.Clear();
                return View(emptyModel);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // ===================== MANAGE / LOOKUP =====================

        [HttpGet]
        public IActionResult Manage()
        {
            if (TempData["Message"] != null)
            {
                ViewBag.Message = TempData["Message"];
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Lookup(string lookupId)
        {
            if (string.IsNullOrWhiteSpace(lookupId))
            {
                ModelState.AddModelError(string.Empty, "Lütfen geçerli bir Kapsül ID girin.");
                return View("Manage");
            }

            var vm = await _service.GetManageInfoAsync(lookupId.Trim());
            if (vm == null)
            {
                ModelState.AddModelError(string.Empty, "Bu ID ile eşleşen kapsül bulunamadı.");
                return View("Manage");
            }

            return View("Manage", vm);
        }
        // ===================== UPDATE =====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(TimeCapsuleUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.LookupId))
            {
                TempData["Message"] = "Geçersiz Kapsül ID.";
                return RedirectToAction("Manage");
            }

            try
            {
                var success = await _service.UpdateAsync(dto);
                TempData["Message"] = success
                    ? "Kapsül bilgileri güncellendi."
                    : "Kapsül güncellenemedi (bulunamadı, gönderilmiş veya iptal edilmiş olabilir).";
            }
            catch (Exception ex)
            {
                TempData["Message"] = ex.Message;
            }

            // Aynı ID ile tekrar lookup yapalım ki güncel veriyi göstersin
            return RedirectToAction("Lookup", new { lookupId = dto.LookupId });
        }


        // ===================== CANCEL =====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string lookupId)
        {
            if (string.IsNullOrWhiteSpace(lookupId))
            {
                TempData["Message"] = "Geçersiz Kapsül ID.";
                return RedirectToAction("Manage");
            }

            var success = await _service.CancelAsync(lookupId.Trim());

            TempData["Message"] = success
                ? "Kapsül başarıyla iptal edildi."
                : "Kapsül iptal edilemedi (bulunamadı, gönderilmiş veya zaten iptal edilmiş olabilir).";

            return RedirectToAction("Manage");
        }

        // ===================== UPDATE SCHEDULE =====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSchedule(string lookupId, DateTime newSendAtLocal)
        {
            if (string.IsNullOrWhiteSpace(lookupId))
            {
                TempData["Message"] = "Geçersiz Kapsül ID.";
                return RedirectToAction("Manage");
            }

            try
            {
                var success = await _service.UpdateScheduleAsync(lookupId.Trim(), newSendAtLocal);

                TempData["Message"] = success
                    ? "Gönderim tarihi güncellendi."
                    : "Tarih güncellenemedi (kapsül aktif değil veya bulunamadı).";
            }
            catch (Exception ex)
            {
                TempData["Message"] = ex.Message;
            }

            return RedirectToAction("Manage");
        }
    }
}
