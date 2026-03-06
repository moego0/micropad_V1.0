using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Micropad.App.Services;

namespace Micropad.App.Views;

public partial class CommandPaletteWindow : Window
{
    private readonly CommandPaletteService _service;
    private List<CommandPaletteEntry> _filtered = new();

    public CommandPaletteWindow(CommandPaletteService service)
    {
        _service = service;
        InitializeComponent();
        SearchBox.TextChanged += (_, _) => RefreshList();
        Loaded += (_, _) =>
        {
            SearchBox.Focus();
            RefreshList();
        };
    }

    private void RefreshList()
    {
        _filtered = _service.Search(SearchBox.Text ?? "").ToList();
        ResultsList.ItemsSource = _filtered;
        if (_filtered.Count > 0)
            ResultsList.SelectedIndex = 0;
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Down)
        {
            if (_filtered.Count > 0)
                ResultsList.SelectedIndex = (ResultsList.SelectedIndex + 1) % _filtered.Count;
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Up)
        {
            if (_filtered.Count > 0)
            {
                var i = ResultsList.SelectedIndex - 1;
                ResultsList.SelectedIndex = i < 0 ? _filtered.Count - 1 : i;
            }
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Enter)
        {
            RunSelected();
            e.Handled = true;
        }
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is CommandPaletteEntry)
            ResultsList.ScrollIntoView(ResultsList.SelectedItem);
    }

    private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        RunSelected();
    }

    private void RunSelected()
    {
        if (ResultsList.SelectedItem is CommandPaletteEntry entry)
        {
            _service.Execute(entry);
            Close();
        }
    }
}
