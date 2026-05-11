using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VkNet;
using VkNet.Model;
using VkBotTest.Models;

namespace VkBotTest.Services;

public class BotService
{
    private readonly VkApi _vk;
    private readonly OllamaService _ollama;
    private readonly StateService _state;
    private readonly int _pollingInterval;
    private bool _ollamaAvailable;

    public BotService(VkApi vk, OllamaService ollama, StateService state, int pollingInterval)
    {
        _vk = vk;
        _ollama = ollama;
        _state = state;
        _pollingInterval = pollingInterval;
    }

    public async Task InitializeAsync()
    {
        _ollamaAvailable = await _ollama.IsAvailableAsync();
        Logger.Info($"Ollama: {(_ollamaAvailable ? "подключена" : "недоступна")}");
    }

    //Начало работы бота
    public async Task RunAsync(CancellationToken token)
    {
        Logger.Info("Запуск polling-цикла...");

        while (!token.IsCancellationRequested)
        {
            try
            {
                //Если нет ошибок в цикле то запускаем метод
                await PollMessagesAsync();
                await Task.Delay(_pollingInterval, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка в цикле: {ex.Message}");
                await Task.Delay(5000, token);
            }
        }

        Logger.Info("Цикл остановлен");
    }

    //Проверка последних сообщений
    private async Task PollMessagesAsync()
    {
        var result = await _vk.Messages.GetConversationsAsync(
            new GetConversationsParams { Count = 20 });  //Обращаемся к вк серверам для отдачи последних 20 сообщений

        //Цикл на проверку сообщений
        foreach (var conv in result.Items)
        {
            var msg = conv.LastMessage;
            if (ShouldSkip(msg)) continue;  //Метод для проверки был ли ответ на данное сообщение

            //Обновляем последний id сообщения и отдаем методу для обработки
            _state.UpdateLastId((long)msg.Id);
            await ProcessMessageAsync(msg);
        }
    }

    // Проверка сообщений
    private bool ShouldSkip(VkNet.Model.Message msg)
    {
        if (msg == null) return true;  //Проверка на текст
        if (msg.FromId < 0) return true;  //Проверка на сообщества 
        if (msg.OutRead == 1) return true;
        if (msg.Id <= _state.LastMessageId) return true;  //Проверка на id сообщения
        if (string.IsNullOrWhiteSpace(msg.Text)) return true;  //Проверка текста
        return false;
    }

    // Ответ на сообщения
    private async Task ProcessMessageAsync(VkNet.Model.Message msg)
    {
        //Отправка данных для статистики и отображение в консоли последнего смс
        _state.IncrementMessages();
        Logger.Info($" [{msg.FromId}]: {msg.Text}");

        string response;

        //Проверка что же в сообщении
        if (msg.Text.StartsWith("/"))
        {
            response = await HandleCommandAsync(msg.Text);//если сообшение начинается на /
                                                          //то отвечаем на шаблоны 
        }
        else if (_ollamaAvailable)
        {
            //Вызываем метод для получения истории
            var userHistory = _state.GetUserChatContext(msg.FromId.Value);

            //Создаем чат и кидаем ему промпт
            var chatMessages = new List<ChatMessage>
            {
                new ChatMessage
                {
                    role = "system",
                    content = "Ты AI-помощник в сообществе ВКонтакте. Отвечай кратко, дружелюбо и по делу. Помни контекст переписки с этим пользователем."
                }
            };
            chatMessages.AddRange(userHistory); //Обьеденение списка
            chatMessages.Add(new ChatMessage { role = "user", content = msg.Text }); //Добовляем в список

            response = await _ollama.ChatAsync(chatMessages); //Отдаем в ollama
        }
        else
        {
            response = $" Эхо: {msg.Text}"; //Заглушка если что то пошло не так с ollama
        }

        await SendResponseAsync(msg.PeerId, response); //Отправляем сообщение в вк

        _state.AppendHistory(new HistoryEntry
        {
            Time = DateTime.Now,
            UserId = msg.FromId.Value,
            Message = msg.Text,
            Response = response
        });
    }

    //Шаблоны
    private async Task<string> HandleCommandAsync(string cmd)
    {
        switch (cmd.ToLower())
        {
            case "/start": return "Привет!. Спрашивай что угодно!";
            case "/help": return "Команды:\n/start - начать\n/help - помощь\n/stats - статистика\n/ping - проверка связи";
            case "/ping": return $"Online. Uptime: {(DateTime.Now - _state.CurrentStats.StartTime).TotalMinutes:F0} мин.";
            case "/stats": return _state.GetStatsReport();  //Отдает статистику в чат
        }
        return "Неизвестная команда"; //Если команда не найдена, то отправится вот это
    }

    //Отправка сообщений
    private async Task SendResponseAsync(long? peerId, string text)
    {
        try
        {
            await _vk.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = peerId,
                Message = text,
                RandomId = Random.Shared.Next()
            });
            Logger.Info("Ответ отправлен");
        }
        catch (Exception ex)
        {
            Logger.Error($"Не смог отправить ответ: {ex.Message}");
        }
    }
}