using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Micropad.Services.Storage;

namespace Micropad.App.DesignSystem;

/// <summary>Accent color for focus rings, selected states, and brand. Persists to settings.</summary>
public class AccentService : INotifyPropertyChanged
{
    private readonly SettingsStorage _settingsStorage;
    private Color _accentColor;
    private SolidColorBrush? _accentBrush;

    public AccentService(SettingsStorage settingsStorage)
    {
        _settingsStorage = settingsStorage;
        _accentColor = ParseAccent(_settingsStorage.Load().AccentColorHex) ?? DefaultAccent;
        _accentBrush = new SolidColorBrush(_accentColor);
    }

    public static Color DefaultAccent => Color.FromRgb(0x00, 0x78, 0xD4);

    public event PropertyChangedEventHandler? PropertyChanged;

    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            if (_accentColor == value) return;
            _accentColor = value;
            _accentBrush = new SolidColorBrush(value);
            var s = _settingsStorage.Load();
            s.AccentColorHex = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
            _settingsStorage.Save(s);
            OnPropertyChanged();
            OnPropertyChanged(nameof(AccentBrush));
        }
    }

    public SolidColorBrush AccentBrush => _accentBrush ?? new SolidColorBrush(_accentColor);

    public void SetFromHex(string? hex)
    {
        var c = ParseAccent(hex);
        if (c.HasValue)
            AccentColor = c.Value;
    }

    private static Color? ParseAccent(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return null;
        try
        {
            var r = Convert.ToByte(hex.Substring(0, 2), 16);
            var g = Convert.ToByte(hex.Substring(2, 2), 16);
            var b = Convert.ToByte(hex.Substring(4, 2), 16);
            return Color.FromRgb(r, g, b);
        }
        catch { return null; }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
