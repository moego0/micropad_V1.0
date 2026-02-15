using System;
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

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private string _batteryLevel = "N/A";

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
        ConnectionStatus = "Connected";
        StatusText = $"Connected to {_connection.DeviceName}";
        _foregroundMonitor.Start();
    }

    private void OnDisconnected(object? sender, EventArgs e)
    {
        ConnectionStatus = "Disconnected";
        StatusText = "Device disconnected";
        BatteryLevel = "N/A";
        _foregroundMonitor.Stop();
    }
}
