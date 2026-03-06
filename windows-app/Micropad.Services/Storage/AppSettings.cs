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

    [JsonProperty("manualLock")]
    public bool ManualLock { get; set; }

    [JsonProperty("defaultProfileId")]
    public int? DefaultProfileId { get; set; }

    [JsonProperty("debounceMs")]
    public int DebounceMs { get; set; } = 800;

    /// <summary>Theme: "Light", "Dark", or empty for system/default.</summary>
    [JsonProperty("theme")]
    public string Theme { get; set; } = "Dark";

    /// <summary>When true, non-essential animations are disabled.</summary>
    [JsonProperty("reduceMotion")]
    public bool ReduceMotion { get; set; }

    /// <summary>Accent color as hex (e.g. #0078D4). Empty = default.</summary>
    [JsonProperty("accentColorHex")]
    public string? AccentColorHex { get; set; }

    /// <summary>Background: "Solid", "Acrylic", "Mica".</summary>
    [JsonProperty("backgroundMode")]
    public string BackgroundMode { get; set; } = "Solid";

    /// <summary>Setup journey completed step ids (e.g. UseTemplate, ConfigureEncoder).</summary>
    [JsonProperty("completedSetupSteps")]
    public List<string> CompletedSetupSteps { get; set; } = new();

    /// <summary>When true, the "Getting started" panel is hidden and will not show again.</summary>
    [JsonProperty("dismissedGettingStarted")]
    public bool DismissedGettingStarted { get; set; }
}
