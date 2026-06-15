using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VkBotTest.Models;

namespace VkBotTest.Services;

public class AIResponder
{
    private readonly OllamaService _ollama;
    private readonly StateService _state;
    private readonly Logger _logger;

    public AIResponder(OllamaService ollama, StateService state, Logger logger)
    {
        _ollama = ollama;
        _state = state;
        _logger = logger;
    }

    /// <summary>
    /// Отправка сообщения в ИИ с последующим ответом
    /// </summary>
    public async Task<string> GenerateAsync(string text, long userId)
    {
        var userHistory = _state.GetUserChatContext(userId);

        var chatMessages = new List<ChatMessage>
        {
            new ChatMessage
            {
                role = "system",
                content = "content = \"Ты дружелюбный помощник Джарвис в сообществе ВКонтакте. Отвечай полезно, интересно и развернуто, но без воды. Если не знаешь ответа, так и скажи. Помни контекст переписки.\""
            }
        };

        _logger.Info("Ответ отправен на обработку ии");

        chatMessages.AddRange(userHistory);
        chatMessages.Add(new ChatMessage { role = "user", content = text });

        return await _ollama.ChatAsync(chatMessages);
    }
}