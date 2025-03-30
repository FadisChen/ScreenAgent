namespace ScreenAgent.Models
{
    public class ConversationItem
    {
        public string UserPrompt { get; set; } = string.Empty;
        public string AiResponse { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}