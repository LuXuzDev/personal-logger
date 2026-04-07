using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace LuxuzDev.PersonalLogger;

public class TelegramService
{
    private readonly HttpClient _httpClient;
    private readonly ITelegramBotSettings _botSettings;

    // Inyectamos HttpClient y los Settings
    public TelegramService(HttpClient httpClient, ITelegramBotSettings botSettings)
    {
        _httpClient = httpClient;
        _botSettings = botSettings;
    }
    
    public async Task SendAlertAsync(string message,string endpoint,string path, string method)
    {
        var ids = _botSettings.BotAdmins;
        if (ids == null || string.IsNullOrEmpty(_botSettings.BotToken)) return;

        foreach (var id in ids)
        {
            // Corregimos la URL: debe llevar 'api.' y el prefijo 'bot'
            var url = $"https://telegram.org{_botSettings.BotToken}/sendMessage";
            
            var payload = new 
            { 
                chat_id = id, 
                project_name = _botSettings.BotNameProject,
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Endpoint = endpoint,
                Path = path,
                Method = method,
                text = message, 
                parse_mode = "Markdown" 
            };

            try 
            {
                await _httpClient.PostAsJsonAsync(url, payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando a Telegram (ID: {id}): {ex.Message}");
            }
        }
    }
}