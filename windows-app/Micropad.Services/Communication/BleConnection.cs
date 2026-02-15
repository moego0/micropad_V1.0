using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
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
        try
        {
            // Connect to device
            _device = await BluetoothLEDevice.FromIdAsync(deviceId);
            if (_device == null)
            {
                return false;
            }

            // Get config service
            var servicesResult = await _device.GetGattServicesForUuidAsync(_configServiceUuid);
            if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
            {
                return false;
            }

            _configService = servicesResult.Services[0];

            // Get CMD characteristic
            var cmdResult = await _configService.GetCharacteristicsForUuidAsync(_cmdCharUuid);
            if (cmdResult.Status != GattCommunicationStatus.Success || cmdResult.Characteristics.Count == 0)
            {
                return false;
            }
            _cmdChar = cmdResult.Characteristics[0];

            // Get EVT characteristic
            var evtResult = await _configService.GetCharacteristicsForUuidAsync(_evtCharUuid);
            if (evtResult.Status != GattCommunicationStatus.Success || evtResult.Characteristics.Count == 0)
            {
                return false;
            }
            _evtChar = evtResult.Characteristics[0];

            // Subscribe to notifications
            var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
            await _evtChar.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue);
            _evtChar.ValueChanged += OnValueChanged;

            // Listen for disconnection
            _device.ConnectionStatusChanged += OnConnectionStatusChanged;

            Connected?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch
        {
            return false;
        }
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
