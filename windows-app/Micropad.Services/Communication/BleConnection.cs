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
    private GattSession? _gattSession;
    private GattDeviceService? _configService;
    private GattCharacteristic? _cmdChar;
    private GattCharacteristic? _evtChar;

    private readonly Guid _configServiceUuid = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
    private readonly Guid _cmdCharUuid = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914c");
    private readonly Guid _evtCharUuid = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914d");

    private string? _lastDeviceId;
    private bool _autoReconnect;
    private CancellationTokenSource? _reconnectCts;
    private readonly object _connectionLock = new();

    /// <summary>When true, attempt to reconnect with exponential backoff after disconnect (e.g. when connection drops).</summary>
    public bool AutoReconnect
    {
        get => _autoReconnect;
        set => _autoReconnect = value;
    }

    public bool IsConnected => _device != null && _device.ConnectionStatus == BluetoothConnectionStatus.Connected;
    public string DeviceName => _device?.Name ?? string.Empty;

    public event EventHandler<string>? MessageReceived;
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public async Task<bool> ConnectAsync(string deviceId)
    {
        lock (_connectionLock)
        {
            _lastDeviceId = deviceId;
            _reconnectCts?.Cancel();
            _reconnectCts = null;
        }

        try
        {
            _device = await BluetoothLEDevice.FromIdAsync(deviceId);
            if (_device == null)
            {
                throw new InvalidOperationException(
                    "Could not open the device (FromIdAsync returned null). Remove Micropad from Settings → Bluetooth → Remove device, then power-cycle the Micropad and try again.");
            }

            if (!_device.DeviceInformation.Pairing.IsPaired)
            {
                var pairingResult = await PairWithFallbackAsync();
                if (pairingResult.Status != DevicePairingResultStatus.Paired &&
                    pairingResult.Status != DevicePairingResultStatus.AlreadyPaired)
                {
                    DisposeConnectionHandles();
                    throw new InvalidOperationException(
                        $"Pairing failed: {pairingResult.Status}. Remove device from Bluetooth settings, power-cycle Micropad, then try Connect again.");
                }
                Trace.WriteLine($"[BLE] Pairing result: {pairingResult.Status}");
            }

            await Task.Delay(500);

            // Create GattSession and set MaintainConnection = true for stable connection on Windows 11
            try
            {
                _gattSession = await GattSession.FromDeviceIdAsync(_device.BluetoothDeviceId);
                if (_gattSession != null && _gattSession.CanMaintainConnection)
                {
                    _gattSession.MaintainConnection = true;
                    Trace.WriteLine("[BLE] GattSession created with MaintainConnection = true");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[BLE] GattSession creation failed (continuing): {ex.Message}");
            }

            var servicesResult = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (servicesResult.Status != GattCommunicationStatus.Success)
            {
                DisposeConnectionHandles();
                var err = servicesResult.Status == GattCommunicationStatus.Unreachable
                    ? "Unreachable (device may be connected as HID only). Remove from Bluetooth, power-cycle Micropad, then Connect."
                    : $"{servicesResult.Status}. Try: remove Micropad from Bluetooth, power-cycle device, then Connect.";
                throw new InvalidOperationException($"GATT services error: {err}");
            }

            var serviceList = servicesResult.Services.Select(s => s.Uuid.ToString()).ToList();
            Trace.WriteLine($"[BLE] GATT status: {servicesResult.Status}, discovered services ({serviceList.Count}): " +
                string.Join("; ", serviceList));

            _configService = servicesResult.Services.FirstOrDefault(s => s.Uuid == _configServiceUuid);
            if (_configService == null)
            {
                DisposeConnectionHandles();
                var listed = string.Join(", ", serviceList);
                throw new InvalidOperationException(
                    "Device does not expose the Micropad config service. Check firmware. Discovered service UUIDs: " + listed);
            }
            Trace.WriteLine("[BLE] Config service 4fafc201-1fb5-459e-8fcc-c5c9c331914b found.");

            var cmdResult = await _configService.GetCharacteristicsForUuidAsync(_cmdCharUuid);
            if (cmdResult.Status != GattCommunicationStatus.Success || cmdResult.Characteristics.Count == 0)
            {
                DisposeConnectionHandles();
                throw new InvalidOperationException("Config CMD characteristic not found. Reflash the Micropad firmware.");
            }
            _cmdChar = cmdResult.Characteristics[0];

            var evtResult = await _configService.GetCharacteristicsForUuidAsync(_evtCharUuid);
            if (evtResult.Status != GattCommunicationStatus.Success || evtResult.Characteristics.Count == 0)
            {
                DisposeConnectionHandles();
                throw new InvalidOperationException("Config EVT characteristic not found. Reflash the Micropad firmware.");
            }
            _evtChar = evtResult.Characteristics[0];

            var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
            await _evtChar.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue);
            _evtChar.ValueChanged += OnValueChanged;

            _device.ConnectionStatusChanged += OnConnectionStatusChanged;

            Connected?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            DisposeConnectionHandles();
            throw new InvalidOperationException($"{ex.GetType().Name}: {ex.Message}", ex);
        }
    }

    private async Task<DevicePairingResult> PairWithFallbackAsync()
    {
        if (_device?.DeviceInformation.Pairing == null) throw new InvalidOperationException("Pairing not available.");
        try
        {
            var result = await _device.DeviceInformation.Pairing.PairAsync(DevicePairingProtectionLevel.Encryption);
            if (result.Status == DevicePairingResultStatus.Paired || result.Status == DevicePairingResultStatus.AlreadyPaired)
                return result;
            Trace.WriteLine($"[BLE] Pairing with Encryption failed: {result.Status}, trying None.");
            return await _device.DeviceInformation.Pairing.PairAsync(DevicePairingProtectionLevel.None);
        }
        catch
        {
            return await _device!.DeviceInformation.Pairing.PairAsync(DevicePairingProtectionLevel.None);
        }
    }

    private void DisposeConnectionHandles()
    {
        if (_evtChar != null)
        {
            try { _evtChar.ValueChanged -= OnValueChanged; } catch { }
            _evtChar = null;
        }
        _cmdChar = null;
        _configService?.Dispose();
        _configService = null;
        _gattSession?.Dispose();
        _gattSession = null;
        if (_device != null)
        {
            try { _device.ConnectionStatusChanged -= OnConnectionStatusChanged; } catch { }
            _device.Dispose();
            _device = null;
        }
    }

    public async Task DisconnectAsync()
    {
        lock (_connectionLock)
        {
            _reconnectCts?.Cancel();
            _reconnectCts = null;
        }
        if (_evtChar != null)
            _evtChar.ValueChanged -= OnValueChanged;
        DisposeConnectionHandles();
        Disconnected?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
        {
            DisposeConnectionHandles();
            Disconnected?.Invoke(this, EventArgs.Empty);

            if (_autoReconnect && !string.IsNullOrEmpty(_lastDeviceId))
            {
                _ = TryAutoReconnectAsync();
            }
        }
    }

    private async Task TryAutoReconnectAsync()
    {
        lock (_connectionLock)
        {
            _reconnectCts?.Cancel();
            _reconnectCts = new CancellationTokenSource();
        }
        var cts = _reconnectCts!;
        int delayMs = 1000;
        const int maxDelayMs = 30000;
        int attempt = 0;

        while (!cts.Token.IsCancellationRequested)
        {
            attempt++;
            await Task.Delay(delayMs, cts.Token).ConfigureAwait(false);
            if (cts.Token.IsCancellationRequested) return;

            var deviceId = _lastDeviceId;
            if (string.IsNullOrEmpty(deviceId)) return;

            try
            {
                Trace.WriteLine($"[BLE] Auto-reconnect attempt {attempt}");
                await ConnectAsync(deviceId).ConfigureAwait(false);
                return;
            }
            catch
            {
                delayMs = Math.Min(delayMs * 2, maxDelayMs);
            }
        }
    }

    public async Task SendMessageAsync(string json)
    {
        if (_cmdChar == null || !IsConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        var bytes = Encoding.UTF8.GetBytes(json);

        if (bytes.Length > 512)
        {
            await SendChunkedAsync(bytes).ConfigureAwait(false);
        }
        else
        {
            var writer = new DataWriter();
            writer.WriteBytes(bytes);
            await _cmdChar.WriteValueAsync(writer.DetachBuffer());
        }
    }

    /// <summary>
    /// Chunk format (firmware-compatible): each chunk is a JSON object:
    /// { "chunk": index, "total": totalChunks, "dataB64": "base64-encoded-utf8-payload" }
    /// Chunks are split by UTF-8 byte length (not string length). Payload is Base64 to avoid JSON escaping.
    /// Max single write ~512 bytes; chunk envelope ~80 bytes so payload ~400 bytes per chunk (Base64 adds ~33%).
    /// </summary>
    private async Task SendChunkedAsync(byte[] utf8Bytes)
    {
        const int maxPayloadBytes = 400;
        var totalChunks = (utf8Bytes.Length + maxPayloadBytes - 1) / maxPayloadBytes;

        for (int i = 0; i < totalChunks; i++)
        {
            int start = i * maxPayloadBytes;
            int len = Math.Min(maxPayloadBytes, utf8Bytes.Length - start);
            var segment = new byte[len];
            Array.Copy(utf8Bytes, start, segment, 0, len);
            var dataB64 = Convert.ToBase64String(segment);

            var chunk = $"{{\"chunk\":{i},\"total\":{totalChunks},\"dataB64\":\"{dataB64}\"}}";
            var chunkBytes = Encoding.UTF8.GetBytes(chunk);

            var writer = new DataWriter();
            writer.WriteBytes(chunkBytes);
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
}
