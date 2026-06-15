using System;

namespace VkBotTest.Models;

public class Reminder
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public string ?Text { get; set; }           // Текст напоминания
    public DateTime TriggerTime { get; set; }  // Когда напомнить
    public DateTime CreatedAt { get; set; }    // Когда создано
    public bool IsSent { get; set; }           // Отправлено ли
}