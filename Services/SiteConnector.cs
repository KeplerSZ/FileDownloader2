using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;

namespace FileDownloader2.Services
{
    public class SiteConnector : ISiteConnector
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SiteConnector> _logger; // Исправьте тип логгера!
        private readonly IAppSettings _appSettings;
        
        public SiteConnector(
            HttpClient httpClient,
            ILogger<SiteConnector> logger, // Должен быть ILogger<SiteConnector>
            IAppSettings appSettings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _appSettings = appSettings;
        }
        
        public bool Connect()
        {
            var url = _appSettings.TestUrl;
            Console.WriteLine($"Проверка подключения к: {url}");
            _logger.LogInformation("Проверка подключения к {Url}", url);
            
            // Настройка авторизации
            string auth = $"{_appSettings.Username}:{_appSettings.Password}";
            string base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(auth));
            
            // Очищаем старые заголовки и добавляем новые
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Basic", base64);
            
            try
            {
                var response = _httpClient.GetAsync(url).Result;
                
                Console.WriteLine($"Статус: {response.StatusCode} ({(int)response.StatusCode})");
                _logger.LogInformation("Статус ответа: {StatusCode}", response.StatusCode);
                
                // Возвращаем true если 200 OK
                bool isSuccess = response.StatusCode == HttpStatusCode.OK;
                
                if (isSuccess)
                {
                    Console.WriteLine("✅ Авторизация успешна");
                    _logger.LogInformation("Авторизация успешна");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка: {response.StatusCode}");
                    _logger.LogWarning("Ошибка авторизации: {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Исключение: {ex.Message}");
                _logger.LogError(ex, "Ошибка подключения");
                return false;
            }
        }
    }
}