using Newtonsoft.Json;

namespace Micropad.Core.Models;

/// <summary>Device capabilities from GET_CAPS / getCaps.</summary>
public class DeviceCaps
{
    [JsonProperty("maxProfiles")]
    public int MaxProfiles { get; set; }

    [JsonProperty("freeBytes")]
    public long FreeBytes { get; set; }

    [JsonProperty("supportsLayers")]
    public bool SupportsLayers { get; set; }

    [JsonProperty("supportsMacros")]
    public bool SupportsMacros { get; set; }

    [JsonProperty("supportsEncoders")]
    public bool SupportsEncoders { get; set; } = true;
}
