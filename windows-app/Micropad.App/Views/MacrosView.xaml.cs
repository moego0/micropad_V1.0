using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Micropad.App.ViewModels;
using Micropad.Core;
using Micropad.Core.Models;

namespace Micropad.App.Views;

public partial class MacrosView : Page
{
    private string? _tagDragTag;
    private bool _tagDragStarted;
    private Point _tagDragStart;

    public MacrosView(MacrosViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Slot_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement el || DataContext is not MacrosViewModel vm) return;
        if (el.Tag is string s && int.TryParse(s, out int index) && vm.SelectSlotCommand.CanExecute(index))
            vm.SelectSlotCommand.Execute(index);
    }

    private void Slot_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(MacroTagCatalog.DataFormat))
            e.Effects = DragDropEffects.Copy;
        else
            e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void Slot_Drop(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement el || DataContext is not MacrosViewModel vm) return;
        if (!e.Data.GetDataPresent(MacroTagCatalog.DataFormat)) return;
        var tag = e.Data.GetData(MacroTagCatalog.DataFormat) as string;
        if (string.IsNullOrEmpty(tag)) return;
        if (el.Tag is string s && int.TryParse(s, out int index))
            vm.DropTagOnSlot(index, tag);
        e.Handled = true;
    }

    private void Tag_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement el || el.DataContext is not MacroTag mt) return;
        _tagDragTag = mt.Tag;
        _tagDragStarted = false;
        _tagDragStart = e.GetPosition(null);
        el.CaptureMouse();
    }

    private void Tag_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement el || string.IsNullOrEmpty(_tagDragTag) || !el.IsMouseCaptured) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var pos = e.GetPosition(null);
        if (Vector.Subtract(new Vector(pos.X, pos.Y), new Vector(_tagDragStart.X, _tagDragStart.Y)).Length < 6)
            return;
        _tagDragStarted = true;
        el.ReleaseMouseCapture();
        try
        {
            var data = new DataObject(MacroTagCatalog.DataFormat, _tagDragTag);
            DragDrop.DoDragDrop(el, data, DragDropEffects.Copy);
        }
        finally
        {
            _tagDragTag = null;
        }
    }

    private void Tag_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement el) return;
        if (el.IsMouseCaptured)
            el.ReleaseMouseCapture();
        if (!_tagDragStarted && _tagDragTag != null && DataContext is MacrosViewModel vm && vm.AppendTagCommand.CanExecute(_tagDragTag))
            vm.AppendTagCommand.Execute(_tagDragTag);
        _tagDragTag = null;
        _tagDragStarted = false;
    }
}
