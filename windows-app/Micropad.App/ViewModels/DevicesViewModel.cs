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
    private DeviceWatcher? _watcherPaired;
    private DeviceWatcher? _watcherUnpaired;

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

    private readonly MainViewModel _mainViewModel;

    public DevicesViewModel(IDeviceConnection connection, ProtocolHandler protocol, MainViewModel mainViewModel)
    {
        _connection = connection;
        _protocol = protocol;
        _mainViewModel = mainViewModel;
    }

    [RelayCommand]
    private async Task StartScanAsync()
    {
        Devices.Clear();
        IsScanning = true;
        StatusText = "Scanning for Micropad (paired and unpaired)...";

        // Watch BOTH paired and unpaired BLE devices so first-time use works
        string selectorPaired = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
        string selectorUnpaired = BluetoothLEDevice.GetDeviceSelectorFromPairingState(false);

        _watcherPaired = DeviceInformation.CreateWatcher(selectorPaired);
        _watcherUnpaired = DeviceInformation.CreateWatcher(selectorUnpaired);

        _watcherPaired.Added += OnDeviceAdded;
        _watcherPaired.Updated += OnDeviceUpdated;
        _watcherPaired.Stopped += OnWatcherStopped;
        _watcherUnpaired.Added += OnDeviceAdded;
        _watcherUnpaired.Updated += OnDeviceUpdated;
        _watcherUnpaired.Stopped += OnWatcherStopped;

        _watcherPaired.Start();
        _watcherUnpaired.Start();

        // Stop after 15 seconds to give unpaired devices time to appear
        await Task.Delay(15000);
        StopScan();
    }

    private void StopScan()
    {
        _watcherPaired?.Stop();
        _watcherPaired = null;
        _watcherUnpaired?.Stop();
        _watcherUnpaired = null;

        IsScanning = false;
        StatusText = $"Found {Devices.Count} device(s). Select one and click Connect.";
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
                _mainViewModel.SetBatteryLevel(deviceInfo.BatteryLevel);
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
        // Include devices named Micropad or ESP32 (advertised name; works for paired and unpaired)
        bool isMicropad = (device.Name?.Contains("Micropad", StringComparison.OrdinalIgnoreCase) ?? false) ||
                          (device.Name?.Contains("ESP32", StringComparison.OrdinalIgnoreCase) ?? false);

        if (isMicropad)
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
            if (_watcherPaired == null && _watcherUnpaired == null)
                IsScanning = false;
        });
    }
}
