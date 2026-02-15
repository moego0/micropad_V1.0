using System.Windows.Controls;
using Micropad.App.ViewModels;

namespace Micropad.App.Views;

public partial class SettingsView : Page
{
    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
