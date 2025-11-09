using System;

namespace Business.DTOs
{
    public class TimeCapsuleUpdateDto
    {
        public string LookupId { get; set; }
        public string SenderEmail { get; set; }
        public string RecipientEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime SendAtLocal { get; set; }
    }
}
