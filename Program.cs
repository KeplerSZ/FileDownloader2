using FileDownloader2.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

Console.WriteLine("=== FileDownloader2 запущен ===");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("FileSettings.json",
            optional: false,
            reloadOnChange: true);
    })
    .ConfigureLogging((context,logging) =>
    {
        
        logging.AddFile("logs/fileDownloader.log", 
        minimumLevel: LogLevel.Information);
        

    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<AppSettings>(
            context.Configuration.GetSection("SitesSettings"));
        services.AddSingleton<IAppSettings>(sp =>
            sp.GetRequiredService<IOptions<AppSettings>>().Value);
        services.AddSingleton<HttpClient>();
        services.AddTransient<ISiteConnector, SiteConnector>();
        services.AddTransient<IFileDownloader, FileDownloader>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

// Получаем сервисы
var siteConnector = services.GetRequiredService<ISiteConnector>();
var fileDownloader = services.GetRequiredService<IFileDownloader>();
var settings = services.GetRequiredService<IAppSettings>();
var logger = services.GetRequiredService<ILogger<Program>>();

// 1. Проверяем подключение
logger.LogInformation("Проверка подключения к сайту...");
bool canConnect = siteConnector.Connect();

if (!canConnect)
{
    logger.LogError("❌ Не удалось подключиться к сайту");
    logger.LogError("Проверьте настройки в FileSettings.json");
    return; // Завершаем программу
}

logger.LogInformation("✅ Подключение успешно!");

// 2. Запускаем скачивание
// 2. Запускаем скачивание
try
{
    string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    string downloadsPath = Path.Combine(homePath, settings.DownloadFolder);
    string savePath = Path.Combine(downloadsPath, "consultant_latest.rar");
    
    // Создаем абсолютный путь если нужно
    if (!Path.IsPathRooted(savePath))
    {
        // Делаем путь относительно папки с программой
        savePath = Path.Combine(Directory.GetCurrentDirectory(), savePath);
    }
    
    // Создаем папку если её нет
    var directory = Path.GetDirectoryName(savePath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    {
        Console.WriteLine($"Создаем папку: {directory}");
        Directory.CreateDirectory(directory);
    }
    
    logger.LogInformation($"\nНачинаем скачивание...");
    logger.LogInformation($"URL для поиска файлов: {settings.FileUrl}");
    logger.LogInformation($"Файл будет сохранен в: {savePath}");
    
    
    string downloadedFilePath = await fileDownloader.DownloadFileAsync(
        settings.FileUrl, // Отсюда парсим HTML
        savePath);
    
    if (!string.IsNullOrEmpty(downloadedFilePath))
    {
        logger.LogInformation($"\n✅ Файл успешно скачан: {downloadedFilePath}");
        var fileInfo = new FileInfo(downloadedFilePath);
        logger.LogInformation($"Размер: {fileInfo.Length:N0} байт ({fileInfo.Length / 1024 / 1024} MB)");
    }
    else
    {
        logger.LogError("\n⚠️  Файл не скачан (возможно, не найдены файлы для скачивания)");
    }
}
catch (Exception ex)
{
    logger.LogError($"\n❌ Ошибка при скачивании: {ex.Message}");
    logger.LogError($"Детали: {ex.InnerException?.Message}");
}

logger.LogInformation("\n=== Завершение работы ===");