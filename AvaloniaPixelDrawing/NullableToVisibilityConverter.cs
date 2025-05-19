﻿using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AvaloniaPixelDrawing;

public class NullableToVisibilityConverter : IValueConverter
{

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}