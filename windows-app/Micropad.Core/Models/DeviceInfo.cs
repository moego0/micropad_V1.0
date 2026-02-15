using Newtonsoft.Json;

namespace Micropad.Core.Models;

public class DeviceInfo
{
    [JsonProperty("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonProperty("firmwareVersion")]
    public string FirmwareVersion { get; set; } = string.Empty;

    [JsonProperty("hardwareVersion")]
    public string HardwareVersion { get; set; } = string.Empty;

    [JsonProperty("batteryLevel")]
    public int BatteryLevel { get; set; }

    [JsonProperty("capabilities")]
    public List<string> Capabilities { get; set; } = new();

    [JsonProperty("uptime")]
    public long Uptime { get; set; }

    [JsonProperty("freeHeap")]
    public long FreeHeap { get; set; }
}
