using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Micropad.App.Models;
using Micropad.App.ViewModels;

namespace Micropad.App.Views;

public partial class ProfilesView : Page
{
    public ProfilesView(ProfilesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void ActionCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not ActionLibraryItem item) return;
        var data = new DataObject("ActionLibraryItemId", item.Id);
        DragDrop.DoDragDrop(fe, data, DragDropEffects.Copy);
    }

    private void KeySlot_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData("ActionLibraryItemId") is not string id) return;
        if (sender is not FrameworkElement fe || fe.Tag is not string tagStr || !int.TryParse(tagStr, out var keyIndex)) return;
        if (DataContext is not ProfilesViewModel vm) return;
        var action = vm.GetActionById(id);
        if (action != null)
            vm.AssignActionToKey(keyIndex, action);
    }
}
