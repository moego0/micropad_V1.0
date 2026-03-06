using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Micropad.App.Converters;

/// <summary>Null -> Collapsed, non-null -> Visible. Pass parameter "Inverse" for null -> Visible, non-null -> Collapsed.</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        bool inverse = string.Equals(parameter?.ToString(), "Inverse", StringComparison.OrdinalIgnoreCase);
        if (inverse)
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
