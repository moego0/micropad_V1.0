using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Micropad.Services.Storage;

namespace Micropad.App.DesignSystem;

/// <summary>Central motion settings: durations, easing, and reduce-motion. Respect Reduce Motion globally.</summary>
public class MotionService : INotifyPropertyChanged
{
    private readonly SettingsStorage _settingsStorage;
    private bool _reduceMotion;

    public MotionService(SettingsStorage settingsStorage)
    {
        _settingsStorage = settingsStorage;
        var s = _settingsStorage.Load();
        _reduceMotion = s.ReduceMotion;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>When true, non-essential animations should be disabled or shortened.</summary>
    public bool ReduceMotion
    {
        get => _reduceMotion;
        set
        {
            if (_reduceMotion == value) return;
            _reduceMotion = value;
            var s = _settingsStorage.Load();
            s.ReduceMotion = value;
            _settingsStorage.Save(s);
            OnPropertyChanged();
        }
    }

    /// <summary>Fast micro-interactions (hover, press). 0 when ReduceMotion.</summary>
    public int DurationFastMs => _reduceMotion ? 0 : 120;

    /// <summary>Normal transitions (layer switch, tab). 0 when ReduceMotion.</summary>
    public int DurationNormalMs => _reduceMotion ? 0 : 160;

    /// <summary>Slower (page, modal). 0 when ReduceMotion.</summary>
    public int DurationSlowMs => _reduceMotion ? 0 : 200;

    /// <summary>Accessibility: minimal feedback duration when ReduceMotion is true (e.g. focus).</summary>
    public int DurationMinimalMs => 50;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
