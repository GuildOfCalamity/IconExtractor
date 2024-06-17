using System;
using Microsoft.UI.Xaml.Data;

namespace IconExtractor;

public class NumberToStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null) 
            return $"NULL";

        if (int.TryParse($"{value}", out var i))
            return parameter == null ? $"{i}" : $"{parameter} {i}";
        else if (float.TryParse($"{value}", out var f))
            return parameter == null ? $"{f:N1}" : $"{parameter} {f:N1}";
        else if (double.TryParse($"{value}", out var d))
            return parameter == null ? $"{d:N1}" : $"{parameter} {d:N1}";
        else
            return "0";
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return null;
    }
}

