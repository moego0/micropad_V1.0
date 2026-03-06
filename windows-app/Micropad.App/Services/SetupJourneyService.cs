using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Micropad.App.Models;
using Micropad.Core.Interfaces;
using Micropad.Services.Storage;

namespace Micropad.App.Services;

/// <summary>Tracks setup journey steps (Connect device, First profile, First macro, etc.) for onboarding UI.</summary>
public class SetupJourneyService : INotifyPropertyChanged
{
    private bool _dismissedGettingStarted;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    public const string StepConnectDevice = "ConnectDevice";
    public const string StepFirstProfile = "FirstProfile";
    public const string StepFirstMacro = "FirstMacro";
    public const string StepUseTemplate = "UseTemplate";
    public const string StepConfigureEncoder = "ConfigureEncoder";
    public const string StepAutoSwitch = "AutoSwitch";

    private readonly IDeviceConnection _connection;
    private readonly LocalProfileStorage _profileStorage;
    private readonly LocalMacroStorage _macroStorage;
    private readonly SettingsStorage _settingsStorage;

    public ObservableCollection<SetupJourneyItem> Steps { get; } = new();

    public SetupJourneyService(
        IDeviceConnection connection,
        LocalProfileStorage profileStorage,
        LocalMacroStorage macroStorage,
        SettingsStorage settingsStorage)
    {
        _connection = connection;
        _profileStorage = profileStorage;
        _macroStorage = macroStorage;
        _settingsStorage = settingsStorage;

        _connection.Connected += (_, _) => Refresh();
        _connection.Disconnected += (_, _) => Refresh();
        _settingsStorage.SettingsSaved += (_, _) => Refresh();

        InitSteps();
        Refresh();
        _dismissedGettingStarted = _settingsStorage.Load().DismissedGettingStarted;
    }

    /// <summary>True when the Getting started panel should be shown.</summary>
    public bool IsGettingStartedVisible
    {
        get => !_dismissedGettingStarted;
        private set
        {
            if (_dismissedGettingStarted == value) return;
            _dismissedGettingStarted = !value;
            OnPropertyChanged();
        }
    }

    /// <summary>Close the panel. If dontShowAgain is true, it will not show again.</summary>
    public void DismissGettingStarted(bool dontShowAgain = true)
    {
        _dismissedGettingStarted = true;
        var settings = _settingsStorage.Load();
        settings.DismissedGettingStarted = true;
        _settingsStorage.Save(settings);
        OnPropertyChanged(nameof(IsGettingStartedVisible));
    }

    private void InitSteps()
    {
        Steps.Clear();
        Steps.Add(new SetupJourneyItem { Id = StepConnectDevice, Title = "Connect your Micropad" });
        Steps.Add(new SetupJourneyItem { Id = StepFirstProfile, Title = "Create your first profile" });
        Steps.Add(new SetupJourneyItem { Id = StepFirstMacro, Title = "Add a macro or shortcut" });
        Steps.Add(new SetupJourneyItem { Id = StepUseTemplate, Title = "Use a preset or template" });
        Steps.Add(new SetupJourneyItem { Id = StepConfigureEncoder, Title = "Configure an encoder" });
        Steps.Add(new SetupJourneyItem { Id = StepAutoSwitch, Title = "Enable profile auto-switch (optional)" });
    }

    public void Refresh()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var settings = _settingsStorage.Load();
            _dismissedGettingStarted = settings.DismissedGettingStarted;
            var completed = settings.CompletedSetupSteps ?? new List<string>();

            foreach (var step in Steps)
            {
                step.IsComplete = step.Id switch
                {
                    StepConnectDevice => _connection.IsConnected,
                    StepFirstProfile => _profileStorage.GetAllProfiles().Count >= 1,
                    StepFirstMacro => _macroStorage.GetAllMacroAssets().Count >= 1,
                    StepUseTemplate => completed.Contains(StepUseTemplate),
                    StepConfigureEncoder => completed.Contains(StepConfigureEncoder),
                    StepAutoSwitch => (settings.ForegroundMonitorMappings?.Count ?? 0) >= 1,
                    _ => false
                };
            }

            // When user finished all steps once, dismiss and don't show again
            if (Steps.All(s => s.IsComplete) && !_dismissedGettingStarted)
            {
                DismissGettingStarted(dontShowAgain: true);
            }
            else
            {
                OnPropertyChanged(nameof(IsGettingStartedVisible));
            }
        });
    }

    /// <summary>Mark a step as completed (for steps that are not derived from app state).</summary>
    public void MarkComplete(string stepId)
    {
        if (stepId != StepUseTemplate && stepId != StepConfigureEncoder)
            return;

        var settings = _settingsStorage.Load();
        if (settings.CompletedSetupSteps == null)
            settings.CompletedSetupSteps = new List<string>();
        if (!settings.CompletedSetupSteps.Contains(stepId))
        {
            settings.CompletedSetupSteps.Add(stepId);
            _settingsStorage.Save(settings);
        }
        Refresh();
    }

    public bool AllComplete => Steps.All(s => s.IsComplete);
}
