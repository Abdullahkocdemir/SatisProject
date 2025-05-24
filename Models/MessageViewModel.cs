namespace SatışProject.Models
{
    public class MessageViewModel
    {
        public int MessageId { get; set; }
        public string SenderFullName { get; set; } = string.Empty;
        public string SenderUserName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public ICollection<MessageRecipientViewModel>? Recipients { get; set; } // Sadece giden mesajlar için
    }
}
