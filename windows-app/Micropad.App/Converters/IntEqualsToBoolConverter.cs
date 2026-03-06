using System;
using System.Globalization;
using System.Windows.Data;

namespace Micropad.App.Converters;

/// <summary>Parameter = int. Convert: value (int) == parameter -> true. ConvertBack: true -> parameter.</summary>
public class IntEqualsToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var paramStr = parameter?.ToString();
        if (!int.TryParse(paramStr, out var paramInt)) return false;
        if (value is int intVal) return intVal == paramInt;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter != null && int.TryParse(parameter.ToString(), out var paramInt))
            return paramInt;
        return Binding.DoNothing;
    }
}
