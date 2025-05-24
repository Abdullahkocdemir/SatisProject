namespace SatışProject.Models
{
    public class MessageRecipientViewModel
    {
        public string RecipientFullName { get; set; } = string.Empty;
        public string RecipientUserName { get; set; } = string.Empty;
        public bool IsRead { get; set; }
    }
}
