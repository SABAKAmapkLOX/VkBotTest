using VkNet;
using VkNet.Model;

namespace VkBotTest.Services;

public class BotPollingService
{
    private readonly VkApi _vk;
    private readonly StateService _state;
    private readonly Logger _logger;
    private readonly int _pollingInterval;

    public BotPollingService(VkApi vk, StateService state, Logger logger, int pollingInterval)
    {
        _vk = vk;
        _state = state;
        _logger = logger;
        _pollingInterval = pollingInterval;
    }

    // Запускает бесконечный цикл опроса сообщений
    public async Task StartAsync(CancellationToken token, Func<VkNet.Model.Message, Task> onMessage)
    {
        _logger.Info("Запуск polling-цикла...");

        while (!token.IsCancellationRequested)
        {
            try
            {
                var result = await _vk.Messages.GetConversationsAsync(
                    new GetConversationsParams { Count = 20 });

                foreach (var conv in result.Items)
                {
                    var msg = conv.LastMessage;
                    if (msg == null) continue;

                    await onMessage(msg);
                }

                await Task.Delay(_pollingInterval, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка в цикле: {ex.Message}");
                await Task.Delay(5000, token);
            }
        }

        _logger.Info("Цикл остановлен");
    }
}