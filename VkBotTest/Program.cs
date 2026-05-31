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
        Console.WriteLine("VK BOT");

        // Загрузка конфигурации
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var dataFolder = config["Paths:DataFolder"] ?? "Data";

        // Создаём экземпляры
        var logger = new Logger(dataFolder);
        logger.Info("Конфигурация загружена"); // После создания logger говорим в консоль о правильности загруженных конфигов

        // Передаем нужные параметры из json 
        var token = config["VkBot:AccessToken"];
        var pollingInterval = int.Parse(config["VkBot:PollingIntervalMs"] ?? "2000");
        var ollamaUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var ollamaModel = config["Ollama:Model"] ?? "llama3.2";

        // Создаем экземпляр vk и авторизируемся с последущем успешном сообщение в консоль
        var vk = new VkApi();
        vk.Authorize(new ApiAuthParams { AccessToken = token }); // Авторизация
        logger.Info("VK API авторизован");

        // Создаем экземпляр сервиса управления состояние бота
        var state = new StateService(dataFolder, logger);
        state.SetStartTime();

        // Создаем экземляр ollama для работы с ии с передаем параметры
        var ollama = new OllamaService(ollamaUrl, ollamaModel, logger);

        // Создаем экземляр бота и передаем ему параметры
        var bot = new BotService(vk, ollama, state, logger, pollingInterval);
        await bot.InitializeAsync(ollama);

        // Поидее для правильной отсановки используется ctrl + c, добавил по приколу
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            logger.Info("Получен сигнал остановки...");
            cts.Cancel();
        };

        // Отправляем  сообщение в консоль об успешном запуске
        logger.Info($"Бот запущен. Последнее сообщение: {state.LastMessageId}");

        try
        {
            await bot.RunAsync(cts.Token); // Поехали!!!
        }
        catch (Exception ex)
        {
            logger.Error($"Критическая ошибка: {ex.Message}");
        }
        finally
        {
            logger.Info($"Завершено. Обработано сообщений: {state.CurrentStats.TotalMessages}");
        }
    }
}