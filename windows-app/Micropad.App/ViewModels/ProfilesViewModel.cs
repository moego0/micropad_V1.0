using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Micropad.Core.Models;
using Micropad.App.Dialogs;
using Micropad.App.Models;
using Micropad.App.Services;
using Micropad.Services.Communication;
using Micropad.Services;

namespace Micropad.App.ViewModels;

public partial class ProfilesViewModel : ObservableObject
{
    private const int KeyCount = 12;
    private readonly ProtocolHandler _protocol;
    private readonly ProfileSyncService _syncService;
    private readonly Micropad.Services.Storage.LocalMacroStorage _macroStorage;
    private readonly HudService? _hudService;
    private readonly SetupJourneyService? _setupJourney;
    private readonly IProfileConflictResolver? _conflictResolver;

    [ObservableProperty]
    private ObservableCollection<Profile> _profiles = new();

    [ObservableProperty]
    private Profile? _selectedProfile;

    [ObservableProperty]
    private Profile? _editingProfile;

    [ObservableProperty]
    private ObservableCollection<KeyConfig> _keySlots = new();

    [ObservableProperty]
    private string _statusText = "Click Refresh to load profiles";

    [ObservableProperty]
    private int? _activeProfileId;

    [ObservableProperty]
    private string _deviceCapacityText = "";

    [ObservableProperty]
    private int _selectedLayerIndex = 0;

    [ObservableProperty]
    private ObservableCollection<ComboAssignment> _comboList = new();

    [ObservableProperty]
    private ObservableCollection<ActionLibraryItem> _actionLibraryItems = new();

    [ObservableProperty]
    private ObservableCollection<ActionLibraryItem> _filteredActionLibrary = new();

    [ObservableProperty]
    private string _actionSearchText = "";

    [ObservableProperty]
    private string _selectedActionCategory = "All";

    [ObservableProperty]
    private bool _isPushInProgress;

    [ObservableProperty]
    private string _pushStepText = "";

    private readonly HashSet<string> _favoriteActionIds = new();
    private List<ActionLibraryItem> _allActionLibraryList = new();

    public IList<string> ActionCategories { get; } = new[] { "All", "Keyboard", "Text", "Media", "Mouse", "App", "Profile", "Macro", "Favorites" };

    [ObservableProperty]
    private int? _liveLayerIndex;

    [ObservableProperty]
    private bool _isProfilesLoading;

    /// <summary>Key slot index that is currently selected (for focus ring). -1 = none.</summary>
    [ObservableProperty]
    private int _selectedKeySlotIndex = -1;

    public ProfilesViewModel(ProtocolHandler protocol, ProfileSyncService syncService, Micropad.Services.Storage.LocalMacroStorage macroStorage, HudService? hudService = null, SetupJourneyService? setupJourney = null, IProfileConflictResolver? conflictResolver = null)
    {
        _protocol = protocol;
        _syncService = syncService;
        _macroStorage = macroStorage;
        _hudService = hudService;
        _setupJourney = setupJourney;
        _conflictResolver = conflictResolver;
        _protocol.EventReceived += OnProtocolEvent;
        BuildActionLibrary();
    }

    private void OnProtocolEvent(object? sender, ProtocolMessage msg)
    {
        if (msg.Event != "layerChanged") return;
        var layerToken = msg.Payload?["layer"];
        if (layerToken != null && int.TryParse(layerToken.ToString(), out var layer))
        {
            LiveLayerIndex = layer >= 0 && layer <= 2 ? layer : (int?)null;
        }
    }

    partial void OnActionSearchTextChanged(string value) => RefreshFilteredActions();
    partial void OnSelectedActionCategoryChanged(string value) => RefreshFilteredActions();

    private static void EnsureKeys(Profile profile)
    {
        while (profile.Keys.Count < KeyCount)
        {
            profile.Keys.Add(new KeyConfig
            {
                Index = profile.Keys.Count,
                Type = ActionType.None
            });
        }
        for (int i = 0; i < profile.Keys.Count; i++)
        {
            profile.Keys[i].Index = i;
        }
    }

