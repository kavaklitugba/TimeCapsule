using Business.Abstract;
using Business.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TimeCapsule.Models;

namespace TimeCapsule.Controllers
{
    public class TimeCapsuleController : Controller
    {
        private readonly ITimeCapsuleService _service;
        private readonly IEmailSender _emailService;
        private readonly IWebHostEnvironment _env;

        public TimeCapsuleController(ITimeCapsuleService service, IEmailSender emailService, IWebHostEnvironment env)
        {
            _service = service;
            _emailService = emailService;
            _env = env;
        }

        // ===================== CREATE =====================

        [HttpGet]
        public IActionResult Create()
        {
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
            {
                ViewBag.Error = "Bir hata oluştuğu için mailiniz gönderilemedi. Tekrar deneyiniz.";
                return View(model);
            }
            try
            {
                ViewBag.Error = null;
                // Eğer hazır süre seçildiyse, tarihi onunla override et
                if (!string.IsNullOrEmpty(presetMonths) &&
                    int.TryParse(presetMonths, out var m) && m > 0)
                {
                    var baseTime = DateTime.Now;
                    baseTime = new DateTime(baseTime.Year, baseTime.Month, baseTime.Day,
                                            baseTime.Hour, baseTime.Minute, 0);

                    model.SendAtLocal = baseTime.AddMonths(m);
                }

                // Resmi kaydet
                var imagePath = await SaveImageAsync(model.ImageFile);
                model.ImagePath = imagePath;

                var lookupId = await _service.CreateAsync(model);
                bool emailSent = false;

                var lookupUrl = Url.Action("Lookup", "TimeCapsule", new { lookupId = lookupId }, Request.Scheme);
                var toEmail = model.SenderEmail;
                var subject = "TimeCapsule – Kapsül ID’niz";


                var body = $@"
                <!DOCTYPE html>
                <html lang='tr'>
                <head>
                  <meta charset='UTF-8'>
                  <title>TimeCapsule</title>
                </head>
                <body style='margin:0; padding:0; background-color:#f4f7f9; font-family:Arial, Helvetica, sans-serif;'>
                  <table width='100%' cellpadding='0' cellspacing='0'>
                    <tr>
                      <td align='center' style='padding:30px 12px;'>
                        <table width='100%' cellpadding='0' cellspacing='0'
                               style='max-width:600px; background:#ffffff;
                                      border-radius:12px;
                                      border:1px solid #e0e6ea;'>
                          <!-- HEADER -->
                          <tr>
                            <td style='padding:20px 24px; background:#0f6f8c; border-radius:12px 12px 0 0;'>
                              <h2 style='margin:0; color:#ffffff; font-size:20px;'>
                                TimeCapsule
                              </h2>
                            </td>
                          </tr>

                          <!-- CONTENT -->
                          <tr>
                            <td style='padding:24px;'>
                              <p style='margin:0 0 12px; font-size:15px; color:#333333;'>
                                Merhaba,
                              </p>

                              <p style='margin:0 0 16px; font-size:14px; color:#555555;'>
                                Mektubunuz başarıyla <strong>zaman kapsülüne kaydedildi</strong>.
                                Aşağıda kapsülünüze ait bilgileri bulabilirsiniz.
                              </p>

                              <div style='background:#f1f8fb;
                                          border:1px dashed #0f6f8c;
                                          border-radius:10px;
                                          padding:14px;
                                          margin-bottom:16px;'>
                                <p style='margin:0 0 6px; font-size:13px; color:#666666;'>
                                  <strong>Kapsül ID</strong>
                                </p>

                                <p style='margin:0; font-family:Consolas, monospace;
                                          font-size:16px; color:#0f6f8c;'>
                                  {lookupId}
                                </p>
                              </div>

                              <p style='margin:0 0 8px; font-size:13px; color:#555555;'>
                                <strong>Planlanan Gönderim Tarihi:</strong>
                                {model.SendAtLocal:dd.MM.yyyy HH:mm}
                              </p>

                              <p style='margin:12px 0 20px; font-size:13px; color:#555555;'>
                                Kapsülünüzü görüntülemek, düzenlemek veya iptal etmek için aşağıdaki bağlantıyı kullanabilirsiniz:
                              </p>

                              <a href='{lookupUrl}'
                                 style='display:inline-block;
                                        background:#0f6f8c;
                                        color:#ffffff;
                                        text-decoration:none;
                                        padding:10px 18px;
                                        border-radius:8px;
                                        font-size:14px;'>
                                Kapsülü Yönet
                              </a>

                              <p style='margin:20px 0 0; font-size:12px; color:#777777;'>
                                Bu e-postayı saklamanızı öneririz. Kapsül ID’niz ile dilediğiniz zaman sorgulama yapabilirsiniz.
                              </p>
                            </td>
                          </tr>

                          <!-- FOOTER -->
                          <tr>
                            <td style='padding:14px 24px;
                                       background:#f8fafb;
                                       border-radius:0 0 12px 12px;
                                       font-size:11px;
                                       color:#999999;'>
                              © {DateTime.Now.Year} TimeCapsule
                            </td>
                          </tr>

                        </table>
                      </td>
                    </tr>
                  </table>
                </body>
                </html>
                ";

                try
                {
                    emailSent = await _emailService.SendAsync(toEmail, subject, body);
                }
                catch
                {
                    emailSent = false;
                }

                ViewBag.Success = emailSent
                ? "Mektubun zaman kapsülüne kaydedildi. Kapsül ID bilgileriniz e-posta adresinize gönderildi."
                : "Mektubun zaman kapsülüne kaydedildi. E-posta gönderimi sırasında sorun oluştu; lütfen Kapsül ID'nizi not alın.";

                ViewBag.LookupId = lookupId;

                // Formu temizle
                var now2 = DateTime.Now.AddMinutes(1);
                now2 = new DateTime(now2.Year, now2.Month, now2.Day, now2.Hour, now2.Minute, 0);

                var emptyModel = new TimeCapsuleCreateDto
                {
                    SendAtLocal = now2
                };

                ModelState.Clear();
                return View(emptyModel);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }

        // ===================== MANAGE / LOOKUP =====================

        [HttpGet]
        public IActionResult Manage()
        {
            if (TempData["Message"] != null)
                ViewBag.Message = TempData["Message"];

            // boş manage ekranı
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
                // MEVCUT imagePath'i çek (boş update olmasın diye)
                var currentVm = await _service.GetManageInfoAsync(dto.LookupId.Trim());
                var currentImagePath = currentVm?.ImagePath;

                // 1) remove işaretliyse sil
                if (dto.RemoveImage)
                {
                    DeleteImageIfExists(currentImagePath);
                    dto.ImagePath = "";
                }
                // 2) yeni resim geldiyse eskiyi sil + kaydet
                else if (dto.ImageFile != null && dto.ImageFile.Length > 0)
                {
                    DeleteImageIfExists(currentImagePath);
                    dto.ImagePath = await SaveImageAsync(dto.ImageFile);
                }
                // 3) hiçbir şey yoksa KORU (kritik)
                else
                {
                    dto.ImagePath = currentImagePath;
                }

                var success = await _service.UpdateAsync(dto);
                TempData["Message"] = success
                    ? "Kapsül bilgileri güncellendi."
                    : "Kapsül güncellenemedi (bulunamadı, gönderilmiş veya iptal edilmiş olabilir).";
            }
            catch (Exception ex)
            {
                TempData["Message"] = ex.Message;
            }

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

            return RedirectToAction("Lookup", new { lookupId = lookupId });
        }

        // ===================== ERROR =====================

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ===================== IMAGE HELPERS =====================

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext))
                throw new Exception("Sadece jpg, jpeg, png, webp yüklenebilir.");

            var uploads = Path.Combine(_env.WebRootPath, "uploads", "timecapsule");
            Directory.CreateDirectory(uploads);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploads, fileName);

            using (var fs = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(fs);

            return "/uploads/timecapsule/" + fileName;
        }

        private void DeleteImageIfExists(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return;

            var relative = imagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var full = Path.Combine(_env.WebRootPath, relative);

            if (System.IO.File.Exists(full))
                System.IO.File.Delete(full);
        }

        private string BuildImageDataUriFromWebRoot(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return null;

            var relative = imagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var fullPath = Path.Combine(_env.WebRootPath, relative);

            if (!System.IO.File.Exists(fullPath))
                return null;

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var mime = ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var bytes = System.IO.File.ReadAllBytes(fullPath);
            var base64 = Convert.ToBase64String(bytes);

            return $"data:{mime};base64,{base64}";
        }
    }
}
