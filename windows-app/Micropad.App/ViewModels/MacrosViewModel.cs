using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Micropad.Core.Models;
using Micropad.Services.Input;

namespace Micropad.App.ViewModels;

public partial class MacrosViewModel : ObservableObject
{
    private readonly MacroRecorder _recorder;
    private readonly Micropad.Services.Storage.LocalMacroStorage _macroStorage;

    [ObservableProperty]
    private ObservableCollection<MacroStep> _steps = new();

    [ObservableProperty]
    private string _macroName = "New Macro";

    [ObservableProperty]
    private string _statusText = "Create a macro or record one.";

    [ObservableProperty]
    private bool _isRecording;

    public bool IsNotRecording => !IsRecording;

    public int StepCount => Steps.Count;

    partial void OnIsRecordingChanged(bool value) => OnPropertyChanged(nameof(IsNotRecording));

    public MacrosViewModel(MacroRecorder recorder, Micropad.Services.Storage.LocalMacroStorage macroStorage)
    {
        _recorder = recorder;
        _macroStorage = macroStorage;
    }

    [RelayCommand]
    private void StartRecording()
    {
        _recorder.StartRecording();
        IsRecording = true;
        Steps.Clear();
        StatusText = "Recording... Press keys. Click Stop when done.";
    }

    [RelayCommand]
    private void StopRecording()
    {
        var recorded = _recorder.StopRecording();
        IsRecording = false;
        Steps.Clear();
        foreach (var s in recorded)
        {
            Steps.Add(s);
        }
        OnPropertyChanged(nameof(StepCount));
        StatusText = $"Recorded {Steps.Count} step(s).";
    }

    [RelayCommand]
    private void AddDelay()
    {
        Steps.Add(new MacroStep { Action = "delay", DelayMs = 100 });
        OnPropertyChanged(nameof(StepCount));
        StatusText = "Added delay step.";
    }

    [RelayCommand]
    private void AddKeyPress(string keyName)
    {
        if (string.IsNullOrEmpty(keyName)) keyName = "A";
        Steps.Add(new MacroStep { Action = "keyPress", Key = keyName });
    }

    [RelayCommand]
    private void RemoveStep(MacroStep? step)
    {
        if (step != null && Steps.Contains(step))
        {
            Steps.Remove(step);
            OnPropertyChanged(nameof(StepCount));
        }
    }

    [RelayCommand]
    private void ClearSteps()
    {
        Steps.Clear();
        OnPropertyChanged(nameof(StepCount));
        StatusText = "Steps cleared.";
    }

    [RelayCommand]
    private void SaveMacro()
    {
        var name = (MacroName ?? "").Trim();
        if (string.IsNullOrEmpty(name))
        {
            StatusText = "Enter a macro name first.";
            return;
        }
        if (Steps.Count == 0)
        {
            StatusText = "Add steps or record a macro first.";
            return;
        }
        _macroStorage.SaveMacro(name, GetStepsCopy());
        OnPropertyChanged(nameof(StepCount));
        StatusText = $"Saved macro '{name}'.";
    }

    public List<MacroStep> GetStepsCopy()
    {
        return Steps.Select(s => new MacroStep
        {
            Action = s.Action,
            Key = s.Key,
            DelayMs = s.DelayMs
        }).ToList();
    }
}
