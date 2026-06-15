using System;
using System.Threading;
using System.Threading.Tasks;
using VkBotTest.Models;
using VkNet;
using VkNet.Model;

namespace VkBotTest.Services;

public class BotPollingService
{
    private readonly VkApi _vk;
    private readonly StateService _state;
    private readonly Logger _logger;
    private readonly int _pollingInterval;
    private readonly ReminderService _reminders;
    private int _reminderCounter;

    public BotPollingService(VkApi vk, StateService state, ReminderService reminders, Logger logger, int pollingInterval)
    {
        _vk = vk;
        _state = state;
        _reminders = reminders;
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

                _reminderCounter++;
                if (_reminderCounter >= 15) // каждые ~30 секунд
                {
                    foreach (var r in _reminders.GetDue())
                    {
                        try
                        {
                            await _vk.Messages.SendAsync(new MessagesSendParams
                            {
                                UserId = r.UserId,
                                Message = $"Напоминание: {r.Text}",
                                RandomId = new Random().Next()
                            });
                            _reminders.MarkSent(r.Id);
                        }
                        catch (Exception ex) { _logger.Error($"Ошибка напоминания: {ex.Message}"); }
                    }
                    _reminderCounter = 0;
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