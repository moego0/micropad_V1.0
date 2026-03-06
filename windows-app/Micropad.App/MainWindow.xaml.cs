using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Micropad.App.Services;
using Micropad.App.ViewModels;
using Micropad.App.Views;
using Micropad.Services.Storage;
using Wpf.Ui.Controls;

namespace Micropad.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private readonly Micropad.Services.TrayService? _trayService;
    private readonly CommandPaletteService? _commandPalette;
    private readonly SettingsStorage? _settingsStorage;

    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider, Micropad.Services.TrayService? trayService = null, CommandPaletteService? commandPalette = null, SettingsStorage? settingsStorage = null)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        _trayService = trayService;
        _commandPalette = commandPalette;
        _settingsStorage = settingsStorage;
        DataContext = _viewModel;

        Closing += MainWindow_Closing;

        Loaded += (s, e) =>
        {
            NavigationList.SelectedIndex = 0;
            NavigateToPage("Devices");
            RegisterCommandPalette();
            ApplyBackdrop();
            if (_settingsStorage != null)
                _settingsStorage.SettingsSaved += (_, _) => ApplyBackdrop();
        };

        KeyDown += MainWindow_KeyDown;
    }

    private void ApplyBackdrop()
    {
        try
        {
            WindowBackdrop.ApplyBackdrop(this, WindowBackdropType.Mica);
        }
        catch { /* fallback to solid */ }
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            OpenCommandPalette();
        }
    }

    private void RegisterCommandPalette()
    {
        if (_commandPalette == null) return;
        _commandPalette.Register(new CommandPaletteEntry { Id = "nav-devices", Title = "Go to Devices", Category = "Navigation", Action = () => NavigateToPage("Devices") });
        _commandPalette.Register(new CommandPaletteEntry { Id = "nav-profiles", Title = "Go to Profiles", Category = "Navigation", Action = () => NavigateToPage("Profiles") });
        _commandPalette.Register(new CommandPaletteEntry { Id = "nav-templates", Title = "Go to Templates", Category = "Navigation", Action = () => NavigateToPage("Templates") });
        _commandPalette.Register(new CommandPaletteEntry { Id = "nav-macros", Title = "Go to Macros", Category = "Navigation", Action = () => NavigateToPage("Macros") });
        _commandPalette.Register(new CommandPaletteEntry { Id = "nav-stats", Title = "Go to Stats", Category = "Navigation", Action = () => NavigateToPage("Stats") });
        _commandPalette.Register(new CommandPaletteEntry { Id = "nav-settings", Title = "Go to Settings", Category = "Navigation", Action = () => NavigateToPage("Settings") });
    }

    private void OpenCommandPalette()
    {
        if (_commandPalette == null) return;
        var win = new CommandPaletteWindow(_commandPalette) { Owner = this };
        win.Show();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_trayService != null && _trayService.MinimizeToTrayEnabled)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavigationList.SelectedItem is ListBoxItem item)
        {
            var content = item.Content?.ToString();
            if (content != null)
            {
                NavigateToPage(content);
            }
        }
    }

    public void NavigateToPage(string pageName)
    {
        var index = pageName switch
        {
            "Devices" => 0,
            "Profiles" => 1,
            "Templates" => 2,
            "Macros" => 3,
            "Stats" => 4,
            "Settings" => 5,
            _ => -1
        };
        if (index >= 0 && NavigationList.Items.Count > index)
            NavigationList.SelectedIndex = index;

        Page? page = pageName switch
        {
            "Devices" => CreatePage<DevicesView>(),
            "Profiles" => CreatePage<ProfilesView>(),
            "Templates" => CreatePage<PresetsView>(),
            "Macros" => CreatePage<MacrosView>(),
            "Stats" => CreatePage<StatsView>(),
            "Settings" => CreatePage<SettingsView>(),
            _ => null
        };

        if (page != null)
            ContentFrame.Navigate(page);
    }

    private Page? CreatePage<T>() where T : Page
    {
        return _serviceProvider.GetService<T>() ?? Activator.CreateInstance<T>();
    }
}
