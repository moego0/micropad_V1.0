using System.Windows;
using Micropad.App.Models;

namespace Micropad.App.Dialogs;

public partial class ProfileConflictWindow : Window
{
    public ProfileConflictResolution? Result { get; private set; }

    public ProfileConflictWindow(string profileName, int localVersion, int deviceVersion)
    {
        InitializeComponent();
        TitleText.Text = $"Profile \"{profileName}\" has different versions";
        MessageText.Text = $"PC has version {localVersion}, device has version {deviceVersion}. Choose how to resolve:";
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = ProfileConflictResolution.Cancel;
        DialogResult = false;
        Close();
    }

    private void Pull_Click(object sender, RoutedEventArgs e)
    {
        Result = ProfileConflictResolution.Pull;
        DialogResult = true;
        Close();
    }

    private void Push_Click(object sender, RoutedEventArgs e)
    {
        Result = ProfileConflictResolution.Push;
        DialogResult = true;
        Close();
    }
}
