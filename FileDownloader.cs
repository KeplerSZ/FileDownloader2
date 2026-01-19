using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FileDownloader2.Services
{
    public class FileDownloader : IFileDownloader
    {
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileDownloader> _logger;
    private readonly IAppSettings _appSettings;
    
   
    public FileDownloader(HttpClient httpClient, ILogger<FileDownloader> logger,IAppSettings appSettings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _appSettings = appSettings;
    }
        public async Task<string> DownloadFileAsync(string url, string savePath)
        {
            try
            {
                 _logger.LogInformation("Поиск файлов на {Url}", url);

                string htmlContent = await _httpClient.GetStringAsync(_appSettings.FileUrl);

                //Находим самый свежий фаил
                var latestFile = FindLatestUpgradeFile(htmlContent);

                

                
                if (string.IsNullOrEmpty(latestFile))
                {
                     _logger.LogWarning("Не найдены файлы для скачивания");
                    return null;
                }
                string fileNameForSave = latestFile;
                
                _logger.LogInformation("Найден файл: {FileName}", latestFile);

                string downloadUrl = _appSettings.DownloadBaseUrl + fileNameForSave;

                //Сохраняем с оригинальным именем
                string directory = Path.GetDirectoryName(savePath); // Папка из savePath
                string correctSavePath = Path.Combine(directory, latestFile);
                _logger.LogInformation("Сохраняю в: {Path}", correctSavePath);
                //скачиваем

                return await DownloadActualFile(downloadUrl, correctSavePath);
            
            }
            catch (System.Exception)
            {
                
                throw;
            }
        }
        public async Task<string> GetLatestFileNameAsync(string url)
        {
            try
            {
                string htmlContent = await _httpClient.GetStringAsync(url);
                return FindLatestUpgradeFile(htmlContent);
            }
            catch
            {
                return null;
            }
        }
        private async Task<string> DownloadActualFile(string fileUrl, string savePath)
{
    try
    {
        _logger.LogInformation("Скачивание {FileUrl}", fileUrl);
        
        // Создаем папку если нужно
        var directory = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

                // 2. ПРОСТАЯ ПРОВЕРКА: если файл уже существует
                if (File.Exists(savePath))
                {
                    _logger.LogInformation($"Файл уже скачан: {savePath}");
                    Console.WriteLine($"Файл уже существует: {Path.GetFileName(savePath)}");

                    // Показываем информацию о существующем файле
                    FileInfo existingFile = new FileInfo(savePath);
                    Console.WriteLine($"Размер: {existingFile.Length} байт");
                    Console.WriteLine($"Создан: {existingFile.CreationTime:dd.MM.yyyy HH:mm}");

                    return savePath; // Возвращаем путь к существующему файлу
                }

                // Скачиваем файл
                using var response = await _httpClient.GetAsync(fileUrl);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Ошибка HTTP {StatusCode} при скачивании {Url}", 
                response.StatusCode, fileUrl);
            return null;
        }
        
        using var fileStream = File.Create(savePath);
        await response.Content.CopyToAsync(fileStream);
        
        // Проверяем размер
        var fileInfo = new FileInfo(savePath);
        _logger.LogInformation("Файл скачан: {Path} ({Size} байт)", 
            savePath, fileInfo.Length);
        
        return savePath;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка скачивания файла");
        throw;
    }
}
    // Метод для поиска самого свежего файла upgrade
    private string FindLatestUpgradeFile(string _httpClient)
    {
        try
        {
            // Список для хранения найденных файлов upgrade
            var upgradeFiles = new List<string>();
            
            // Ищем все файлы с "upgrade" и ".rar" в имени
            // Разбиваем HTML по ссылкам или по имени файла
            int startIndex = 0;
            
            while (true)
            {
                // Ищем начало имени файла (формат :upgradeYYYYMMDD...)
                int fileStart =  _httpClient.IndexOf("-upgrade", startIndex);
                if (fileStart == -1) break;
                
                // Ищем конец имени файла (.rar)
                int fileEnd =  _httpClient.IndexOf(".rar", fileStart);
                if (fileEnd == -1) break;
                
                // Извлекаем полное имя файла
                string fileName =  _httpClient.Substring(fileStart , fileEnd - fileStart + 4); // +1 чтобы убрать двоеточие, +3 для ".rar"
                upgradeFiles.Add(fileName);
                
                // Перемещаемся дальше
                startIndex = fileEnd + 4;
            }
            
            if (upgradeFiles.Count == 0)
            {
                Console.WriteLine("Не найдено файлов upgrade");
                return null;
            }
            
            Console.WriteLine($"Найдено файлов: {upgradeFiles.Count}");
            foreach (var file in upgradeFiles)
            {
                Console.WriteLine($"  - {file}");
            }
            
            // Ищем самый свежий файл (с максимальной датой в имени)
            string latestFile = null;
            DateTime latestDate = DateTime.MinValue;
            
            foreach (var file in upgradeFiles)
            {
                DateTime fileDate = ExtractDateFromFileName(file);
                
                if (fileDate > latestDate)
                {
                    latestDate = fileDate;
                    latestFile = file;
                }
            }
            
            return latestFile;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при поиске файлов: {ex.Message}");
            return null;
        }
    }
    

    

    // Метод для извлечения даты из имени файла
    private DateTime ExtractDateFromFileName(string fileName)
    {
        try
        {
            // Имя файла: upgrade202511211610361.rar
            // Нужно извлечь: 20251121 (YYYYMMDD)
            
            // Находим "upgrade" и берем следующие 8 цифр (год+месяц+день)
            int upgradeIndex = fileName.IndexOf("upgrade");
            if (upgradeIndex == -1) return DateTime.MinValue;
            
            string datePart = fileName.Substring(upgradeIndex + 7, 8); // 7 = длина "upgrade"
            
            // Парсим дату: 20251121 -> 2025-11-21
            if (DateTime.TryParseExact(datePart, "yyyyMMdd", 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            
            return DateTime.MinValue;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
     public async Task DownloadFile(string fileurl, string savePath)
    {
        try
        {
           
            byte[] fileData = await  _httpClient.GetByteArrayAsync(fileurl);
            await File.WriteAllBytesAsync(savePath, fileData);
            
            
            FileInfo fileInfo = new FileInfo(savePath);
            Console.WriteLine($"Размер файла: {fileInfo.Length} байт ({fileInfo.Length / 1024 / 1024} MB)");
        }
        catch (Exception ex)
        {
           System.Console.WriteLine($"Ошибка загрузки файла: {ex.Message}");
        }
    }
}
}