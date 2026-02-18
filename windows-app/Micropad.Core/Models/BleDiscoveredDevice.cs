namespace Micropad.Core.Models;

/// <summary>
/// Represents a BLE device discovered via advertisement or from paired devices.
/// When from paired list, DeviceId is set so we can connect without scanning.
/// </summary>
public class BleDiscoveredDevice
{
    public ulong BluetoothAddress { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Rssi { get; set; }

    /// <summary>Windows device id (e.g. BTHLEDevice#...) when device was added from paired list. Use for Connect when set.</summary>
    public string? DeviceId { get; set; }

    /// <summary>Display-friendly id (address or "Paired" when from paired list).</summary>
    public string DisplayId => !string.IsNullOrEmpty(DeviceId)
        ? "Paired"
        : $"{BluetoothAddress:X12}";
}
