using VkBotTest.Services;

namespace VkBotTest.Services;

public class CommandHandler
{
    private readonly StateService _state;

    public CommandHandler(StateService state)
    {
        _state = state;
    }

    // Обрабатывает сообщение пользователя и возвращает ответ
    public async Task<string> HandleAsync(string cmd)
    {
        return cmd.ToLower() switch
        {
            "/start" => "Привет!. Спрашивай что угодно!",

            "/help" => "Команды:\n/start - начать\n/help - помощь\n/stats - статистика\n/ping - проверка связи",

            "/ping" => $"Online. Uptime: {(DateTime.Now - _state.CurrentStats.StartTime).TotalMinutes:F0} мин.",

            "/stats" => _state.GetStatsReport(),

            "callback_useful" => "Спасибо, делаем все возможное для вашего счастья",

            "callback_not_useful" => "Спасибо, мы сделаем все возможное что бы исправить"
        };
    }
}