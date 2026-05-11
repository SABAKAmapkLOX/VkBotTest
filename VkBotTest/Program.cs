using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using VkNet;
using VkNet.Model;
using VkBotTest.Services;

namespace VkBotTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("VK BOT TEST v3.1 (Chat Context Isolation)\n");

        //Загруска конфигурации из json файлика
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsetting.json", optional: false, reloadOnChange: true)
            .Build();

        //Создание папки data и помещения туда логов и всего такого
        var dataFolder = config["Paths:DataFolder"] ?? "Data";
        Logger.Init(dataFolder);
        Logger.Info("Конфигурация загружена");

        //Загрузка в переменные данных с конфигуроочного json файлика
        var token = config["VkBot:AccessToken"];
        var pollingInterval = int.Parse(config["VkBot:PollingIntervalMs"] ?? "2000");
        var ollamaUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var ollamaModel = config["Ollama:Model"] ?? "llama3.2";

        //Инициализациия и авторизация 
        var vk = new VkApi();
        vk.Authorize(new ApiAuthParams { AccessToken = token });
        Logger.Info("VK API авторизован");

        //Отдаем месторасположение папки data и записываем туда логи и всю инфу
        var state = new StateService(dataFolder);
        state.SetStartTime();

        //Отдаем ollama ссылку для отпраки http запросов и модель
        var ollama = new OllamaService(ollamaUrl, ollamaModel);

        //Отдаем все настроенные обьекты боту для работы бота
        var bot = new BotService(vk, ollama, state, pollingInterval);
        await bot.InitializeAsync();

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Logger.Info("Получен сигнал остановки...");
            cts.Cancel();
        };

        Logger.Info($"Бот запущен. Последнее сообщение: {state.LastMessageId}");

        //Запуск бота 
        try
        {
            await bot.RunAsync(cts.Token);
        }
        catch (Exception ex)
        {
            Logger.Error($"Критическая ошибка: {ex.Message}");
        }
        finally
        {
            Logger.Info($" Завершено. Обработано сообщений: {state.CurrentStats.TotalMessages}");
        }
    }
}