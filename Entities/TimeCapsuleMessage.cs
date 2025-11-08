using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class TimeCapsuleMessage
    {
        public int Id { get; set; }

        public string SenderEmail { get; set; }
        public string RecipientEmail { get; set; }
        public string Subject { get; set; }

        public byte[] EncryptedBody { get; set; }
        public byte[] Iv { get; set; }

        public DateTime SendAtUtc { get; set; }
        public DateTime? SentAtUtc { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public byte[] CancelTokenHash { get; set; }
        public byte[] PreviewTokenHash { get; set; }
    }
}
