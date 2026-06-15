using System;
using System.Linq;
using System.Threading.Tasks;

namespace VkBotTest.Services;

public class CommandHandler
{
    private readonly StateService _state;
    private readonly ReminderService _reminderService;

    public CommandHandler(StateService state, ReminderService reminderService)
    {
        _state = state;
        _reminderService = reminderService;
    }

    public async Task<string> HandleAsync(string cmd, long? userId = null)
    {
        return cmd.ToLower() switch
        {
            "/start" => "Привет! Я Джарвис. Спрашивай что угодно! 😊\n\nТеперь я умею напоминать! Просто напиши: 'напомни мне завтра в 15:00 сделать уроки'",

            "/help" => "Команды:\n/start — приветствие\n/help — эта справка\n/stats — статистика\n/ping — проверка связи\n/reminders — список напоминаний\n/clearremind — удалить все напоминания",

            "/ping" => $"Online. Uptime: {(DateTime.Now - _state.CurrentStats.StartTime).TotalMinutes:F0} мин.",

            "/stats" => _state.GetStatsReport(),

            "/reminders" => userId.HasValue ? GetUserReminders(userId.Value) : "Команда доступна только в личных сообщениях",

            "/clearremind" => userId.HasValue ? ClearReminders(userId.Value) : "Команда доступна только в личных сообщениях",

            "callback_useful" => "Спасибо! Рады, что ответ был полезен ",

            "callback_not_useful" => "Спасибо за отзыв! Мы постараемся улучшить качество ответов.",

            _ => "Неизвестная команда. Напишите /help для списка команд."
        };
    }

    private string GetUserReminders(long userId)
    {
        var reminders = _reminderService.GetUser(userId);
        if (!reminders.Any())
            return "У вас нет активных напоминаний.";

        var report = "Ваши напоминания:\n\n";
        int i = 1;
        foreach (var r in reminders)
            report += $"{i++}. [{r.TriggerTime:dd.MM.yyyy HH:mm}] — {r.Text}\n";

        return report;
    }

    private string ClearReminders(long userId)
    {
        var count = _reminderService.GetUser(userId).Count;
        _reminderService.ClearUser(userId);
        return $"Удалено напоминаний: {count}";
    }
}