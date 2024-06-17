using System;
using Microsoft.UI.Xaml.Data;

namespace IconExtractor;

public class BooleanInverseConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)// => !(bool)value;
    {
        bool? result = null;

        if (value is bool flag) { result = !flag; }

        return result;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return null;
    }
}

