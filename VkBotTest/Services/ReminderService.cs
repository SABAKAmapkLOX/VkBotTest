using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VkBotTest.Models;

namespace VkBotTest.Services;

public class ReminderService
{
    private readonly string _file;
    private readonly Logger _logger;
    private List<Reminder> _reminders;

    public ReminderService(string dataFolder, Logger logger)
    {
        _logger = logger;
        _file = Path.Combine(dataFolder, "reminders.json");
        _reminders = File.Exists(_file)
            ? JsonSerializer.Deserialize<List<Reminder>>(File.ReadAllText(_file)) ?? new()
            : new();
    }

    private void Save() => File.WriteAllText(_file, JsonSerializer.Serialize(_reminders, new JsonSerializerOptions { WriteIndented = true }));

    public void Add(long userId, string text, DateTime time)
    {
        _reminders.Add(new Reminder
        {
            Id = _reminders.Count > 0 ? _reminders.Max(r => r.Id) + 1 : 1,
            UserId = userId,
            Text = text,
            TriggerTime = time,
            IsSent = false
        });
        Save();
        _logger.Info($"Напоминание #{_reminders.Count} для {userId} на {time:dd.MM HH:mm}");
    }

    public List<Reminder> GetDue() => _reminders.Where(r => !r.IsSent && r.TriggerTime <= DateTime.Now).ToList();

    public void MarkSent(int id)
    {
        var r = _reminders.FirstOrDefault(x => x.Id == id);
        if (r != null) { r.IsSent = true; Save(); }
    }

    public List<Reminder> GetUser(long userId) => _reminders.Where(r => r.UserId == userId && !r.IsSent).ToList();

    public void ClearUser(long userId)
    {
        _reminders.RemoveAll(r => r.UserId == userId && !r.IsSent);
        Save();
    }
}