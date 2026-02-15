using System.Diagnostics;
using System.Linq;
using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Micropad.Core.Interfaces;

namespace Micropad.Services.Communication;

public class BleConnection : IDeviceConnection
{
    private BluetoothLEDevice? _device;
    private GattDeviceService? _configService;
    private GattCharacteristic? _cmdChar;
    private GattCharacteristic? _evtChar;

    private readonly Guid _configServiceUuid = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
    private readonly Guid _cmdCharUuid = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914c");
    private readonly Guid _evtCharUuid = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914d");

    public bool IsConnected => _device != null && _device.ConnectionStatus == BluetoothConnectionStatus.Connected;
    public string DeviceName => _device?.Name ?? string.Empty;

    public event EventHandler<string>? MessageReceived;
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public async Task<bool> ConnectAsync(string deviceId)
    {
        _device = await BluetoothLEDevice.FromIdAsync(deviceId);
        if (_device == null)
        {
            throw new InvalidOperationException("Could not open the device. Ensure Bluetooth is on and the Micropad is in range.");
        }

        // First-time connection: pair if not already paired (no PIN needed with ProtectionLevel.None)
        if (!_device.DeviceInformation.Pairing.IsPaired)
        {
            var pairingResult = await _device.DeviceInformation.Pairing.PairAsync(
                DevicePairingProtectionLevel.None);
            if (pairingResult.Status != DevicePairingResultStatus.Paired &&
                pairingResult.Status != DevicePairingResultStatus.AlreadyPaired)
            {
                throw new InvalidOperationException(
                    $"Pairing failed: {pairingResult.Status}. Accept any pairing prompt on the PC or try again.");
            }
        }

        var servicesResult = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
        if (servicesResult.Status != GattCommunicationStatus.Success)
        {
            throw new InvalidOperationException(
                $"GATT error: {servicesResult.Status}. Ensure the Micropad firmware is running and supports the config service.");
        }
        _configService = servicesResult.Services.FirstOrDefault(s => s.Uuid == _configServiceUuid);
        if (_configService == null)
        {
            throw new InvalidOperationException("Device does not expose the Micropad config service. Check firmware.");
        }

        var cmdResult = await _configService.GetCharacteristicsForUuidAsync(_cmdCharUuid);
        if (cmdResult.Status != GattCommunicationStatus.Success || cmdResult.Characteristics.Count == 0)
        {
            throw new InvalidOperationException("Config command characteristic not found. Check firmware.");
        }
        _cmdChar = cmdResult.Characteristics[0];

        var evtResult = await _configService.GetCharacteristicsForUuidAsync(_evtCharUuid);
        if (evtResult.Status != GattCommunicationStatus.Success || evtResult.Characteristics.Count == 0)
        {
            throw new InvalidOperationException("Config event characteristic not found. Check firmware.");
        }
        _evtChar = evtResult.Characteristics[0];

        var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
        await _evtChar.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue);
        _evtChar.ValueChanged += OnValueChanged;

        _device.ConnectionStatusChanged += OnConnectionStatusChanged;

        Connected?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public async Task DisconnectAsync()
    {
        if (_evtChar != null)
        {
            _evtChar.ValueChanged -= OnValueChanged;
        }

        if (_device != null)
        {
            _device.ConnectionStatusChanged -= OnConnectionStatusChanged;
            _device.Dispose();
            _device = null;
        }

        _configService?.Dispose();
        _configService = null;
        _cmdChar = null;
        _evtChar = null;

        Disconnected?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    public async Task SendMessageAsync(string json)
    {
        if (_cmdChar == null || !IsConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        var bytes = Encoding.UTF8.GetBytes(json);

        // Check if chunking needed
        if (bytes.Length > 512)
        {
            await SendChunkedAsync(json);
        }
        else
        {
            var writer = new DataWriter();
            writer.WriteBytes(bytes);
            await _cmdChar.WriteValueAsync(writer.DetachBuffer());
        }
    }

    private async Task SendChunkedAsync(string message)
    {
        const int chunkSize = 480;
        var totalChunks = (message.Length + chunkSize - 1) / chunkSize;

        for (int i = 0; i < totalChunks; i++)
        {
            var start = i * chunkSize;
            var len = Math.Min(chunkSize, message.Length - start);
            var data = message.Substring(start, len);

            var chunk = $"{{\"chunk\":{i},\"total\":{totalChunks},\"data\":\"{data}\"}}";
            var bytes = Encoding.UTF8.GetBytes(chunk);

            var writer = new DataWriter();
            writer.WriteBytes(bytes);
            await _cmdChar!.WriteValueAsync(writer.DetachBuffer());

            await Task.Delay(10);
        }
    }

    private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        var reader = DataReader.FromBuffer(args.CharacteristicValue);
        var bytes = new byte[reader.UnconsumedBufferLength];
        reader.ReadBytes(bytes);

        var json = Encoding.UTF8.GetString(bytes);
        MessageReceived?.Invoke(this, json);
    }

    private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}
