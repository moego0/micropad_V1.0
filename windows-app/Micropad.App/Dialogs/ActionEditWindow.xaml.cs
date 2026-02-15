using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Micropad.Core.Models;

namespace Micropad.App.Dialogs;

public partial class ActionEditWindow : Window
{
    private readonly KeyConfig _keyConfig;
    public KeyConfig Result { get; private set; } = null!;

    private static readonly Dictionary<string, int> KeyNameToCode = new()
    {
        {"A", 0x04}, {"B", 0x05}, {"C", 0x06}, {"D", 0x07}, {"E", 0x08}, {"F", 0x09},
        {"G", 0x0A}, {"H", 0x0B}, {"I", 0x0C}, {"J", 0x0D}, {"K", 0x0E}, {"L", 0x0F},
        {"M", 0x10}, {"N", 0x11}, {"O", 0x12}, {"P", 0x13}, {"Q", 0x14}, {"R", 0x15},
        {"S", 0x16}, {"T", 0x17}, {"U", 0x18}, {"V", 0x19}, {"W", 0x1A}, {"X", 0x1B},
        {"Y", 0x1C}, {"Z", 0x1D}, {"1", 0x1E}, {"2", 0x1F}, {"3", 0x20}, {"4", 0x21},
        {"5", 0x22}, {"6", 0x23}, {"7", 0x24}, {"8", 0x25}, {"9", 0x26}, {"0", 0x27},
        {"Enter", 0x28}, {"Esc", 0x29}, {"Backspace", 0x2A}, {"Tab", 0x2B},
        {"Space", 0x2C}, {"F1", 0x3A}, {"F2", 0x3B}, {"F3", 0x3C}, {"F4", 0x3D},
        {"F5", 0x3E}, {"F6", 0x3F}, {"F7", 0x40}, {"F8", 0x41}, {"F9", 0x42}, {"F10", 0x43},
        {"F11", 0x44}, {"F12", 0x45}, {"Insert", 0x49}, {"Delete", 0x4C}, {"Home", 0x4A},
        {"End", 0x4D}, {"PageUp", 0x4B}, {"PageDown", 0x4E}, {"Left", 0x50},
        {"Up", 0x52}, {"Right", 0x4F}, {"Down", 0x51}, {"Win", 0xE3}
    };

    public ActionEditWindow(KeyConfig keyConfig, int keyIndex)
    {
        InitializeComponent();
        _keyConfig = new KeyConfig
        {
            Index = keyIndex,
            Type = keyConfig.Type,
            Modifiers = keyConfig.Modifiers,
            Key = keyConfig.Key,
            Text = keyConfig.Text,
            Function = keyConfig.Function,
            Action = keyConfig.Action,
            Value = keyConfig.Value,
            ProfileId = keyConfig.ProfileId,
            AppPath = keyConfig.AppPath,
            Url = keyConfig.Url,
            MacroId = keyConfig.MacroId
        };

        LoadKeyList();
        LoadMediaList();
        LoadMouseList();
        TypeCombo.SelectionChanged += TypeCombo_SelectionChanged;
        LoadFromConfig();
    }

    private void LoadKeyList()
    {
        KeyCombo.Items.Clear();
        foreach (var k in KeyNameToCode.Keys.OrderBy(x => x))
        {
            KeyCombo.Items.Add(k);
        }
    }

    private void LoadMediaList()
    {
        MediaCombo.Items.Clear();
        foreach (MediaFunction f in Enum.GetValues(typeof(MediaFunction)))
        {
            MediaCombo.Items.Add(f.ToString());
        }
    }

    private void LoadMouseList()
    {
        MouseCombo.Items.Clear();
        foreach (MouseAction a in Enum.GetValues(typeof(MouseAction)))
        {
            MouseCombo.Items.Add(a.ToString());
        }
    }

