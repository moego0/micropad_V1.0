using System.Windows.Controls;
using Micropad.App.ViewModels;

namespace Micropad.App.Views;

public partial class PresetsView : Page
{
    public PresetsView(PresetsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
