using System.ComponentModel.DataAnnotations;

namespace SatışProject.Models
{
    public class SendMessageViewModel
    {
        [Required(ErrorMessage = "Alıcı seçimi zorunludur.")]
        [Display(Name = "Alıcı")]
        public string RecipientUserId { get; set; } = null!;

        [Required(ErrorMessage = "Konu boş bırakılamaz.")]
        [StringLength(500, ErrorMessage = "Konu en fazla 500 karakter olabilir.")]
        [Display(Name = "Konu")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mesaj içeriği boş bırakılamaz.")]
        [StringLength(4000, ErrorMessage = "Mesaj içeriği en fazla 4000 karakter olabilir.")]
        [Display(Name = "Mesaj")]
        public string Content { get; set; } = string.Empty;
    }
}
