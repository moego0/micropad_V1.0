using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Micropad.Core.Interfaces;
using Micropad.Services.Communication;

namespace Micropad.App.ViewModels;

public partial class DevicesViewModel : ObservableObject
{
    private readonly IDeviceConnection _connection;
    private readonly ProtocolHandler _protocol;
    private DeviceWatcher? _watcher;

    [ObservableProperty]
    private ObservableCollection<DeviceInformation> _devices = new();

    [ObservableProperty]
    private DeviceInformation? _selectedDevice;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusText = "Not scanning";

    [ObservableProperty]
    private string _deviceInfo = "Select a device to view info";

    public DevicesViewModel(IDeviceConnection connection, ProtocolHandler protocol)
    {
        _connection = connection;
        _protocol = protocol;
    }

    [RelayCommand]
    private async Task StartScanAsync()
    {
        Devices.Clear();
        IsScanning = true;
        StatusText = "Scanning for devices...";

        string selector = BluetoothLEDevice.GetDeviceSelector();
        _watcher = DeviceInformation.CreateWatcher(selector);

        _watcher.Added += OnDeviceAdded;
        _watcher.Updated += OnDeviceUpdated;
        _watcher.Stopped += OnWatcherStopped;

        _watcher.Start();

        // Stop after 10 seconds
        await Task.Delay(10000);
        StopScan();
    }

    private void StopScan()
    {
        if (_watcher != null)
        {
            _watcher.Stop();
            _watcher = null;
        }

        IsScanning = false;
        StatusText = $"Found {Devices.Count} device(s)";
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (SelectedDevice == null) return;

        try
        {
            StatusText = "Connecting...";
            await _connection.ConnectAsync(SelectedDevice.Id);

            // Get device info
            var deviceInfo = await _protocol.GetDeviceInfoAsync();
            if (deviceInfo != null)
            {
                DeviceInfo = $"ID: {deviceInfo.DeviceId}\nFW: {deviceInfo.FirmwareVersion}\nHW: {deviceInfo.HardwareVersion}\nBattery: {deviceInfo.BatteryLevel}%";
            }

            IsConnected = true;
            StatusText = "Connected";
        }
        catch (Exception ex)
        {
            StatusText = $"Connection failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        try
        {
            await _connection.DisconnectAsync();
            IsConnected = false;
            DeviceInfo = "Disconnected";
            StatusText = "Disconnected";
        }
        catch (Exception ex)
        {
            StatusText = $"Disconnect failed: {ex.Message}";
        }
    }

    private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation device)
    {
        // Filter for "Micropad" devices
        if (device.Name.Contains("Micropad", StringComparison.OrdinalIgnoreCase))
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (!Devices.Any(d => d.Id == device.Id))
                {
                    Devices.Add(device);
                }
            });
        }
    }

    private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate deviceUpdate)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var device = Devices.FirstOrDefault(d => d.Id == deviceUpdate.Id);
            if (device != null)
            {
                device.Update(deviceUpdate);
            }
        });
    }

    private void OnWatcherStopped(DeviceWatcher sender, object args)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            IsScanning = false;
        });
    }
}
