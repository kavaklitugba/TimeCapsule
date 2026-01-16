using System;
using Microsoft.AspNetCore.Http;

namespace Business.DTOs
{
    public class TimeCapsuleManageViewModel
    {
        public string LookupId { get; set; }

        public DateTime CreatedAtLocal { get; set; }
        public DateTime SendAtLocal { get; set; }
        public DateTime? SentAtLocal { get; set; }

        public bool IsActive { get; set; }

        public string SenderEmail { get; set; }
        public string RecipientEmail { get; set; }

        public string Subject { get; set; }
        public string Body { get; set; }

        // Mevcut resim (DB'den gelir)
        public string ImagePath { get; set; }

        // Manage ekranında yeni resim seçmek için (POST)
        public IFormFile ImageFile { get; set; }

        // "Resmi kaldır" checkbox'ı (POST)
        public bool RemoveImage { get; set; }
    }
}
