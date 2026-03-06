using System.Windows;
using System.Windows.Controls;

namespace Micropad.App.Dialogs;

public partial class ComboEditWindow : Window
{
    public int Key1Index { get; private set; }
    public int Key2Index { get; private set; }

    public ComboEditWindow(int key1Index, int key2Index)
    {
        InitializeComponent();
        Key1Index = key1Index;
        Key2Index = key2Index;
        Loaded += (_, _) =>
        {
            for (int i = 0; i < 12; i++)
            {
                Key1Combo.Items.Add($"K{i + 1}");
                Key2Combo.Items.Add($"K{i + 1}");
            }
            Key1Combo.SelectedIndex = key1Index >= 0 && key1Index < 12 ? key1Index : 0;
            Key2Combo.SelectedIndex = key2Index >= 0 && key2Index < 12 ? key2Index : 1;
        };
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Key1Index = Key1Combo.SelectedIndex >= 0 ? Key1Combo.SelectedIndex : 0;
        Key2Index = Key2Combo.SelectedIndex >= 0 ? Key2Combo.SelectedIndex : 1;
        DialogResult = true;
        Close();
    }
}
