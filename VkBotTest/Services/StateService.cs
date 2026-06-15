using System.Text.Json;
using VkBotTest.Models;

namespace VkBotTest.Services;

public class StateService
{
    private readonly string _dataFolder;
    private readonly string _lastIdFile;
    private readonly string _statsFile;
    private readonly string _historyFile;
    private readonly Logger _logger;

    public Stats CurrentStats { get; private set; } = new();

    // При создании экземпляра передаем эти параметры
    public StateService(string dataFolder, Logger logger)
    {
        _logger = logger;
        _dataFolder = dataFolder;
        Directory.CreateDirectory(dataFolder); // Создаем папку
        _lastIdFile = Path.Combine(dataFolder, "last_id.txt");

        _statsFile = Path.Combine(dataFolder, "stats.json"); // stats в stats.json
        _historyFile = Path.Combine(dataFolder, "history.csv"); // Создание excel файла с историей запросов пользователей

        Load();
    }

    private void Load()
    {
        long id = 0;

        if (File.Exists(_statsFile))
        {
            var json = File.ReadAllText(_statsFile); // Считываем
            CurrentStats = JsonSerializer.Deserialize<Stats>(json) ?? new Stats(); // Передаем его, если нету то создаем  новый
        }

        // КОСТЫЛЬ
        if (File.Exists(_historyFile))
            File.Delete(_historyFile);
        File.WriteAllText(_historyFile, "Time,UserId,Message,Response\n");
    }

    public void IncrementMessages()
    {
        CurrentStats.TotalMessages++;
        SaveStats();
    }

    private void SaveStats()
    {
        var json = JsonSerializer.Serialize(CurrentStats);
        File.WriteAllText(_statsFile, json);
    }

    public void AppendHistory(HistoryEntry entry)
    {
        // Заменяем переносы строк и экранируем кавычки
        var message = entry.Message
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\"", "\"\"");

        var response = entry.Response
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\"", "\"\"");

        var line = $"{entry.Time:yyyy-MM-dd HH:mm:ss},{entry.UserId},\"{message}\",\"{response}\"";
        File.AppendAllText(_historyFile, line + Environment.NewLine);
    }

    public List<HistoryEntry> GetHistory()
    {
        var list = new List<HistoryEntry>();
        if (!File.Exists(_historyFile)) return list;

        foreach (var line in File.ReadLines(_historyFile).Skip(1))
        {
            try
            {
                var parts = line.Split(',', 4);
                if (parts.Length == 4)
                {
                    list.Add(new HistoryEntry
                    {
                        Time = DateTime.Parse(parts[0]),
                        UserId = long.Parse(parts[1]),
                        Message = parts[2].Trim('"'),
                        Response = parts[3].Trim('"')
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка парсинга строки истории: {ex.Message}");
            }
        }
        return list;
    }

    public List<ChatMessage> GetUserChatContext(long userId, int maxExchanges = 6)
    {
        var logs = GetHistory()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.Time)
            .Take(maxExchanges * 2)
            .OrderBy(h => h.Time);

        var context = new List<ChatMessage>();
        foreach (var log in logs)
        {
            context.Add(new ChatMessage { role = "user", content = log.Message });
            context.Add(new ChatMessage { role = "assistant", content = log.Response });
        }
        return context;
    }

    public string GetStatsReport()
    {
        var uptime = DateTime.Now - CurrentStats.StartTime;
        var history = GetHistory();

        var topUsers = history
            .Where(x => x.UserId > 0)
            .GroupBy(x => x.UserId)
            .OrderByDescending(g => g.Count())
            .Take(3);

        var report = $"Статистика:\n" +
                    $" Работает: {uptime.Hours}ч {uptime.Minutes}м\n" +
                    $"Всего сообщений: {CurrentStats.TotalMessages}\n" +
                    $"Уникальных пользователей: {history.Count(x => x.UserId > 0)}\n\n" +
                    $"Топ-3 пользователей:\n";

        int rank = 1;
        foreach (var group in topUsers)
            report += $"{rank++}. ID {group.Key}: {group.Count()} сообщений\n";

        return report;
    }

    public void SetStartTime()
    {
        CurrentStats.StartTime = DateTime.Now;
        SaveStats();
    }

    // Добавь методы:
    public long GetLastProcessedId()
    {
        if (!File.Exists(_lastIdFile)) return 0;
        return long.TryParse(File.ReadAllText(_lastIdFile), out var id) ? id : 0;
    }

    public void SaveLastProcessedId(long id)
    {
        File.WriteAllText(_lastIdFile, id.ToString());
    }
}