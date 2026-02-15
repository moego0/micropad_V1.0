using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Micropad.Core.Models;

public class ProtocolMessage
{
    [JsonProperty("v")]
    public int Version { get; set; } = 1;

    [JsonProperty("type")]
    public string Type { get; set; } = "request";

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("ts")]
    public long Timestamp { get; set; }

    [JsonProperty("cmd")]
    public string? Command { get; set; }

    [JsonProperty("event")]
    public string? Event { get; set; }

    [JsonProperty("payload")]
    public JObject? Payload { get; set; }

    [JsonProperty("profileId")]
    public int? ProfileId { get; set; }

    [JsonProperty("profile")]
    public JObject? Profile { get; set; }
}
