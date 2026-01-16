namespace Entities
{
    public class TimeCapsuleMessage
    {
        public int Id { get; set; }

        // Kullanıcıya verdiğimiz yönetim ID'si
        public string LookupId { get; set; }

        // Şifreli alanlar
        public byte[] SenderEmailEncrypted { get; set; }
        public byte[] SenderEmailIv { get; set; }

        public byte[] RecipientEmailEncrypted { get; set; }
        public byte[] RecipientEmailIv { get; set; }

        public byte[] SubjectEncrypted { get; set; }
        public byte[] SubjectIv { get; set; }

        public byte[] EncryptedBody { get; set; }
        public byte[] BodyIv { get; set; }

        // Hash'ler
        public byte[] SenderEmailHash { get; set; }
        public byte[] RecipientEmailHash { get; set; }
        public byte[] SubjectHash { get; set; }

        // Zamanlar
        public DateTime CreatedAtUtc { get; set; }
        public DateTime SendAtUtc { get; set; }
        public DateTime? SentAtUtc { get; set; }

        // Durum
        public bool IsActive { get; set; }

        //Resim
        public string ImagePath { get; set; }
    }
}
