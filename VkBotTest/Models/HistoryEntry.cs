namespace VkBotTest.Models;

public class HistoryEntry
{
    public DateTime Time { get; set; }
    public long UserId { get; set; }
    public string Message { get; set; } = "";
    public string Response { get; set; } = "";
    public bool IsBot => UserId < 0;
}