    private static void EnsureLayerKeys(Profile profile)
    {
        if (profile.Layer1Keys == null)
        {
            profile.Layer1Keys = new List<KeyConfig>();
            for (int i = 0; i < KeyCount; i++)
                profile.Layer1Keys.Add(new KeyConfig { Index = i, Type = ActionType.None });
        }
        else
        {
            while (profile.Layer1Keys.Count < KeyCount)
                profile.Layer1Keys.Add(new KeyConfig { Index = profile.Layer1Keys.Count, Type = ActionType.None });
            for (int i = 0; i < profile.Layer1Keys.Count; i++) profile.Layer1Keys[i].Index = i;
        }
        if (profile.Layer2Keys == null)
        {
            profile.Layer2Keys = new List<KeyConfig>();
            for (int i = 0; i < KeyCount; i++)
                profile.Layer2Keys.Add(new KeyConfig { Index = i, Type = ActionType.None });
        }
        else
        {
            while (profile.Layer2Keys.Count < KeyCount)
                profile.Layer2Keys.Add(new KeyConfig { Index = profile.Layer2Keys.Count, Type = ActionType.None });
            for (int i = 0; i < profile.Layer2Keys.Count; i++) profile.Layer2Keys[i].Index = i;
        }
    }

    private static void EnsureEncoders(Profile profile)
    {
        if (profile.Encoders == null) profile.Encoders = new List<EncoderConfig>();
        while (profile.Encoders.Count < 2)
        {
            profile.Encoders.Add(new EncoderConfig
            {
                Index = profile.Encoders.Count,
                CwAction = new EncoderActionConfig { Type = "none" },
                CcwAction = new EncoderActionConfig { Type = "none" },
                PressAction = new EncoderActionConfig { Type = "none" }
            });
        }
        for (int i = 0; i < profile.Encoders.Count; i++) profile.Encoders[i].Index = i;
    }

    public IList<string> EncoderPresetNames { get; } = new[] { "None", "Volume", "Scroll", "Zoom", "Media" };

