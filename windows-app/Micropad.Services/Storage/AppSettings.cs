using System.Collections.Generic;
using Newtonsoft.Json;

namespace Micropad.Services.Storage;

public class AppSettings
{
    [JsonProperty("autoConnect")]
    public bool AutoConnect { get; set; } = true;

    [JsonProperty("minimizeToTray")]
    public bool MinimizeToTray { get; set; } = true;

    [JsonProperty("startWithWindows")]
    public bool StartWithWindows { get; set; }

    [JsonProperty("lastDeviceId")]
    public string? LastDeviceId { get; set; }

    [JsonProperty("autoReconnect")]
    public bool AutoReconnect { get; set; }

    [JsonProperty("foregroundMonitorMappings")]
    public Dictionary<string, int> ForegroundMonitorMappings { get; set; } = new();
}
