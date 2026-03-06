using Newtonsoft.Json;

namespace Micropad.Core.Models;

/// <summary>Reusable macro saved in the library (GUID, name, tags, steps, timestamps).</summary>
public class MacroAsset
{
    [JsonProperty("macroId")]
    public string MacroId { get; set; } = Guid.NewGuid().ToString("N");

    [JsonProperty("name")]
    public string Name { get; set; } = "Unnamed";

    [JsonProperty("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonProperty("steps")]
    public List<MacroStep> Steps { get; set; } = new();

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("version")]
    public int Version { get; set; } = 1;
}
