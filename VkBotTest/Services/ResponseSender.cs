using VkNet;
using VkNet.Model;
using VkBotTest.Builders;

namespace VkBotTest.Services;

public class ResponseSender
{
    private readonly VkApi _vk;
    private readonly Logger _logger;

    public ResponseSender(VkApi vk, Logger logger)
    {
        _vk = vk;
        _logger = logger;
    }

    //Отправляет сообщение с клавиатурой
    public async Task SendAsync(long? peerId, string text, bool showKeyboard = false)
    {
        try
        {
            var sendParams = new MessagesSendParams
            {
                PeerId = peerId,
                Message = text,
                RandomId = Random.Shared.Next(),
                Keyboard = KeyboardBuilderFactory.CreateInlineKeyboard().Build()
            };

            if (showKeyboard)
            {
                sendParams.Keyboard = KeyboardBuilderFactory.CreateMainKeyboard().Build();
            }

            await _vk.Messages.SendAsync(sendParams);
            _logger.Info("Ответ отправлен");
        }
        catch (Exception ex)
        {
            _logger.Error($"Не смог отправить ответ: {ex.Message}");
        }
    }
}