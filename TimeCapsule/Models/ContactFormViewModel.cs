using System.ComponentModel.DataAnnotations;

namespace TimeCapsule.Models
{
    public class ContactFormViewModel
    {
        [Required, StringLength(80)]
        public string Name { get; set; } = "";

        [Required, EmailAddress, StringLength(120)]
        public string Email { get; set; } = "";

        [Required, StringLength(120)]
        public string Subject { get; set; } = "";

        [Required, StringLength(2000)]
        public string Message { get; set; } = "";
    }
}
