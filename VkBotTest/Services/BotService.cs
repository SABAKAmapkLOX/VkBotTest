using VkBotTest.Models;
using VkNet;
using VkNet.Model;

namespace VkBotTest.Services;

public class BotService
{
    private readonly BotPollingService _polling;
    private readonly MessageRouter _router;
    private readonly CommandHandler _commands;
    private readonly AIResponder _ai;
    private readonly ResponseSender _sender;
    private readonly StateService _state;
    private readonly Logger _logger;
    private bool _ollamaAvailable;
    private readonly ReminderService _reminders;


    // При создании данного экземпляра передаем данные
    public BotService(VkApi vk, OllamaService ollama, StateService state, Logger logger, int pollingInterval, ReminderService reminders)
    {
        _state = state;
        _logger = logger;
        _reminders = reminders;

        // Создаем экземпляры для всего что бы работоло :)
        _polling = new BotPollingService(vk, state, reminders, logger, pollingInterval);
        _router = new MessageRouter();
        _commands = new CommandHandler(state, reminders);
        _ai = new AIResponder(ollama, state, logger);
        _sender = new ResponseSender(vk, logger);
    }

    public async Task InitializeAsync(OllamaService ollama)
    {
        _ollamaAvailable = await ollama.IsAvailableAsync();
        _logger.Info($"Ollama: {(_ollamaAvailable ? "подключена" : "недоступна")}");
    }

    public async Task RunAsync(CancellationToken token)
    {
        await _polling.StartAsync(token, ProcessMessageAsync);
    }

    private async Task ProcessMessageAsync(Message msg)
    {
        if (_router.ShouldSkip(msg)) return;
        _state.IncrementMessages();
        _logger.Info($"[{msg.FromId}]: {msg.Text}");

        string response;
        var command = _router.DetermineCommand(msg);

        if (command != null)
        {
            // Обработка команд /reminders и /clearremind
            if (command == "/reminders")
            {
                var list = _reminders.GetUser(msg.FromId.Value);
                response = list.Any()
                    ? "Ваши напоминания:\n" + string.Join("\n", list.Select((r, i) => $"{i + 1}. [{r.TriggerTime:dd.MM HH:mm}] {r.Text}"))
                    : " Нет активных напоминаний";
            }
            else if (command == "/clearremind")
            {
                _reminders.ClearUser(msg.FromId.Value);
                response = "Все напоминания удалены";
            }
            else
            {
                response = await _commands.HandleAsync(command);
            }
        }
        else if (msg.Text?.ToLower().Contains("напомни") == true)
        {
            var (time, text) = ReminderParser.Parse(msg.Text);
            if (time.HasValue)
            {
                _reminders.Add(msg.FromId.Value, text, time.Value);
                response = $"Запомню! Напомню {time.Value:dd.MM в HH:mm} — «{text}»";
            }
            else
            {
                response = " Не понял когда. Примеры:\n• напомни через 30 минут позвонить\n• напомни завтра в 15:00\n• напомни в 18:00";
            }
        }
        else if (_ollamaAvailable)
        {
            response = await _ai.GenerateAsync(msg.Text, msg.FromId.Value);
        }
        else
        {
            response = $"Эхо: {msg.Text}";
        }

        await _sender.SendAsync(msg.PeerId, response, command != null);
        _state.AppendHistory(new HistoryEntry { Time = DateTime.Now, UserId = msg.FromId.Value, Message = msg.Text, Response = response });
    }

    public async Task CheckRemindersAsync(VkApi vk)
    {
        foreach (var r in _reminders.GetDue())
        {
            try
            {
                await vk.Messages.SendAsync(new MessagesSendParams
                {
                    UserId = r.UserId,
                    Message = $"Напоминание: {r.Text}",
                    RandomId = new Random().Next()
                });
                _reminders.MarkSent(r.Id);
            }
            catch (Exception ex) { _logger.Error($"Ошибка напоминания: {ex.Message}"); }
        }
    }
}