using System.Windows;

namespace Micropad.App.Dialogs;

public partial class RenameProfileWindow : Window
{
    public string ProfileName { get; private set; } = "";

    public RenameProfileWindow(string currentName)
    {
        InitializeComponent();
        NameBox.Text = currentName ?? "";
        NameBox.SelectAll();
        NameBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        ProfileName = (NameBox.Text ?? "").Trim();
        DialogResult = true;
        Close();
    }
}
