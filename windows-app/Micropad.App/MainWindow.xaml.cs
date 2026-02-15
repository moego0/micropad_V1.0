using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Micropad.App.ViewModels;
using Micropad.App.Views;

namespace Micropad.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        DataContext = _viewModel;
        
        // Navigate to devices by default
        Loaded += (s, e) =>
        {
            NavigationList.SelectedIndex = 0;
            NavigateToPage("Devices");
        };
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

    private void NavigateToPage(string pageName)
    {
        Page? page = pageName switch
        {
            "Devices" => CreatePage<DevicesView>(),
            "Profiles" => CreatePage<ProfilesView>(),
            "Macros" => CreatePage<MacrosView>(),
            "Stats" => CreatePage<StatsView>(),
            "Settings" => CreatePage<SettingsView>(),
            _ => null
        };

        if (page != null)
        {
            ContentFrame.Navigate(page);
        }
    }

    private Page? CreatePage<T>() where T : Page
    {
        return _serviceProvider.GetService<T>() ?? Activator.CreateInstance<T>();
    }
}
