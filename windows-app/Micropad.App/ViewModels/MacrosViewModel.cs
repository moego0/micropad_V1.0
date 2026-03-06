using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
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

    [ObservableProperty]
    private string _urlInput = string.Empty;

    [ObservableProperty]
    private string _applicationPathInput = string.Empty;

    [ObservableProperty]
    private string _selectedBrowser = "Default";

    [ObservableProperty]
    private string _textToAdd = string.Empty;

    [ObservableProperty]
    private MacroStep? _selectedStep;

    public bool IsInspectorVisible => SelectedStep != null;
    public bool IsDelayStep => SelectedStep?.Action == "delay";
    public bool IsTextStep => SelectedStep?.Action == "textType";
    public bool IsKeyStep => SelectedStep != null && (SelectedStep.Action == "keyPress" || SelectedStep.Action == "keyDown" || SelectedStep.Action == "keyUp");

    partial void OnSelectedStepChanged(MacroStep? value)
    {
        OnPropertyChanged(nameof(IsInspectorVisible));
        OnPropertyChanged(nameof(IsDelayStep));
        OnPropertyChanged(nameof(IsTextStep));
        OnPropertyChanged(nameof(IsKeyStep));
    }

    public ObservableCollection<string> Browsers { get; } = new()
    {
        "Default",
        "Chrome",
        "Edge",
        "Firefox",
        "Opera",
        "Brave"
    };

    public ObservableCollection<MacroTag> AllTags { get; } = new();
    public IEnumerable<IGrouping<string, MacroTag>> TagGroups => AllTags.GroupBy(t => t.Group).OrderBy(g => g.Key);

    public bool IsNotRecording => !IsRecording;

    public int StepCount => Steps.Count;

    public bool IsStepsEmpty => Steps.Count == 0;

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
        OnPropertyChanged(nameof(IsStepsEmpty));
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
        OnPropertyChanged(nameof(IsStepsEmpty));
        StatusText = $"Recorded {Steps.Count} step(s).";
    }

    [RelayCommand]
    private void AddDelay()
    {
        Steps.Add(new MacroStep { Action = "delay", DelayMs = 100 });
        OnPropertyChanged(nameof(StepCount));
        OnPropertyChanged(nameof(IsStepsEmpty));
        StatusText = "Added delay step.";
    }

    [RelayCommand]
    private void AddKeyPress(string keyName)
    {
        if (string.IsNullOrEmpty(keyName)) keyName = "A";
        Steps.Add(new MacroStep { Action = "keyPress", Key = keyName });
        OnPropertyChanged(nameof(StepCount));
        OnPropertyChanged(nameof(IsStepsEmpty));
    }

    [RelayCommand]
    private void AddText()
    {
        var text = (TextToAdd ?? "").Trim();
        if (string.IsNullOrEmpty(text))
        {
            StatusText = "Enter text first.";
            return;
        }
        Steps.Add(new MacroStep { Action = "textType", Text = text });
        TextToAdd = string.Empty;
        OnPropertyChanged(nameof(StepCount));
        OnPropertyChanged(nameof(IsStepsEmpty));
        StatusText = "Added text step.";
    }

    [RelayCommand]
    private void ClearMacro()
    {
        Steps.Clear();
        SelectedStep = null;
        MacroName = "New Macro";
        OnPropertyChanged(nameof(StepCount));
        OnPropertyChanged(nameof(IsStepsEmpty));
        StatusText = "Macro cleared. Record or add steps to start fresh.";
    }

    [RelayCommand]
    private void RemoveStep(MacroStep? step)
    {
        if (step != null && Steps.Contains(step))
        {
            if (step == SelectedStep) SelectedStep = null;
            Steps.Remove(step);
            OnPropertyChanged(nameof(StepCount));
            OnPropertyChanged(nameof(IsStepsEmpty));
        }
    }

    [RelayCommand]
    private void MoveStepUp(MacroStep? step)
    {
        if (step == null || !Steps.Contains(step)) return;
        var i = Steps.IndexOf(step);
        if (i <= 0) return;
        Steps.Move(i, i - 1);
        OnPropertyChanged(nameof(StepCount));
        OnPropertyChanged(nameof(IsStepsEmpty));
        StatusText = "Step moved up.";
    }

    [RelayCommand]
    private void MoveStepDown(MacroStep? step)
    {
        if (step == null || !Steps.Contains(step)) return;
        var i = Steps.IndexOf(step);
        if (i < 0 || i >= Steps.Count - 1) return;
        Steps.Move(i, i + 1);
        OnPropertyChanged(nameof(StepCount));
        OnPropertyChanged(nameof(IsStepsEmpty));
        StatusText = "Step moved down.";
    }

    [RelayCommand]
    private void ClearSteps()
    {
        Steps.Clear();
        OnPropertyChanged(nameof(StepCount));
        OnPropertyChanged(nameof(IsStepsEmpty));
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

    [RelayCommand]
    private void ExportMacro()
    {
        var name = (MacroName ?? "").Trim();
        if (string.IsNullOrEmpty(name)) name = "Unnamed";
        if (Steps.Count == 0)
        {
            StatusText = "Add steps or record a macro first.";
            return;
        }
        var dialog = new SaveFileDialog
        {
            Filter = "Macro JSON (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json",
            FileName = name + ".json"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var asset = new MacroAsset
            {
                Name = name,
                Steps = GetStepsCopy(),
                MacroId = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _macroStorage.ExportMacroAsset(asset, dialog.FileName);
            StatusText = $"Exported to {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusText = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ImportMacro()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Macro JSON (*.json)|*.json|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var asset = _macroStorage.ImportMacroAsset(dialog.FileName);
            if (asset != null)
            {
                MacroName = asset.Name;
                SelectedStep = null;
                Steps.Clear();
                foreach (var s in asset.Steps)
                    Steps.Add(s);
                OnPropertyChanged(nameof(StepCount));
                OnPropertyChanged(nameof(IsStepsEmpty));
                StatusText = $"Imported '{asset.Name}'. Assign it from Profiles.";
            }
            else
                StatusText = "Import failed or file invalid.";
        }
        catch (Exception ex)
        {
            StatusText = $"Import failed: {ex.Message}";
        }
    }

    public List<MacroStep> GetStepsCopy()
    {
        return Steps.Select(s => new MacroStep
        {
            Action = s.Action,
            Key = s.Key,
            DelayMs = s.DelayMs,
            Text = s.Text,
            Value = s.Value,
            VkCode = s.VkCode,
            MediaFunction = s.MediaFunction
        }).ToList();
    }

    [RelayCommand]
    private void InsertUrl()
    {
        if (string.IsNullOrWhiteSpace(UrlInput))
        {
            StatusText = "Enter a URL first.";
            return;
        }
        if (SelectedSlot == null)
        {
            StatusText = "Select a key or encoder slot first.";
            return;
        }

        var url = UrlInput.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        string browserTag;
        switch (SelectedBrowser)
        {
            case "Chrome":
                browserTag = "{RUN:chrome.exe " + url + "}";
                break;
            case "Edge":
                browserTag = "{RUN:msedge.exe " + url + "}";
                break;
            case "Firefox":
                browserTag = "{RUN:firefox.exe " + url + "}";
                break;
            case "Opera":
                browserTag = "{RUN:opera.exe " + url + "}";
                break;
            case "Brave":
                browserTag = "{RUN:brave.exe " + url + "}";
                break;
            default:
                // Use Windows shell start command for default browser
                browserTag = "{RUN:start " + url + "}";
                break;
        }

        SelectedSlot.Sequence += browserTag;
        SelectedSequence = SelectedSlot.Sequence;
        OnPropertyChanged(nameof(Slots));
        StatusText = $"Added URL to {SelectedSlot.Label}.";
        UrlInput = string.Empty;
    }

    [RelayCommand]
    private void InsertApplication()
    {
        if (string.IsNullOrWhiteSpace(ApplicationPathInput))
        {
            StatusText = "Enter an application path first.";
            return;
        }
        if (SelectedSlot == null)
        {
            StatusText = "Select a key or encoder slot first.";
            return;
        }

        var path = ApplicationPathInput.Trim();
        var appTag = "{RUN:" + path + "}";
        
        SelectedSlot.Sequence += appTag;
        SelectedSequence = SelectedSlot.Sequence;
        OnPropertyChanged(nameof(Slots));
        StatusText = $"Added application to {SelectedSlot.Label}.";
        ApplicationPathInput = string.Empty;
    }

    [RelayCommand]
    private void BrowseApplication()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            Title = "Select Application"
        };

        if (dialog.ShowDialog() == true)
        {
            ApplicationPathInput = dialog.FileName;
        }
    }
}
