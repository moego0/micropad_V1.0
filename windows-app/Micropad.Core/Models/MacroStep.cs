using Newtonsoft.Json;

namespace Micropad.Core.Models;

public class MacroStep
{
    [JsonProperty("action")]
    public string Action { get; set; } = "keyPress"; // keyDown, keyUp, keyPress, delay

    [JsonProperty("key")]
    public string? Key { get; set; }

    [JsonProperty("ms")]
    public int DelayMs { get; set; }

    /// <summary>Windows virtual-key code (VK_*). Stored when recording; converted to HID usage when exporting to device.</summary>
    [JsonProperty("vkCode")]
    public int? VkCode { get; set; }

    public string DisplayText => Action switch
    {
        "keyDown" => $"Key Down: {Key}",
        "keyUp" => $"Key Up: {Key}",
        "keyPress" => $"Key Press: {Key}",
        "delay" => $"Delay: {DelayMs}ms",
        _ => $"{Action}: {Key}"
    };
}