    [RelayCommand]
    private void ApplyEncoderPreset(object? parameter)
    {
        if (EditingProfile?.Encoders == null || parameter is not string s) return;
        var parts = s.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var encIndex) || encIndex < 0 || encIndex >= EditingProfile.Encoders.Count) return;
        var preset = parts[1];
        var enc = EditingProfile.Encoders[encIndex];
        enc.CwAction ??= new EncoderActionConfig();
        enc.CcwAction ??= new EncoderActionConfig();
        enc.PressAction ??= new EncoderActionConfig();
        switch (preset)
        {
            case "Volume":
                enc.CwAction.Type = "volume"; enc.CwAction.MediaFunction = (int)MediaFunction.VolumeUp;
                enc.CcwAction.Type = "volume"; enc.CcwAction.MediaFunction = (int)MediaFunction.VolumeDown;
                enc.PressAction.Type = "media"; enc.PressAction.MediaFunction = (int)MediaFunction.Mute;
                break;
            case "Scroll":
                enc.CwAction.Type = "scrollV"; enc.CwAction.Value = -1;
                enc.CcwAction.Type = "scrollV"; enc.CcwAction.Value = 1;
                enc.PressAction.Type = "none";
                break;
            case "Zoom":
                enc.CwAction.Type = "hotkey"; enc.CwAction.Modifiers = 0x01; enc.CwAction.Key = 0x35; // Ctrl+=
                enc.CcwAction.Type = "hotkey"; enc.CcwAction.Modifiers = 0x01; enc.CcwAction.Key = 0x36; // Ctrl+-
                enc.PressAction.Type = "none";
                break;
            case "Media":
                enc.CwAction.Type = "media"; enc.CwAction.MediaFunction = (int)MediaFunction.Next;
                enc.CcwAction.Type = "media"; enc.CcwAction.MediaFunction = (int)MediaFunction.Prev;
                enc.PressAction.Type = "media"; enc.PressAction.MediaFunction = (int)MediaFunction.PlayPause;
                break;
            default:
                enc.CwAction.Type = enc.CcwAction.Type = enc.PressAction.Type = "none";
                break;
        }
        OnPropertyChanged(nameof(EditingProfile));
        _setupJourney?.MarkComplete(SetupJourneyService.StepConfigureEncoder);
        StatusText = $"Encoder {encIndex + 1}: applied preset '{preset}'.";
    }

    [RelayCommand]
    private async Task LoadProfilesAsync()
    {
        IsProfilesLoading = true;
        try
        {
            StatusText = "Loading profiles...";

            var profileList = await _protocol.ListProfilesAsync();
            var localProfiles = _syncService.GetLocalProfiles();

            var combined = new List<Profile>();
            if (profileList != null)
                combined.AddRange(profileList);
            var deviceIds = combined.Select(p => p.Id).ToHashSet();
            foreach (var local in localProfiles)
            {
                if (deviceIds.Contains(local.Id)) continue;
                combined.Add(local);
            }
            Profiles.Clear();
            foreach (var p in combined.OrderBy(x => x.Id))
                Profiles.Add(p);

            ActiveProfileId = await _syncService.GetActiveProfileIdAsync();
            var caps = await _syncService.GetCapsAsync();
            DeviceCapacityText = caps != null
                ? $"Device: {Profiles.Count}/{caps.MaxProfiles} slots, {caps.FreeBytes} bytes free"
                : "";

            StatusText = $"Loaded {Profiles.Count} profile(s)" + (ActiveProfileId.HasValue ? $" • Active: slot {ActiveProfileId}" : "");
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to load profiles: {ex.Message}";
        }
        finally
        {
            IsProfilesLoading = false;
        }
    }

    partial void OnSelectedProfileChanged(Profile? value)
    {
        SelectedKeySlotIndex = -1;
        if (value != null)
        {
            _ = LoadProfileDetailsAsync();
        }
    }

    partial void OnSelectedLayerIndexChanged(int value)
    {
        RefreshKeySlots();
    }

    [RelayCommand]
    private async Task LoadProfileDetailsAsync()
    {
        if (SelectedProfile == null) return;

        try
        {
            var fullProfile = await _protocol.GetProfileAsync(SelectedProfile.Id);
            if (fullProfile != null)
            {
                EnsureKeys(fullProfile);
                EnsureLayerKeys(fullProfile);
                SelectedProfile = fullProfile;
                EditingProfile = CloneProfile(fullProfile);
                SelectedLayerIndex = 0;
                RefreshKeySlots();
                RefreshComboList();
                StatusText = $"Editing: {fullProfile.Name}";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to load profile details: {ex.Message}";
        }
    }

    private static Profile CloneProfile(Profile p)
    {
        return new Profile
        {
            Id = p.Id,
            Name = p.Name,
            Version = p.Version,
            Keys = p.Keys.Select(k => new KeyConfig
            {
                Index = k.Index,
                Type = k.Type,
                Modifiers = k.Modifiers,
                Key = k.Key,
                Text = k.Text,
                Function = k.Function,
                Action = k.Action,
                Value = k.Value,
                ProfileId = k.ProfileId,
                AppPath = k.AppPath,
                Url = k.Url,
                MacroId = k.MacroId,
                MacroSnapshot = k.MacroSnapshot,
                TapAction = k.TapAction,
                HoldAction = k.HoldAction,
                DoubleTapAction = k.DoubleTapAction
            }).ToList(),
            Encoders = p.Encoders.Select(e => new EncoderConfig
            {
                Index = e.Index,
                Acceleration = e.Acceleration,
                StepsPerDetent = e.StepsPerDetent,
                StepSize = e.StepSize,
                AccelerationCurve = e.AccelerationCurve,
                Smoothing = e.Smoothing,
                Mode = e.Mode,
                CwAction = e.CwAction,
                CcwAction = e.CcwAction,
                PressAction = e.PressAction,
                HoldAction = e.HoldAction,
                PressRotateCwAction = e.PressRotateCwAction,
                PressRotateCcwAction = e.PressRotateCcwAction,
                HoldRotateCwAction = e.HoldRotateCwAction,
                HoldRotateCcwAction = e.HoldRotateCcwAction
            }).ToList(),
            Layer1Keys = p.Layer1Keys?.Select(k => new KeyConfig
            {
                Index = k.Index,
                Type = k.Type,
                Modifiers = k.Modifiers,
                Key = k.Key,
                Text = k.Text,
                Function = k.Function,
                Action = k.Action,
                Value = k.Value,
                ProfileId = k.ProfileId,
                AppPath = k.AppPath,
                Url = k.Url,
                MacroId = k.MacroId,
                MacroSnapshot = k.MacroSnapshot,
                TapAction = k.TapAction,
                HoldAction = k.HoldAction,
                DoubleTapAction = k.DoubleTapAction
            }).ToList(),
            Layer2Keys = p.Layer2Keys?.Select(k => new KeyConfig
            {
                Index = k.Index,
                Type = k.Type,
                Modifiers = k.Modifiers,
                Key = k.Key,
                Text = k.Text,
                Function = k.Function,
                Action = k.Action,
                Value = k.Value,
                ProfileId = k.ProfileId,
                AppPath = k.AppPath,
                Url = k.Url,
                MacroId = k.MacroId,
                MacroSnapshot = k.MacroSnapshot,
                TapAction = k.TapAction,
                HoldAction = k.HoldAction,
                DoubleTapAction = k.DoubleTapAction
            }).ToList(),
            Combos = p.Combos != null ? new List<ComboAssignment>(p.Combos) : null
        };
    }

    private void RefreshKeySlots()
    {
        KeySlots.Clear();
        if (EditingProfile != null)
        {
            EnsureKeys(EditingProfile);
            EnsureLayerKeys(EditingProfile);
            EnsureEncoders(EditingProfile);
            List<KeyConfig>? source = SelectedLayerIndex switch
            {
                1 => EditingProfile.Layer1Keys,
                2 => EditingProfile.Layer2Keys,
                _ => EditingProfile.Keys
            };
            if (source != null)
            {
                for (int i = 0; i < KeyCount; i++)
                    KeySlots.Add(i < source.Count ? source[i] : new KeyConfig { Index = i, Type = ActionType.None });
            }
            else
            {
                for (int i = 0; i < KeyCount; i++)
                    KeySlots.Add(new KeyConfig { Index = i, Type = ActionType.None });
            }
        }
        else
        {
            for (int i = 0; i < KeyCount; i++)
                KeySlots.Add(new KeyConfig { Index = i, Type = ActionType.None });
        }
    }

    private void RefreshComboList()
    {
        ComboList.Clear();
        if (EditingProfile?.Combos != null)
        {
            foreach (var c in EditingProfile.Combos)
                ComboList.Add(c);
        }
        OnPropertyChanged(nameof(IsComboListEmpty));
    }

    public bool IsComboListEmpty => ComboList.Count == 0;

    [RelayCommand]
    private void EditKey(object? parameter)
    {
        var index = parameter is int i ? i : (parameter is string s && int.TryParse(s, out var parsed) ? parsed : -1);
        if (EditingProfile == null || index < 0 || index >= KeySlots.Count) return;

        SelectedKeySlotIndex = index;
        var keyConfig = KeySlots[index];
        var macroAssets = _macroStorage.GetAllMacroAssets();
        var dialog = new ActionEditWindow(keyConfig, index, macroAssets)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            var result = dialog.Result;
            keyConfig.Type = result.Type;
            keyConfig.Modifiers = result.Modifiers;
            keyConfig.Key = result.Key;
            keyConfig.Text = result.Text;
            keyConfig.Function = result.Function;
            keyConfig.Action = result.Action;
            keyConfig.Value = result.Value;
            keyConfig.ProfileId = result.ProfileId;
            keyConfig.AppPath = result.AppPath;
            keyConfig.Url = result.Url;
            keyConfig.MacroId = result.MacroId;
            keyConfig.MacroSnapshot = result.MacroSnapshot;
            OnPropertyChanged(nameof(KeySlots));
            StatusText = "Key updated. Push to device to save.";
        }
        // Keep SelectedKeySlotIndex so the edited key stays highlighted
    }

    [RelayCommand]
    private async Task PushToDeviceAsync()
    {
        if (EditingProfile == null)
        {
            StatusText = "No profile selected";
            return;
        }

        try
        {
            IsPushInProgress = true;
            PushStepText = "Preparing...";

            // Conflict check: device has same profile id but different version?
            var deviceProfile = await _syncService.PullProfileFromDeviceAsync(EditingProfile.Id);
            if (deviceProfile != null && deviceProfile.Version != EditingProfile.Version && _conflictResolver != null)
            {
                var resolution = await _conflictResolver.ResolveAsync(EditingProfile, deviceProfile);
                if (resolution == null || resolution == ProfileConflictResolution.Cancel)
                {
                    StatusText = "Push cancelled.";
                    return;
                }
                if (resolution == ProfileConflictResolution.Pull)
                {
                    // Use device version: replace local and editing with device profile
                    EnsureKeys(deviceProfile);
                    EnsureLayerKeys(deviceProfile);
                    EnsureEncoders(deviceProfile);
                    var idx = Profiles.ToList().FindIndex(p => p.Id == deviceProfile.Id);
                    if (idx >= 0)
                        Profiles[idx] = deviceProfile;
                    else
                        Profiles.Add(deviceProfile);
                    SelectedProfile = deviceProfile;
                    EditingProfile = CloneProfile(deviceProfile);
                    _syncService.SaveProfileLocally(deviceProfile);
                    RefreshKeySlots();
                    RefreshComboList();
                    StatusText = $"Pulled '{deviceProfile.Name}' from device (replaced local).";
                    _hudService?.Show("Profile pulled", deviceProfile.Name);
                    return;
                }
                // resolution == Push: fall through to push
            }

            await Task.Delay(120);
            PushStepText = "Sending to device...";
            var ok = await _syncService.PushProfileToDeviceAsync(EditingProfile);
            PushStepText = ok ? "Verifying..." : "Failed";
            await Task.Delay(ok ? 150 : 100);
            if (ok)
            {
                PushStepText = "Done";
                StatusText = $"Pushed '{EditingProfile.Name}' to device";
                _syncService.SaveProfileLocally(EditingProfile);
                _hudService?.Show("Profile pushed", EditingProfile.Name);
                await Task.Delay(400);
            }
            else
            {
                StatusText = "Failed to push profile (check connection)";
            }
        }
        catch (Exception ex)
        {
            PushStepText = "Error";
            StatusText = $"Failed to push: {ex.Message}";
        }
        finally
        {
            IsPushInProgress = false;
            PushStepText = "";
        }
    }

    [RelayCommand]
    private void SaveLocally()
    {
        if (EditingProfile == null)
        {
            StatusText = "No profile selected";
            return;
        }

        try
        {
            _syncService.SaveProfileLocally(EditingProfile);
            StatusText = $"Saved '{EditingProfile.Name}' locally";
        }
        catch (Exception ex)
        {
            StatusText = $"Save failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CreateProfile()
    {
        var localIds = _syncService.GetLocalProfiles().Select(p => p.Id).ToHashSet();
        var listIds = Profiles.Select(p => p.Id).ToHashSet();
        int nextId = 0;
        for (int i = 0; i < 64; i++)
        {
            if (!localIds.Contains(i) && !listIds.Contains(i)) { nextId = i; break; }
            if (i == 63) { nextId = (listIds.Concat(localIds).DefaultIfEmpty(-1).Max()) + 1; break; }
        }
        var profile = new Profile { Id = nextId, Name = "New profile", Version = 1 };
        EnsureKeys(profile);
        EnsureLayerKeys(profile);
        EnsureEncoders(profile);
        _syncService.SaveProfileLocally(profile);
        Profiles.Add(profile);
        SelectedProfile = profile;
        EditingProfile = CloneProfile(profile);
        RefreshKeySlots();
        RefreshComboList();
        StatusText = $"Created profile '{profile.Name}'. Edit and push to device.";
    }

    [RelayCommand]
    private async Task PullFromDeviceAsync()
    {
        if (SelectedProfile == null)
        {
            StatusText = "Select a profile to pull.";
            return;
        }
        try
        {
            var full = await _syncService.PullProfileFromDeviceAsync(SelectedProfile.Id);
            if (full == null)
            {
                StatusText = "Failed to pull (check connection).";
                return;
            }
            EnsureKeys(full);
            EnsureLayerKeys(full);
            EnsureEncoders(full);
            _syncService.SaveProfileLocally(full);
            var idx = Profiles.ToList().FindIndex(p => p.Id == full.Id);
            if (idx >= 0) Profiles[idx] = full;
            else Profiles.Add(full);
            SelectedProfile = full;
            EditingProfile = CloneProfile(full);
            RefreshKeySlots();
            RefreshComboList();
            StatusText = $"Pulled '{full.Name}' from device and saved locally.";
            _hudService?.Show("Profile pulled", full.Name);
        }
        catch (Exception ex)
        {
            StatusText = $"Pull failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DuplicateProfile()
    {
        if (EditingProfile == null)
        {
            StatusText = "Select a profile to duplicate.";
            return;
        }
        var localIds = _syncService.GetLocalProfiles().Select(p => p.Id).ToHashSet();
        var listIds = Profiles.Select(p => p.Id).ToHashSet();
        int nextId = 0;
        for (int i = 0; i < 64; i++)
        {
            if (!localIds.Contains(i) && !listIds.Contains(i)) { nextId = i; break; }
            if (i == 63) { nextId = (listIds.Concat(localIds).DefaultIfEmpty(-1).Max()) + 1; break; }
        }
        var copy = CloneProfile(EditingProfile);
        copy.Id = nextId;
        copy.Name = "Copy of " + (EditingProfile.Name ?? "Unnamed");
        copy.Version = 1;
        _syncService.SaveProfileLocally(copy);
        Profiles.Add(copy);
        SelectedProfile = copy;
        EditingProfile = CloneProfile(copy);
        RefreshKeySlots();
        RefreshComboList();
        StatusText = $"Duplicated as '{copy.Name}'.";
    }

    [RelayCommand]
    private void RenameProfile()
    {
        if (EditingProfile == null) return;
        var dialog = new Dialogs.RenameProfileWindow(EditingProfile.Name ?? "Unnamed") { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;
        var newName = dialog.ProfileName;
        if (string.IsNullOrWhiteSpace(newName)) return;
        EditingProfile.Name = newName.Trim();
        var idx = Profiles.ToList().FindIndex(p => p.Id == EditingProfile.Id);
        if (idx >= 0) Profiles[idx] = EditingProfile;
        _syncService.SaveProfileLocally(EditingProfile);
        OnPropertyChanged(nameof(EditingProfile));
        StatusText = $"Renamed to '{EditingProfile.Name}'.";
    }

    [RelayCommand]
    private void DeleteProfileLocal()
    {
        if (SelectedProfile == null) return;
        if (MessageBox.Show($"Delete profile '{SelectedProfile.Name}' from this PC? (File only, not from device.)", "Delete local profile", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            var id = SelectedProfile.Id;
            _syncService.DeleteProfileLocally(id);
            Profiles.Remove(SelectedProfile);
            SelectedProfile = Profiles.FirstOrDefault();
            EditingProfile = SelectedProfile != null ? CloneProfile(SelectedProfile) : null;
            RefreshKeySlots();
            RefreshComboList();
            StatusText = "Profile deleted from PC.";
        }
        catch (Exception ex)
        {
            StatusText = $"Delete failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ImportProfile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Import Profile"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var json = System.IO.File.ReadAllText(dlg.FileName);
            var imported = Newtonsoft.Json.JsonConvert.DeserializeObject<Profile>(json);
            if (imported != null)
            {
                EnsureKeys(imported);
                _syncService.SaveProfileLocally(imported);
                if (!Profiles.Any(p => p.Id == imported.Id))
                    Profiles.Add(imported);
                SelectedProfile = imported;
                EditingProfile = CloneProfile(imported);
                RefreshKeySlots();
                StatusText = $"Imported '{imported.Name}'";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Import failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportProfile()
    {
        if (EditingProfile == null)
        {
            StatusText = "No profile to export";
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = $"{EditingProfile.Name.Replace(" ", "_")}.json",
            Title = "Export Profile"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            _syncService.ExportProfile(EditingProfile, dlg.FileName);
            StatusText = $"Exported to {dlg.FileName}";
        }
        catch (Exception ex)
        {
            StatusText = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ActivateProfileAsync()
    {
        if (SelectedProfile == null) return;

        try
        {
            StatusText = "Activating profile...";
            await _protocol.SetActiveProfileAsync(SelectedProfile.Id);
            ActiveProfileId = SelectedProfile.Id;
            StatusText = $"Activated profile: {SelectedProfile.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to activate profile: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddCombo()
    {
        if (EditingProfile == null) return;
        if (EditingProfile.Combos == null) EditingProfile.Combos = new List<ComboAssignment>();
        EditingProfile.Combos.Add(new ComboAssignment { Key1 = 0, Key2 = 1, Action = new KeyAction { Type = ActionType.None } });
        RefreshComboList();
        StatusText = "Combo added. Edit to set which two keys form the chord (firmware support may vary).";
    }

    [RelayCommand]
    private void RemoveCombo(ComboAssignment? combo)
    {
        if (EditingProfile?.Combos == null || combo == null) return;
        EditingProfile.Combos.Remove(combo);
        RefreshComboList();
    }

    /// <summary>Edit combo key indices (Key1, Key2). Returns true if user confirmed.</summary>
    public bool EditComboKeys(ComboAssignment combo, out int key1, out int key2)
    {
        key1 = combo.Key1;
        key2 = combo.Key2;
        var dialog = new Dialogs.ComboEditWindow(key1, key2) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true) return false;
        key1 = dialog.Key1Index;
        key2 = dialog.Key2Index;
        return true;
    }

    [RelayCommand]
    private void EditCombo(ComboAssignment? combo)
    {
        if (EditingProfile?.Combos == null || combo == null) return;
        if (!EditComboKeys(combo, out var k1, out var k2)) return;
        combo.Key1 = k1;
        combo.Key2 = k2;
        RefreshComboList();
        OnPropertyChanged(nameof(ComboList));
        StatusText = $"Combo set to K{k1 + 1}+K{k2 + 1}.";
    }

    [RelayCommand]
    private async Task DeleteFromDeviceAsync()
    {
        if (SelectedProfile == null) return;
        if (MessageBox.Show($"Delete profile '{SelectedProfile.Name}' from device?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            var ok = await _syncService.DeleteProfileFromDeviceAsync(SelectedProfile.Id);
            if (ok)
            {
                Profiles.Remove(SelectedProfile);
                SelectedProfile = Profiles.FirstOrDefault();
                EditingProfile = SelectedProfile != null ? CloneProfile(SelectedProfile) : null;
                RefreshKeySlots();
                RefreshComboList();
                StatusText = "Profile deleted from device. Click Refresh to reload.";
            }
            else
                StatusText = "Could not delete (e.g. active profile or last profile).";
        }
        catch (Exception ex)
        {
            StatusText = $"Delete failed: {ex.Message}";
        }
    }

    /// <summary>Build or refresh the Action Library (categories + macros). Call when opening Profiles or after macro change.</summary>
    public void BuildActionLibrary()
    {
        _allActionLibraryList = new List<ActionLibraryItem>
        {
            new() { Id = "act-none", Title = "None", Description = "Clear key", Category = "System", ActionType = ActionType.None },
            new() { Id = "act-hotkey", Title = "Hotkey", Description = "Keyboard shortcut (e.g. Ctrl+C)", Category = "Keyboard", ActionType = ActionType.Hotkey },
            new() { Id = "act-text", Title = "Text", Description = "Type text or Unicode", Category = "Text", ActionType = ActionType.Text },
            new() { Id = "act-media", Title = "Media", Description = "Volume, play/pause, next/prev", Category = "Media", ActionType = ActionType.Media },
            new() { Id = "act-mouse", Title = "Mouse", Description = "Click, scroll, move", Category = "Mouse", ActionType = ActionType.Mouse },
            new() { Id = "act-profile", Title = "Profile", Description = "Switch to another profile", Category = "Profile", ActionType = ActionType.Profile },
            new() { Id = "act-app", Title = "Launch App", Description = "Open an application", Category = "App", ActionType = ActionType.App, Badge = "Requires App" },
            new() { Id = "act-url", Title = "Open URL", Description = "Open a web link", Category = "App", ActionType = ActionType.Url }
        };
        foreach (var macro in _macroStorage.GetAllMacroAssets())
        {
            _allActionLibraryList.Add(new ActionLibraryItem
            {
                Id = "macro-" + (macro.MacroId ?? ""),
                Title = macro.Name ?? "Macro",
                Description = "Run macro",
                Category = "Macro",
                ActionType = ActionType.Macro,
                MacroId = macro.MacroId,
                IsFavorite = _favoriteActionIds.Contains("macro-" + (macro.MacroId ?? ""))
            });
        }
        RefreshFilteredActions();
    }

    private void RefreshFilteredActions()
    {
        var q = (ActionSearchText ?? "").Trim().ToLowerInvariant();
        var cat = SelectedActionCategory ?? "All";
        var filtered = _allActionLibraryList.Where(a =>
        {
            if (cat != "All" && cat != "Favorites" && a.Category != cat) return false;
            if (cat == "Favorites" && !a.IsFavorite) return false;
            if (string.IsNullOrEmpty(q)) return true;
            return (a.Title?.ToLowerInvariant().Contains(q) == true) || (a.Description?.ToLowerInvariant().Contains(q) == true);
        }).ToList();
        FilteredActionLibrary.Clear();
        foreach (var item in filtered)
            FilteredActionLibrary.Add(item);
        OnPropertyChanged(nameof(IsActionLibraryEmpty));
    }

    public bool IsActionLibraryEmpty => FilteredActionLibrary.Count == 0;

    /// <summary>Assign an action from the library to a key slot (e.g. from drag-drop).</summary>
    public void AssignActionToKey(int keyIndex, ActionLibraryItem item)
    {
        if (EditingProfile == null || keyIndex < 0 || keyIndex >= KeyCount) return;
        var config = CreateKeyConfigFromLibraryItem(keyIndex, item);
        List<KeyConfig>? source = SelectedLayerIndex switch
        {
            1 => EditingProfile.Layer1Keys,
            2 => EditingProfile.Layer2Keys,
            _ => EditingProfile.Keys
        };
        if (source == null) return;
        EnsureKeys(EditingProfile);
        EnsureLayerKeys(EditingProfile);
        source = SelectedLayerIndex switch { 1 => EditingProfile.Layer1Keys, 2 => EditingProfile.Layer2Keys, _ => EditingProfile.Keys };
        if (source == null) return;
        if (keyIndex < source.Count)
            source[keyIndex] = config;
        else
        {
            while (source.Count <= keyIndex) source.Add(new KeyConfig { Index = source.Count, Type = ActionType.None });
            source[keyIndex] = config;
        }
        config.Index = keyIndex;
        RefreshKeySlots();
        StatusText = $"Assigned {item.Title} to key {keyIndex + 1}. Click key to edit details.";
    }

    private static KeyConfig CreateKeyConfigFromLibraryItem(int index, ActionLibraryItem item)
    {
        var k = new KeyConfig { Index = index, Type = item.ActionType };
        switch (item.ActionType)
        {
            case ActionType.Hotkey: k.Modifiers = 0x01; k.Key = 0x06; break; // Ctrl+C
            case ActionType.Macro: k.MacroId = item.MacroId; break;
            case ActionType.Profile: k.ProfileId = 0; break;
            default: break;
        }
        return k;
    }

    [RelayCommand]
    private void ToggleActionFavorite(ActionLibraryItem? item)
    {
        if (item == null) return;
        item.IsFavorite = !item.IsFavorite;
        if (item.IsFavorite) _favoriteActionIds.Add(item.Id); else _favoriteActionIds.Remove(item.Id);
        RefreshFilteredActions();
    }

    /// <summary>Resolve action by id for drag-drop target.</summary>
    public ActionLibraryItem? GetActionById(string? id)
    {
        return string.IsNullOrEmpty(id) ? null : _allActionLibraryList.FirstOrDefault(a => a.Id == id);
    }
}
