using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UI.Models;

namespace UI.Utils;

public class SaveLoader
{
    public string FileName { get; }

    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true, 
        IncludeFields = true
    };
    
    public SaveLoader(string fileName)
    {
        FileName = fileName;
    }

    public void Save(IEnumerable<TaskInfo> tasks)
    {
        // Сериализуем коллекцию объектов в строку JSON
        var jsonString = JsonSerializer.Serialize(tasks.ToArray(), _serializerOptions);

        // Записываем строку в файл. Если файл существует, он будет перезаписан.
        File.WriteAllText(FileName, jsonString);
    }

    public IEnumerable<TaskInfo> Load()
    {
        // 1. Проверяем, существует ли файл.
        if (!File.Exists(FileName))
        {
            // Если файла нет, возвращаем пустую коллекцию.
            // Это лучше, чем выбрасывать исключение, так как приложение сможет
            // просто начать работу с чистого листа.
            return Enumerable.Empty<TaskInfo>();
        }

        try
        {
            // 2. Читаем все содержимое файла в строку.
            string jsonString = File.ReadAllText(FileName);

            // Если файл пустой, вернем пустую коллекцию.
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return Enumerable.Empty<TaskInfo>();
            }

            // 3. Десериализуем (преобразуем) строку JSON обратно в список объектов TaskInfo.
            // Мы используем List<TaskInfo> как конкретный тип для десериализации.
            var tasks = JsonSerializer.Deserialize<TaskInfo[]>(jsonString, _serializerOptions);

            // Если по какой-то причине десериализация вернула null, 
            // мы вернем пустую коллекцию для безопасности.
            return tasks ?? Enumerable.Empty<TaskInfo>();
        }
        catch (JsonException ex)
        {
            // 4. Если файл поврежден и не является валидным JSON,
            // десериализатор выбросит исключение JsonException.
            // Мы ловим его, чтобы приложение не "упало".
            // В реальном приложении здесь можно добавить логирование ошибки.
            Console.WriteLine($"Ошибка при чтении файла сохранения: {ex.Message}");
            
            // Возвращаем пустой список, чтобы приложение могло продолжить работу.
            return Enumerable.Empty<TaskInfo>();
        }
        catch (Exception ex)
        {
            // Ловим другие возможные исключения при чтении файла (например, нет прав доступа)
            Console.WriteLine($"Произошла непредвиденная ошибка при загрузке: {ex.Message}");
            return Enumerable.Empty<TaskInfo>();
        }
    }
}