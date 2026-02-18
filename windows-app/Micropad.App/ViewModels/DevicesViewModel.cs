using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Micropad.Core.Interfaces;
using Micropad.Core.Models;
using Micropad.Services.Communication;

namespace Micropad.App.ViewModels;

public partial class DevicesViewModel : ObservableObject
{
    /// <summary>Micropad Config Service UUID - used to filter BLE advertisements so we only discover our device (works even if name is blank or changes).</summary>
    private static readonly Guid ConfigServiceUuid = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b");

    private readonly IDeviceConnection _connection;
    private readonly ProtocolHandler _protocol;
    private readonly Micropad.Services.Storage.SettingsStorage _settingsStorage;
    private BluetoothLEAdvertisementWatcher? _watcher;

    [ObservableProperty]
    private ObservableCollection<BleDiscoveredDevice> _devices = new();

    [ObservableProperty]
    private BleDiscoveredDevice? _selectedDevice;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusText = "Not scanning";

    [ObservableProperty]
    private string _deviceInfo = "Select a device to view info";

    private readonly MainViewModel _mainViewModel;

    public DevicesViewModel(IDeviceConnection connection, ProtocolHandler protocol, MainViewModel mainViewModel, Micropad.Services.Storage.SettingsStorage settingsStorage)
    {
        _connection = connection;
        _protocol = protocol;
        _mainViewModel = mainViewModel;
        _settingsStorage = settingsStorage;
        _connection.Disconnected += (_, _) =>
        {
            App.Current.Dispatcher.Invoke(() => IsConnected = false);
        };
        _connection.Connected += (_, _) =>
        {
            App.Current.Dispatcher.Invoke(() => IsConnected = true);
        };
        // Reflect actual connection state (e.g. already connected from startup auto-connect)
        IsConnected = _connection.IsConnected;
        _ = LoadPairedDevicesAsync();
    }

    /// <summary>Load already-paired BLE devices so Micropad appears even when connected to PC (not advertising).</summary>
    private async Task LoadPairedDevicesAsync()
    {
        try
        {
            var selector = BluetoothLEDevice.GetDeviceSelector();
            var paired = await DeviceInformation.FindAllAsync(selector);
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var info in paired)
                {
                    var name = info.Name ?? string.Empty;
                    if (!name.Contains("Micropad", StringComparison.OrdinalIgnoreCase)) continue;
                    if (Devices.Any(d => d.DeviceId == info.Id)) continue;
                    Devices.Add(new BleDiscoveredDevice
                    {
                        Name = string.IsNullOrEmpty(name) ? "Micropad" : name,
                        DeviceId = info.Id,
                        BluetoothAddress = 0,
                        Rssi = 0
                    });
                }
            });
        }
        catch
        {
            // Ignore (e.g. no Bluetooth, permission)
        }
    }

    [RelayCommand]
    private async Task StartScanAsync()
    {
        Devices.Clear();
        await LoadPairedDevicesAsync();
        IsScanning = true;
        StatusText = "Scanning for Micropad (devices with Config service)...";

        // Filter by Micropad Config Service UUID so we find the device even with blank or changed name
        var filter = new BluetoothLEAdvertisementFilter();
        filter.Advertisement.ServiceUuids.Add(ConfigServiceUuid);
        _watcher = new BluetoothLEAdvertisementWatcher(filter)
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };

        _watcher.Received += OnAdvertisementReceived;
        _watcher.Stopped += OnWatcherStopped;
        _watcher.Start();

        // Stop after 15 seconds
        await Task.Delay(15000);
        StopScan();
    }

    private void StopScan()
    {
        _watcher?.Stop();
        _watcher = null;
        IsScanning = false;
        StatusText = $"Found {Devices.Count} device(s). Select one and click Connect.";
    }

    private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        var name = args.Advertisement.LocalName ?? "(Micropad)";
        App.Current.Dispatcher.Invoke(() =>
        {
            var existing = Devices.FirstOrDefault(d => d.BluetoothAddress == args.BluetoothAddress);
            if (existing != null)
            {
                existing.Name = name;
                existing.Rssi = args.RawSignalStrengthInDBm;
                OnPropertyChanged(nameof(Devices));
                return;
            }
            // Avoid duplicate paired entry: same name Micropad might already be in list from LoadPairedDevices
            var byName = Devices.FirstOrDefault(d => d.Name.Contains("Micropad", StringComparison.OrdinalIgnoreCase) && d.DeviceId != null);
            if (byName != null)
            {
                byName.BluetoothAddress = args.BluetoothAddress;
                byName.Rssi = args.RawSignalStrengthInDBm;
                byName.Name = name;
                OnPropertyChanged(nameof(Devices));
                return;
            }
            Devices.Add(new BleDiscoveredDevice
            {
                BluetoothAddress = args.BluetoothAddress,
                Name = name,
                Rssi = args.RawSignalStrengthInDBm
            });
        });
    }

    private void OnWatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (_watcher == null)
                IsScanning = false;
        });
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (SelectedDevice == null) return;

        const int maxRetries = 3;
        int[] backoffMs = { 0, 1000, 2000 };

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                    StatusText = $"Retrying in {(backoffMs[attempt] / 1000)}s... (attempt {attempt + 1}/{maxRetries})";
                else
                    StatusText = "Connecting...";
                if (backoffMs[attempt] > 0)
                    await Task.Delay(backoffMs[attempt]);

                string deviceId;
                if (!string.IsNullOrEmpty(SelectedDevice.DeviceId))
                {
                    deviceId = SelectedDevice.DeviceId;
                }
                else
                {
                    var device = await BluetoothLEDevice.FromBluetoothAddressAsync(SelectedDevice.BluetoothAddress);
                    if (device == null)
                    {
                        throw new InvalidOperationException("Could not open device from address. Remove from Bluetooth settings, power-cycle Micropad, then try again.");
                    }
                    deviceId = device.DeviceInformation.Id;
                    device.Dispose();
                }

                await _connection.ConnectAsync(deviceId);

                var deviceInfo = await _protocol.GetDeviceInfoAsync();
                if (deviceInfo != null)
                {
                    DeviceInfo = $"ID: {deviceInfo.DeviceId}\nFW: {deviceInfo.FirmwareVersion}\nHW: {deviceInfo.HardwareVersion}\nBattery: {deviceInfo.BatteryLevel}%";
                    _mainViewModel.SetBatteryLevel(deviceInfo.BatteryLevel);
                }

                IsConnected = true;
                StatusText = "Connected";
                var settings = _settingsStorage.Load();
                settings.LastDeviceId = deviceId;
                _settingsStorage.Save(settings);
                return;
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} ({ex.InnerException.Message})" : ex.Message;
                StatusText = attempt < maxRetries - 1 ? $"Connection failed: {msg}" : $"Connection failed: {msg}";
            }
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
}
