using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Micropad.Core.Models;
using Micropad.App.Dialogs;
using Micropad.Services.Communication;
using Micropad.Services;

namespace Micropad.App.ViewModels;

public partial class ProfilesViewModel : ObservableObject
{
    private const int KeyCount = 12;
    private readonly ProtocolHandler _protocol;
    private readonly ProfileSyncService _syncService;
    private readonly Micropad.Services.Storage.LocalMacroStorage _macroStorage;

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

    public ProfilesViewModel(ProtocolHandler protocol, ProfileSyncService syncService, Micropad.Services.Storage.LocalMacroStorage macroStorage)
    {
        _protocol = protocol;
        _syncService = syncService;
        _macroStorage = macroStorage;
    }

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

    [RelayCommand]
    private async Task LoadProfilesAsync()
    {
        try
        {
            StatusText = "Loading profiles...";

            var profileList = await _protocol.ListProfilesAsync();

            Profiles.Clear();
            if (profileList != null)
            {
                foreach (var profile in profileList)
                {
                    Profiles.Add(profile);
                }
            }

            StatusText = $"Loaded {Profiles.Count} profile(s)";
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to load profiles: {ex.Message}";
        }
    }

    partial void OnSelectedProfileChanged(Profile? value)
    {
        if (value != null)
        {
            _ = LoadProfileDetailsAsync();
        }
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
                SelectedProfile = fullProfile;
                EditingProfile = CloneProfile(fullProfile);
                RefreshKeySlots();
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
                MacroId = k.MacroId
            }).ToList(),
            Encoders = p.Encoders.Select(e => new EncoderConfig
            {
                Index = e.Index,
                Acceleration = e.Acceleration,
                StepsPerDetent = e.StepsPerDetent
            }).ToList()
        };
    }

    private void RefreshKeySlots()
    {
        KeySlots.Clear();
        if (EditingProfile != null)
        {
            EnsureKeys(EditingProfile);
            for (int i = 0; i < KeyCount; i++)
            {
                KeySlots.Add(i < EditingProfile.Keys.Count ? EditingProfile.Keys[i] : new KeyConfig { Index = i, Type = ActionType.None });
            }
        }
        else
        {
            for (int i = 0; i < KeyCount; i++)
            {
                KeySlots.Add(new KeyConfig { Index = i, Type = ActionType.None });
            }
        }
    }

    [RelayCommand]
    private void EditKey(object? parameter)
    {
        var index = parameter is int i ? i : (parameter is string s && int.TryParse(s, out var parsed) ? parsed : -1);
        if (EditingProfile == null || index < 0 || index >= KeySlots.Count) return;

        var keyConfig = KeySlots[index];
        var macroNames = _macroStorage.GetMacroNames();
        var dialog = new ActionEditWindow(keyConfig, index, macroNames)
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
            OnPropertyChanged(nameof(KeySlots));
            StatusText = "Key updated. Push to device to save.";
        }
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
            StatusText = "Pushing profile to device...";
            var ok = await _syncService.PushProfileToDeviceAsync(EditingProfile);
            if (ok)
            {
                StatusText = $"Pushed '{EditingProfile.Name}' to device";
                _syncService.SaveProfileLocally(EditingProfile);
            }
            else
            {
                StatusText = "Failed to push profile (check connection)";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to push: {ex.Message}";
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
            StatusText = $"Activated profile: {SelectedProfile.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to activate profile: {ex.Message}";
        }
    }
}
