namespace Business.DTOs
{
    public class TimeCapsuleManageViewModel
    {
        public string LookupId { get; set; }

        public DateTime CreatedAtLocal { get; set; }
        public DateTime SendAtLocal { get; set; }
        public DateTime? SentAtLocal { get; set; }

        public bool IsActive { get; set; }

        // Düzenleme için düz metin alanlar
        public string SenderEmail { get; set; }
        public string RecipientEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }

        public string Status
        {
            get
            {
                if (SentAtLocal.HasValue) return "Gönderildi";
                if (!IsActive) return "İptal Edildi";
                return "Beklemede";
            }
        }
    }
}
