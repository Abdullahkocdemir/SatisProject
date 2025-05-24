using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatışProject.Entities
{
    public class MessageRecipient
    {
        public int MessageRecipientId { get; set; }

        [Required]
        public int MessageId { get; set; } // Hangi mesajın alıcısı olduğu

        [ForeignKey("MessageId")]
        public virtual Message Message { get; set; } = null!;

        [Required]
        public string RecipientUserId { get; set; } = null!; // Alıcı kullanıcının Id'si

        [ForeignKey("RecipientUserId")]
        public virtual AppUser Recipient { get; set; } = null!; // Alıcı kullanıcı

        public bool IsRead { get; set; } = false; // Okundu bilgisi

        public DateTime? ReadAt { get; set; } // Okunma tarihi
    }
}
