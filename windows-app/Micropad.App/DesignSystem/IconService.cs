using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Micropad.App.DesignSystem;

/// <summary>Consistent icon system: Fluent/Segoe style, sizes 16/20/24. Returns TextBlock or Path for reuse.</summary>
public class IconService
{
    /// <summary>Segoe Fluent icon font (if available) or symbol. Sizes: 16, 20, 24.</summary>
    public static TextBlock GetSymbol(string symbolOrName, int sizePx = 20, Brush? foreground = null)
    {
        var tb = new TextBlock
        {
            Text = ResolveSymbol(symbolOrName),
            FontSize = sizePx,
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Symbol"),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        if (foreground != null)
            tb.Foreground = foreground;
        return tb;
    }

    private static string ResolveSymbol(string symbolOrName)
    {
        return symbolOrName.Length == 1 ? symbolOrName : GetNamedSymbol(symbolOrName);
    }

    private static string GetNamedSymbol(string name)
    {
        return name switch
        {
            "Devices" => "\uE8BE",
            "Profiles" => "\uE8A7",
            "Templates" => "\uE8F1",
            "Macros" => "\uE8ED",
            "Stats" => "\uE9D9",
            "Settings" => "\uE713",
            "Search" => "\uE721",
            "Add" => "\uE710",
            "Delete" => "\uE74D",
            "Edit" => "\uE70F",
            "Play" => "\uE768",
            "Stop" => "\uE71A",
            "Key" => "\uE92D",
            "Layer" => "\uE8F1",
            "Encoder" => "\uE7F8",
            "Favorite" => "\uE734",
            "FavoriteFill" => "\uE735",
            "Command" => "\uE756",
            "Check" => "\uE73E",
            "Warning" => "\uE7BA",
            "Error" => "\uE783",
            "Info" => "\uE946",
            _ => "\uE11D"
        };
    }
}
