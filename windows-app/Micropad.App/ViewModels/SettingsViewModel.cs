using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Micropad.Services.Automation;

namespace Micropad.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ForegroundMonitor _foregroundMonitor;

    [ObservableProperty]
    private bool _autoConnect = true;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private string _newProcessName = "";

    [ObservableProperty]
    private int _newProfileId;

    [ObservableProperty]
    private ObservableCollection<KeyValuePair<string, int>> _processProfileMappings = new();

    public SettingsViewModel(ForegroundMonitor foregroundMonitor)
    {
        _foregroundMonitor = foregroundMonitor;
        RefreshMappings();
    }

    [RelayCommand]
    private void AddProcessMapping()
    {
        if (string.IsNullOrWhiteSpace(NewProcessName)) return;
        _foregroundMonitor.SetProcessProfileMapping(NewProcessName.Trim(), NewProfileId);
        RefreshMappings();
        NewProcessName = "";
    }

    [RelayCommand]
    private void RemoveProcessMapping(string? processName)
    {
        if (processName != null)
        {
            _foregroundMonitor.RemoveProcessMapping(processName);
            RefreshMappings();
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
