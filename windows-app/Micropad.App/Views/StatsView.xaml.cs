using System.Windows.Controls;
using Micropad.App.ViewModels;

namespace Micropad.App.Views;

public partial class StatsView : Page
{
    public StatsView(StatsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