    private void LoadFromConfig()
    {
        var typeName = _keyConfig.Type.ToString();
        for (int i = 0; i < TypeCombo.Items.Count; i++)
        {
            if (TypeCombo.Items[i] is ComboBoxItem item && item.Tag?.ToString() == typeName)
            {
                TypeCombo.SelectedIndex = i;
                break;
            }
        }

        ModCtrl.IsChecked = (_keyConfig.Modifiers & 0x01) != 0;
        ModShift.IsChecked = (_keyConfig.Modifiers & 0x02) != 0;
        ModAlt.IsChecked = (_keyConfig.Modifiers & 0x04) != 0;
        ModWin.IsChecked = (_keyConfig.Modifiers & 0x08) != 0;

        var keyName = KeyNameToCode.FirstOrDefault(k => k.Value == _keyConfig.Key).Key;
        if (keyName != null && KeyCombo.Items.Contains(keyName))
            KeyCombo.SelectedItem = keyName;

        TextInputBox.Text = _keyConfig.Text ?? "";
        if (_keyConfig.Function >= 0 && _keyConfig.Function < MediaCombo.Items.Count)
            MediaCombo.SelectedIndex = _keyConfig.Function;
        if (_keyConfig.Action >= 0 && _keyConfig.Action < MouseCombo.Items.Count)
            MouseCombo.SelectedIndex = _keyConfig.Action;
        ProfileIdInput.Text = _keyConfig.ProfileId.ToString();
        AppPathInput.Text = _keyConfig.AppPath ?? "";
        UrlInput.Text = _keyConfig.Url ?? "";
    }

    private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        HotkeyPanel.Visibility = Visibility.Collapsed;
        TextPanel.Visibility = Visibility.Collapsed;
        MediaPanel.Visibility = Visibility.Collapsed;
        MousePanel.Visibility = Visibility.Collapsed;
        ProfilePanel.Visibility = Visibility.Collapsed;
        AppPanel.Visibility = Visibility.Collapsed;
        UrlPanel.Visibility = Visibility.Collapsed;

        if (TypeCombo.SelectedItem is ComboBoxItem item)
        {
            var tag = item.Tag?.ToString() ?? "None";
            switch (tag)
            {
                case "Hotkey": HotkeyPanel.Visibility = Visibility.Visible; break;
                case "Text": TextPanel.Visibility = Visibility.Visible; break;
                case "Media": MediaPanel.Visibility = Visibility.Visible; break;
                case "Mouse": MousePanel.Visibility = Visibility.Visible; break;
                case "Profile": ProfilePanel.Visibility = Visibility.Visible; break;
                case "App": AppPanel.Visibility = Visibility.Visible; break;
                case "Url": UrlPanel.Visibility = Visibility.Visible; break;
            }
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (TypeCombo.SelectedItem is not ComboBoxItem item)
        {
            DialogResult = false;
            Close();
            return;
        }

        var tag = item.Tag?.ToString() ?? "None";
        _keyConfig.Type = tag switch
        {
            "Hotkey" => ActionType.Hotkey,
            "Text" => ActionType.Text,
            "Media" => ActionType.Media,
            "Mouse" => ActionType.Mouse,
            "Profile" => ActionType.Profile,
            "App" => ActionType.App,
            "Url" => ActionType.Url,
            _ => ActionType.None
        };

        _keyConfig.Modifiers = 0;
        if (ModCtrl.IsChecked == true) _keyConfig.Modifiers |= 0x01;
        if (ModShift.IsChecked == true) _keyConfig.Modifiers |= 0x02;
        if (ModAlt.IsChecked == true) _keyConfig.Modifiers |= 0x04;
        if (ModWin.IsChecked == true) _keyConfig.Modifiers |= 0x08;

        if (KeyCombo.SelectedItem is string keyStr && KeyNameToCode.TryGetValue(keyStr, out var keyCode))
            _keyConfig.Key = keyCode;
        _keyConfig.Text = TextInputBox.Text;
        if (MediaCombo.SelectedIndex >= 0)
            _keyConfig.Function = MediaCombo.SelectedIndex;
        if (MouseCombo.SelectedIndex >= 0)
            _keyConfig.Action = MouseCombo.SelectedIndex;
        if (int.TryParse(ProfileIdInput.Text, out var pid))
            _keyConfig.ProfileId = pid;
        _keyConfig.AppPath = AppPathInput.Text?.Trim();
        _keyConfig.Url = UrlInput.Text?.Trim();

        Result = _keyConfig;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
