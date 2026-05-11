using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VkBotTest.Models;

namespace VkBotTest.Services;

public class OllamaService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _model;

    public OllamaService(string baseUrl, string model)
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
    }

    public async Task<string> ChatAsync(List<ChatMessage> messages)
    {
        try
        {
            var request = new { model = _model, messages = messages, stream = false };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"{_baseUrl}/api/chat", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(result);

            return doc.RootElement
                      .GetProperty("message")
                      .GetProperty("content")
                      .GetString() ?? "AI не вернул ответ";
        }
        catch (TaskCanceledException)
        {
            Logger.Error("Ollama: тайм-аут (слишком длинный контекст?)");
            return "Ответ занимает слишком много времени. Попробуйте позже.";
        }
        catch (Exception ex)
        {
            Logger.Error($"Ollama Chat ошибка: {ex.Message}");
            return "️ AI временно недоступен.";
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var r = await _http.GetAsync($"{_baseUrl}/api/tags");
            return r.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}