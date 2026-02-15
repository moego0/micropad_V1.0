using System.Windows.Controls;
using Micropad.App.ViewModels;

namespace Micropad.App.Views;

public partial class ProfilesView : Page
{
    public ProfilesView(ProfilesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
