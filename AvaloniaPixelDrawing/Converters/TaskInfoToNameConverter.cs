using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace AvaloniaPixelDrawing; // Выберите подходящий неймспейс

public class TaskInfoToNameConverter : IValueConverter
{
    // Статический экземпляр для удобного использования в XAML без создания нового каждый раз
    public static readonly TaskInfoToNameConverter Instance = new TaskInfoToNameConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TaskInfo taskInfo)
        {
            return taskInfo.Name;
        }
        // Если значение не TaskInfo или null, возвращаем что-то по умолчанию
        // или BindingOperations.DoNothing, чтобы привязка не обновлялась/не выдавала ошибку
        return BindingOperations.DoNothing; // или null, или string.Empty
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Обычно не требуется для отображения, если только у вас нет двусторонней привязки
        // и вы хотите создать TaskInfo из строки имени, что маловероятно в этом контексте.
        throw new NotImplementedException("ConvertBack не реализован для TaskInfoToNameConverter.");
        // или return BindingOperations.DoNothing;
    }
}