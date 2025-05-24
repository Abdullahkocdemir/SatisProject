using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SatışProject.Entities
{
    public class Message
    {
        public int MessageId { get; set; }

        [Required]
        public string SenderUserId { get; set; } = null!; // Gönderen kullanıcının Id'si

        [ForeignKey("SenderUserId")]
        public virtual AppUser Sender { get; set; } = null!; // Gönderen kullanıcı

        [Required]
        [StringLength(500)]
        public string Subject { get; set; } = string.Empty; // Mesaj konusu

        [Required]
        [StringLength(4000)]
        public string Content { get; set; } = string.Empty; // Mesaj içeriği

        public DateTime SentAt { get; set; } = DateTime.Now; // Gönderim tarihi

        public virtual ICollection<MessageRecipient> Recipients { get; set; } = new List<MessageRecipient>(); // Alıcılar
    }
}
