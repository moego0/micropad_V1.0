using System.Windows.Controls;
using Micropad.App.ViewModels;

namespace Micropad.App.Views;

public partial class MacrosView : Page
{
    public MacrosView(MacrosViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
