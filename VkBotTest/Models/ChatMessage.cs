namespace VkBotTest.Models;

public class ChatMessage
{
    public string role { get; set; } = "";      // "system", "user", "assistant"
    public string content { get; set; } = "";
}