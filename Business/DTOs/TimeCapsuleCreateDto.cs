using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs
{
    public class TimeCapsuleCreateDto
    {
        public string SenderEmail { get; set; }
        public string RecipientEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }

        public DateTime SendAtLocal { get; set; } // Formdan gelen (kullanıcı saati)
    }

}
