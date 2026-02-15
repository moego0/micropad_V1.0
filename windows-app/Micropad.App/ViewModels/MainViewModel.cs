using System;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Micropad.Core.Interfaces;
using Micropad.Services.Communication;
using Micropad.Services.Automation;

namespace Micropad.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDeviceConnection _connection;
    private readonly ProtocolHandler _protocol;
    private readonly ForegroundMonitor _foregroundMonitor;

    private static readonly SolidColorBrush ConnectedBrush = new(Color.FromRgb(0x10, 0xB9, 0x81));
    private static readonly SolidColorBrush DisconnectedBrush = new(Color.FromRgb(0x6E, 0x6E, 0x6E));
    private static readonly SolidColorBrush BatteryGoodBrush = new(Color.FromRgb(0x10, 0xB9, 0x81));
    private static readonly SolidColorBrush BatteryLowBrush = new(Color.FromRgb(0xF5, 0x9E, 0x0B));
    private static readonly SolidColorBrush BatteryCriticalBrush = new(Color.FromRgb(0xEF, 0x44, 0x44));

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private string _batteryLevel = "N/A";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private Brush _connectionColor = DisconnectedBrush;

    [ObservableProperty]
    private Brush _batteryColor = DisconnectedBrush;

    [ObservableProperty]
    private string _lastSyncTime = "Never";

    public MainViewModel(IDeviceConnection connection, ProtocolHandler protocol, ForegroundMonitor foregroundMonitor)
    {
        _connection = connection;
        _protocol = protocol;
        _foregroundMonitor = foregroundMonitor;

        _connection.Connected += OnConnected;
        _connection.Disconnected += OnDisconnected;
        _foregroundMonitor.ProfileSwitchRequested += OnProfileSwitchRequested;
    }

    private async void OnProfileSwitchRequested(object? sender, int profileId)
    {
        if (!_connection.IsConnected) return;
        try
        {
            await _protocol.SetActiveProfileAsync(profileId);
        }
        catch
        {
            // Ignore
        }
    }

    private void OnConnected(object? sender, EventArgs e)
    {
        IsConnected = true;
        ConnectionStatus = "Connected";
        ConnectionColor = ConnectedBrush;
        StatusText = $"Connected to {_connection.DeviceName}";
        _foregroundMonitor.Start();
        UpdateBatteryColor();
    }

    private void OnDisconnected(object? sender, EventArgs e)
    {
        IsConnected = false;
        ConnectionStatus = "Disconnected";
        ConnectionColor = DisconnectedBrush;
        StatusText = "Device disconnected";
        BatteryLevel = "N/A";
        BatteryColor = DisconnectedBrush;
        _foregroundMonitor.Stop();
    }

    private void UpdateBatteryColor()
    {
        if (BatteryLevel == "N/A" || !int.TryParse(BatteryLevel.TrimEnd('%'), out var pct))
        {
            BatteryColor = DisconnectedBrush;
            return;
        }
        BatteryColor = pct > 20 ? BatteryGoodBrush : (pct > 10 ? BatteryLowBrush : BatteryCriticalBrush);
    }

    public void SetBatteryLevel(int percent)
    {
        BatteryLevel = $"{percent}%";
        UpdateBatteryColor();
    }

    public void SetLastSync(string value)
    {
        LastSyncTime = value;
    }
}
