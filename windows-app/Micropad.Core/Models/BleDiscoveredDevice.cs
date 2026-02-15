namespace Micropad.Core.Models;

/// <summary>
/// Represents a BLE device discovered via advertisement (e.g. filtered by Config Service UUID).
/// Used when scanning with BluetoothLEAdvertisementWatcher so we can show devices
/// regardless of advertised name.
/// </summary>
public class BleDiscoveredDevice
{
    public ulong BluetoothAddress { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Rssi { get; set; }

    /// <summary>Display-friendly id (e.g. address as hex).</summary>
    public string DisplayId => $"{BluetoothAddress:X12}";
}
