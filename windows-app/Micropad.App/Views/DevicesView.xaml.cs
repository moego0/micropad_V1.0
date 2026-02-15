using System.Windows.Controls;
using Micropad.App.ViewModels;

namespace Micropad.App.Views;

public partial class DevicesView : Page
{
    public DevicesView(DevicesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
