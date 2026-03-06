using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Micropad.App.DesignSystem;
using Micropad.Services.Automation;
using Micropad.Services.Storage;

namespace Micropad.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ForegroundMonitor _foregroundMonitor;
    private readonly SettingsStorage _settingsStorage;
    private readonly ThemeService _themeService;
    private readonly MotionService _motionService;
    private bool _loading;

    [ObservableProperty]
    private bool _autoConnect = true;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private bool _autoReconnect;

    [ObservableProperty]
    private string _newProcessName = "";

    [ObservableProperty]
    private int _newProfileId;

    [ObservableProperty]
    private ObservableCollection<KeyValuePair<string, int>> _processProfileMappings = new();

    [ObservableProperty]
    private bool _manualLock;

    [ObservableProperty]
    private string _defaultProfileIdText = "";

    [ObservableProperty]
    private string _debounceMsText = "800";

    [ObservableProperty]
    private bool _isDarkTheme = true;

    [ObservableProperty]
    private bool _reduceMotion;

    [ObservableProperty]
    private string _backgroundMode = "Solid";

    public IList<string> BackgroundModeOptions { get; } = new[] { "Solid", "Mica", "Acrylic" };

    public SettingsViewModel(ForegroundMonitor foregroundMonitor, SettingsStorage settingsStorage,
        ThemeService themeService, MotionService motionService)
    {
        _foregroundMonitor = foregroundMonitor;
        _settingsStorage = settingsStorage;
        _themeService = themeService;
        _motionService = motionService;
        LoadSettings();
    }

    partial void OnAutoConnectChanged(bool value) => SaveSettingsIfNotLoading();
    partial void OnStartWithWindowsChanged(bool value) { SaveSettingsIfNotLoading(); ApplyStartWithWindows(value); }
    partial void OnMinimizeToTrayChanged(bool value) => SaveSettingsIfNotLoading();
    partial void OnAutoReconnectChanged(bool value) => SaveSettingsIfNotLoading();

    private void LoadSettings()
    {
        _loading = true;
        try
        {
            var s = _settingsStorage.Load();
            AutoConnect = s.AutoConnect;
            StartWithWindows = s.StartWithWindows;
            MinimizeToTray = s.MinimizeToTray;
            AutoReconnect = s.AutoReconnect;
            ManualLock = s.ManualLock;
            DefaultProfileIdText = s.DefaultProfileId.HasValue ? s.DefaultProfileId.Value.ToString() : "";
            DebounceMsText = s.DebounceMs > 0 ? s.DebounceMs.ToString() : "800";
            IsDarkTheme = _themeService.IsDark;
            ReduceMotion = _motionService.ReduceMotion;
            BackgroundMode = string.IsNullOrEmpty(s.BackgroundMode) ? "Solid" : s.BackgroundMode;
            _foregroundMonitor.ManualLock = s.ManualLock;
            _foregroundMonitor.DefaultProfileId = s.DefaultProfileId;
            _foregroundMonitor.DebounceMs = s.DebounceMs > 0 ? s.DebounceMs : 800;
            foreach (var kv in s.ForegroundMonitorMappings)
                _foregroundMonitor.SetProcessProfileMapping(kv.Key, kv.Value);
            RefreshMappings();
            ApplyStartWithWindows(StartWithWindows);
        }
        finally
        {
            _loading = false;
        }
    }

    private void SaveSettingsIfNotLoading()
    {
        if (_loading) return;
        var s = _settingsStorage.Load();
        s.BackgroundMode = BackgroundMode;
        s.AutoConnect = AutoConnect;
        s.StartWithWindows = StartWithWindows;
        s.MinimizeToTray = MinimizeToTray;
        s.AutoReconnect = AutoReconnect;
        s.ForegroundMonitorMappings = new Dictionary<string, int>(_foregroundMonitor.GetMappings());
        s.ManualLock = ManualLock;
        s.DefaultProfileId = int.TryParse(DefaultProfileIdText?.Trim(), out var pid) && pid >= 0 && pid <= 7 ? pid : null;
        s.DebounceMs = int.TryParse(DebounceMsText?.Trim(), out var ms) && ms > 0 ? ms : 800;
        _settingsStorage.Save(s);
        _foregroundMonitor.ManualLock = ManualLock;
        _foregroundMonitor.DefaultProfileId = s.DefaultProfileId;
        _foregroundMonitor.DebounceMs = s.DebounceMs;
    }

    partial void OnManualLockChanged(bool value) => SaveSettingsIfNotLoading();
    partial void OnDefaultProfileIdTextChanged(string value) => SaveSettingsIfNotLoading();
    partial void OnDebounceMsTextChanged(string value) => SaveSettingsIfNotLoading();

    partial void OnIsDarkThemeChanged(bool value)
    {
        _themeService.IsDark = value;
        SaveSettingsIfNotLoading();
    }

    partial void OnReduceMotionChanged(bool value)
    {
        _motionService.ReduceMotion = value;
        SaveSettingsIfNotLoading();
    }

    partial void OnBackgroundModeChanged(string value) => SaveSettingsIfNotLoading();

    private static void ApplyStartWithWindows(bool enable)
    {
        try
        {
            const string key = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
            const string valueName = "Micropad";
            using var k = Registry.CurrentUser.OpenSubKey(key, writable: true);
            if (k == null) return;
            if (enable)
            {
                var exe = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
                    exe = Environment.ProcessPath ?? "";
                if (!string.IsNullOrEmpty(exe))
                    k.SetValue(valueName, "\"" + exe + "\"");
            }
            else
            {
                k.DeleteValue(valueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Ignore registry errors
        }
    }

    [RelayCommand]
    private void AddProcessMapping()
    {
        if (string.IsNullOrWhiteSpace(NewProcessName)) return;
        _foregroundMonitor.SetProcessProfileMapping(NewProcessName.Trim(), NewProfileId);
        RefreshMappings();
        NewProcessName = "";
        SaveSettingsIfNotLoading();
    }

    [RelayCommand]
    private void RemoveProcessMapping(string? processName)
    {
        if (processName != null)
        {
            _foregroundMonitor.RemoveProcessMapping(processName);
            RefreshMappings();
            SaveSettingsIfNotLoading();
        }
    }

    private void RefreshMappings()
    {
        ProcessProfileMappings.Clear();
        foreach (var kv in _foregroundMonitor.GetMappings())
        {
            ProcessProfileMappings.Add(new KeyValuePair<string, int>(kv.Key, kv.Value));
        }
    }
}
