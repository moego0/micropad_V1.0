using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Micropad.Core;
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

    [ObservableProperty]
    private ObservableCollection<MicroSlot> _slots = new();

    [ObservableProperty]
    private int _selectedSlotIndex = -1;

    [ObservableProperty]
    private string _selectedSequence = string.Empty;

    public ObservableCollection<MacroTag> AllTags { get; } = new();
    public IEnumerable<IGrouping<string, MacroTag>> TagGroups => AllTags.GroupBy(t => t.Group).OrderBy(g => g.Key);

    public bool IsNotRecording => !IsRecording;

    public int StepCount => Steps.Count;

    public MicroSlot? SelectedSlot => SelectedSlotIndex >= 0 && SelectedSlotIndex < Slots.Count ? Slots[SelectedSlotIndex] : null;

    partial void OnIsRecordingChanged(bool value) => OnPropertyChanged(nameof(IsNotRecording));
    partial void OnSelectedSlotIndexChanged(int value) => UpdateSelectedSequence();
    partial void OnSelectedSequenceChanged(string value)
    {
        if (SelectedSlot != null)
        {
            SelectedSlot.Sequence = value;
            OnPropertyChanged(nameof(Slots));
        }
    }

    public MacrosViewModel(MacroRecorder recorder, Micropad.Services.Storage.LocalMacroStorage macroStorage)
    {
        _recorder = recorder;
        _macroStorage = macroStorage;
        InitSlots();
        foreach (var t in MacroTagCatalog.GetAll())
            AllTags.Add(t);
    }

    private void InitSlots()
    {
        Slots.Clear();
        for (int i = 0; i < 12; i++)
            Slots.Add(new MicroSlot { Index = i, Label = $"K{i + 1}", IsEncoder = false });
        Slots.Add(new MicroSlot { Index = 12, Label = "E1", IsEncoder = true });
        Slots.Add(new MicroSlot { Index = 13, Label = "E2", IsEncoder = true });
    }

    private void UpdateSelectedSequence()
    {
        SelectedSequence = SelectedSlot?.Sequence ?? string.Empty;
    }

    [RelayCommand]
    private void SelectSlot(int index)
    {
        if (index >= 0 && index < Slots.Count)
        {
            SelectedSlotIndex = index;
            StatusText = $"Editing {Slots[index].Label}. Drag tags here or type below.";
        }
    }

    [RelayCommand]
    private void AppendTag(string? tag)
    {
        if (string.IsNullOrEmpty(tag)) return;
        if (SelectedSlot != null)
        {
            SelectedSlot.Sequence += tag;
            SelectedSequence = SelectedSlot.Sequence;
            OnPropertyChanged(nameof(Slots));
        }
        else
        {
            StatusText = "Select a key or encoder slot first.";
        }
    }

    public void DropTagOnSlot(int slotIndex, string tag)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count || string.IsNullOrEmpty(tag)) return;
        Slots[slotIndex].Sequence += tag;
        SelectedSlotIndex = slotIndex;
        SelectedSequence = Slots[slotIndex].Sequence;
        OnPropertyChanged(nameof(Slots));
        StatusText = $"Dropped {tag} on {Slots[slotIndex].Label}.";
    }

    [RelayCommand]
    private void ClearSlot(int index)
    {
        if (index >= 0 && index < Slots.Count)
        {
            Slots[index].Sequence = string.Empty;
            if (SelectedSlotIndex == index) SelectedSequence = string.Empty;
            OnPropertyChanged(nameof(Slots));
            StatusText = $"Cleared {Slots[index].Label}.";
        }
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
