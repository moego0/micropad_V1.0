using System;
using System.Windows;
using System.Windows.Threading;

namespace Micropad.App.Views;

public partial class HudOverlayWindow : Window
{
    private DispatcherTimer? _hideTimer;

    public HudOverlayWindow()
    {
        InitializeComponent();
        PositionTopRight();
    }

    private void PositionTopRight()
    {
        var screen = System.Windows.Forms.Screen.PrimaryScreen?.WorkingArea ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);
        Left = screen.Right - Width - 24;
        Top = screen.Top + 24;
    }

    public void ShowHud(string text, int displayMs = 1500)
    {
        HudText.Text = text;
        PositionTopRight();
        Show();
        Activate();
        _hideTimer?.Stop();
        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(displayMs) };
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer?.Stop();
            Hide();
        };
        _hideTimer.Start();
    }
}
