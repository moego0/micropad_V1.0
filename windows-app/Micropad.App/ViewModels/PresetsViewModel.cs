using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Micropad.Core.Models;
using Micropad.Services;
using Micropad.Services.Storage;

namespace Micropad.App.ViewModels;

public partial class PresetItem : ObservableObject
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string RequiredExe { get; set; } = ""; // e.g. "Code", "Figma", "Adobe Premiere Pro"
    public bool IsAppDetected { get; set; }
}

public partial class PresetsViewModel : ObservableObject
{
    private readonly ProfileSyncService _syncService;
    private readonly LocalProfileStorage _localStorage;

    [ObservableProperty]
    private ObservableCollection<PresetItem> _presets = new();

    [ObservableProperty]
    private string _statusText = "";

    public PresetsViewModel(ProfileSyncService syncService, LocalProfileStorage localStorage)
    {
        _syncService = syncService;
        _localStorage = localStorage;
        LoadPresets();
    }

    private void LoadPresets()
    {
        Presets.Clear();
        var items = new[]
        {
            new PresetItem
            {
                Id = "vscode",
                Name = "VS Code",
                Description = "Shortcuts for editing, terminal, git, refactor.",
                RequiredExe = "Code",
                IsAppDetected = IsProcessAvailable("Code")
            },
            new PresetItem
            {
                Id = "github",
                Name = "GitHub",
                Description = "Git and GitHub CLI (gh) workflows.",
                RequiredExe = "gh",
                IsAppDetected = IsProcessAvailable("gh") || File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "GitHub CLI", "gh.exe"))
            },
            new PresetItem
            {
                Id = "fusion360",
                Name = "Fusion 360",
                Description = "CAD shortcuts and timeline jog.",
                RequiredExe = "Fusion 360",
                IsAppDetected = IsProcessAvailable("Fusion 360") || IsProcessAvailable("Fusion360")
            },
            new PresetItem
            {
                Id = "figma",
                Name = "Figma",
                Description = "Design shortcuts, zoom, layers.",
                RequiredExe = "Figma",
                IsAppDetected = IsProcessAvailable("Figma")
            },
            new PresetItem
            {
                Id = "premiere",
                Name = "Adobe Premiere Pro",
                Description = "Timeline jog, markers, export.",
                RequiredExe = "Adobe Premiere Pro",
                IsAppDetected = IsProcessAvailable("Adobe Premiere Pro") || IsProcessAvailable("Adobe Premiere Pro 2024")
            }
        };
        foreach (var p in items)
            Presets.Add(p);
    }

    private static bool IsProcessAvailable(string processName)
    {
        try
        {
            return Process.GetProcessesByName(processName.Replace(" ", "")).Length > 0 ||
                   Process.GetProcesses().Any(pr => pr.ProcessName.Contains(processName, StringComparison.OrdinalIgnoreCase));
        }
        catch { return false; }
    }

    [RelayCommand]
    private void UsePreset(string presetId)
    {
        var profile = CreatePresetProfile(presetId);
        if (profile == null)
        {
            StatusText = "Unknown preset.";
            return;
        }
        _syncService.SaveProfileLocally(profile);
        StatusText = $"Created profile '{profile.Name}'. Open Profiles to edit and push to device.";
        MessageBox.Show($"Profile '{profile.Name}' has been added to your local library.\n\nGo to Profiles to edit and push to device.", "Preset applied", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static Profile? CreatePresetProfile(string presetId)
    {
        const int keyCount = 12;
        var keys = new List<KeyConfig>();
        for (int i = 0; i < keyCount; i++)
            keys.Add(new KeyConfig { Index = i, Type = ActionType.None });

        var encoders = new List<EncoderConfig>
        {
            new() { Index = 0, Acceleration = true, StepsPerDetent = 4 },
            new() { Index = 1, Acceleration = true, StepsPerDetent = 4 }
        };

        return presetId switch
        {
            "vscode" => new Profile
            {
                Id = 0,
                Name = "VS Code",
                Version = 1,
                Keys = ApplyKeys(keys, new[] { (0, ActionType.Hotkey, 0x01, 0x16), (1, ActionType.Hotkey, 0x02, 0x2F), (2, ActionType.Hotkey, 0x01, 0x37) }), // Ctrl+S, Shift+`, Ctrl+/
                Encoders = encoders
            },
            "github" => new Profile { Id = 0, Name = "GitHub", Version = 1, Keys = keys, Encoders = encoders },
            "fusion360" => new Profile { Id = 0, Name = "Fusion 360", Version = 1, Keys = keys, Encoders = encoders },
            "figma" => new Profile { Id = 0, Name = "Figma", Version = 1, Keys = keys, Encoders = encoders },
            "premiere" => new Profile { Id = 0, Name = "Adobe Premiere", Version = 1, Keys = keys, Encoders = encoders },
            _ => null
        };
    }

    private static List<KeyConfig> ApplyKeys(List<KeyConfig> keys, IEnumerable<(int index, ActionType type, int modifiers, int key)> assignments)
    {
        foreach (var (index, type, mod, key) in assignments)
        {
            if (index >= 0 && index < keys.Count)
            {
                keys[index].Type = type;
                keys[index].Modifiers = mod;
                keys[index].Key = key;
            }
        }
        return keys;
    }

    [RelayCommand]
    private void RefreshDetection()
    {
        LoadPresets();
        StatusText = "Refreshed app detection.";
    }
}
