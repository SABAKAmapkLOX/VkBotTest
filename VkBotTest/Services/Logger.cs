using System;
using System.IO;

namespace VkBotTest.Services;

public static class Logger
{
    private static string _logPath = "Data/bot.log";

    public static void Init(string dataFolder)
    {
        Directory.CreateDirectory(dataFolder);
        _logPath = Path.Combine(dataFolder, "bot.log");
    }

    public static void Info(string msg) => Write("INFO", msg);
    public static void Error(string msg) => Write("ERROR", msg);
    public static void Debug(string msg) => Write("DEBUG", msg);

    private static void Write(string level, string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {msg}";
        Console.WriteLine(line);
        File.AppendAllText(_logPath, line + Environment.NewLine);
    }
}