using System.Text.Json;
using VkNet.Model;

namespace VkBotTest.Services;

public class MessageRouter
{

    // Определяет тип входящего сообщения команда или обычный диалог
    public string? DetermineCommand(Message msg)
    {
        // Проверяем payload
        if (!string.IsNullOrEmpty(msg.Payload))
        {
            try
            {
                using var doc = JsonDocument.Parse(msg.Payload);
                var firstProp = doc.RootElement.EnumerateObject().FirstOrDefault();
                return firstProp.Value.GetString();
            }
            catch { /* Игнорируем ошибки парсинга */ }
        }

        // Проверяем текст на команду
        if (msg.Text?.StartsWith("/") == true)
        {
            return msg.Text;
        }

        return null; // Обычный диалог
    }


    // Проверка сообщений
    public bool ShouldSkip(VkNet.Model.Message msg, long lastMessageId)
    {
        if (msg == null) return true;
        if (msg.FromId < 0) return true;           // Не группы
        if (msg.OutRead == 1) return true;         // Не исходящие
        if (msg.Id == lastMessageId) return true;  // Уже обработано
        if (string.IsNullOrWhiteSpace(msg.Text)) return true;
        return false;
    }
}