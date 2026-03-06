using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Micropad.Services.Storage;
using Wpf.Ui.Appearance;
using ApplicationTheme = Wpf.Ui.Appearance.ApplicationTheme;

namespace Micropad.App.DesignSystem;

/// <summary>Light/Dark theme toggle. Syncs with Wpf.Ui ApplicationThemeManager and persists to settings.</summary>
public class ThemeService : INotifyPropertyChanged
{
    private readonly SettingsStorage _settingsStorage;
    private bool _isDark = true;

    public ThemeService(SettingsStorage settingsStorage)
    {
        _settingsStorage = settingsStorage;
        var s = _settingsStorage.Load();
        _isDark = string.IsNullOrEmpty(s.Theme) || string.Equals(s.Theme, "Dark", StringComparison.OrdinalIgnoreCase);
        ApplyThemeToWpfUi(_isDark);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>True = Dark, False = Light.</summary>
    public bool IsDark
    {
        get => _isDark;
        set
        {
            if (_isDark == value) return;
            _isDark = value;
            var s = _settingsStorage.Load();
            s.Theme = value ? "Dark" : "Light";
            _settingsStorage.Save(s);
            ApplyThemeToWpfUi(value);
            OnPropertyChanged();
        }
    }

    public void ApplyThemeToWpfUi(bool dark)
    {
        try
        {
            ApplicationThemeManager.Apply(dark ? ApplicationTheme.Dark : ApplicationTheme.Light);
        }
        catch (Exception)
        {
            // Wpf.Ui may not be fully initialized yet
        }
    }

    public void LoadFromSettings()
    {
        var s = _settingsStorage.Load();
        _isDark = string.IsNullOrEmpty(s.Theme) || string.Equals(s.Theme, "Dark", StringComparison.OrdinalIgnoreCase);
        ApplyThemeToWpfUi(_isDark);
        OnPropertyChanged(nameof(IsDark));
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
