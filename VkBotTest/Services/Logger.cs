using System;
using System.IO;

namespace VkBotTest.Services;

public class Logger
{
    private readonly string _logPath;

    // При создании экземпляра передаем параметры
    public Logger(string dataFolder)
    {
        Directory.CreateDirectory(dataFolder); // Создаем деректорию
        _logPath = Path.Combine(dataFolder, "bot.log"); // Записываем в файл bot.log
    }

    public void Info(string msg) => Write("INFO", msg);
    public void Error(string msg) => Write("ERROR", msg);
    public void Debug(string msg) => Write("DEBUG", msg);

    private void Write(string level, string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {msg}"; 
        Console.WriteLine(line);
        File.AppendAllText(_logPath, line + Environment.NewLine);
    }
}