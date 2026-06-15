using System;
using System.Text.RegularExpressions;

namespace VkBotTest.Services;

public static class ReminderParser
{
    public static (DateTime? time, string text) Parse(string msg)
    {
        var lower = msg.ToLower();

        // "через N минут/часов"
        var m1 = Regex.Match(lower, @"через\s+(\d+)\s+(минут|мин|час|часа|часов)");
        if (m1.Success)
        {
            int val = int.Parse(m1.Groups[1].Value);
            var time = m1.Groups[2].Value.StartsWith("мин")
                ? DateTime.Now.AddMinutes(val)
                : DateTime.Now.AddHours(val);
            var text = msg.Replace(m1.Value, "").Replace("напомни", "").Replace("мне", "").Trim();
            return (time, string.IsNullOrEmpty(text) ? "Напоминание" : text);
        }

        // "завтра в HH:MM"
        var m2 = Regex.Match(lower, @"завтра\s+в\s+(\d{1,2}):(\d{2})");
        if (m2.Success)
        {
            var time = DateTime.Today.AddDays(1).AddHours(int.Parse(m2.Groups[1].Value)).AddMinutes(int.Parse(m2.Groups[2].Value));
            var text = msg.Replace(m2.Value, "").Replace("напомни", "").Replace("мне", "").Trim();
            return (time, string.IsNullOrEmpty(text) ? "Напоминание" : text);
        }

        // "сегодня в HH:MM" или просто "в HH:MM"
        var m3 = Regex.Match(lower, @"(сегодня\s+)?в\s+(\d{1,2}):(\d{2})");
        if (m3.Success)
        {
            var time = DateTime.Today.AddHours(int.Parse(m3.Groups[2].Value)).AddMinutes(int.Parse(m3.Groups[3].Value));
            if (time <= DateTime.Now) time = time.AddDays(1);
            var text = msg.Replace(m3.Value, "").Replace("напомни", "").Replace("мне", "").Trim();
            return (time, string.IsNullOrEmpty(text) ? "Напоминание" : text);
        }

        return (null, null);
    }
}