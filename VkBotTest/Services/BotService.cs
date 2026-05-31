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


    // При создании данного экземпляра передаем данные
    public BotService(VkApi vk, OllamaService ollama, StateService state, Logger logger, int pollingInterval)
    {
        _state = state;
        _logger = logger;

        // Создаем экземпляры для всего что бы работоло :)
        _polling = new BotPollingService(vk, state, logger, pollingInterval);
        _router = new MessageRouter();
        _commands = new CommandHandler(state);
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

    private async Task ProcessMessageAsync(VkNet.Model.Message msg)
    {
        if (_router.ShouldSkip(msg, _state.LastMessageId)) return;

        _state.IncrementMessages();
        _logger.Info($" [{msg.FromId}]: {msg.Text}");

        string response;
        var command = _router.DetermineCommand(msg);

        if (command != null)
        {
            response = await _commands.HandleAsync(command);
        }
        else if (_ollamaAvailable)
        {
            response = await _ai.GenerateAsync(msg.Text, msg.FromId.Value);
        }
        else
        {
            response = $" Эхо: {msg.Text}";
        }

        await _sender.SendAsync(msg.PeerId, response);

        _state.AppendHistory(new HistoryEntry
        {
            Time = DateTime.Now,
            UserId = msg.FromId.Value,
            Message = msg.Text,
            Response = response
        });
    }
}