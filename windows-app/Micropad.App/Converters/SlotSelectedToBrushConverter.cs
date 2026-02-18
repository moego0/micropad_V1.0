using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Micropad.App.Converters;

/// <summary>Converts SelectedSlotIndex to a brush: selected = glow, else default. Parameter = slot index (e.g. "0"). Use "border" suffix for border brush.</summary>
public class SlotSelectedToBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int selectedIndex)
            return GetDefaultBrush(false);
        bool forBorder = false;
        int slotIndex;
        if (parameter is string paramStr)
        {
            var parts = paramStr.Split(';');
            var slotStr = parts[0].Trim();
            forBorder = parts.Length > 1 && parts[1].Trim().Equals("border", StringComparison.OrdinalIgnoreCase);
            if (!int.TryParse(slotStr, out slotIndex))
                return GetDefaultBrush(forBorder);
        }
        else if (parameter is int idx)
        {
            slotIndex = idx;
        }
        else
        {
            return GetDefaultBrush(false);
        }
        bool isSelected = selectedIndex == slotIndex;
        return isSelected ? GetSelectedBrush(forBorder) : GetDefaultBrush(forBorder);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();

    private static Brush? GetSelectedBrush(bool forBorder)
    {
        if (forBorder)
            return Application.Current?.Resources["BrandBlue"] as Brush;
        return Application.Current?.Resources["SlotSelectedBg"] as Brush;
    }

    private static Brush? GetDefaultBrush(bool forBorder)
    {
        if (forBorder)
            return Application.Current?.Resources["Border"] as Brush;
        return Application.Current?.Resources["BgTertiary"] as Brush;
    }
}
