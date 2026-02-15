using Newtonsoft.Json;

namespace Micropad.Core.Models;

public class Profile
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = "Unnamed";

    [JsonProperty("version")]
    public int Version { get; set; } = 1;

    [JsonProperty("keys")]
    public List<KeyConfig> Keys { get; set; } = new();

    [JsonProperty("encoders")]
    public List<EncoderConfig> Encoders { get; set; } = new();
}

public class KeyConfig
{
    [JsonProperty("index")]
    public int Index { get; set; }

    [JsonProperty("type")]
    public ActionType Type { get; set; }

    [JsonProperty("modifiers")]
    public int Modifiers { get; set; }

    [JsonProperty("key")]
    public int Key { get; set; }

    [JsonProperty("text")]
    public string? Text { get; set; }

    [JsonProperty("function")]
    public int Function { get; set; }

    [JsonProperty("action")]
    public int Action { get; set; }

    [JsonProperty("value")]
    public int Value { get; set; }

    [JsonProperty("profileId")]
    public int ProfileId { get; set; }

    [JsonProperty("path")]
    public string? AppPath { get; set; }

    [JsonProperty("url")]
    public string? Url { get; set; }

    [JsonProperty("macroId")]
    public string? MacroId { get; set; }

    public string DisplayName => GetDisplayName();

    private string GetDisplayName()
    {
        return Type switch
        {
            ActionType.Hotkey => $"Hotkey: {GetHotkeyString()}",
            ActionType.Text => $"Text: {Text?.Substring(0, Math.Min(20, Text?.Length ?? 0))}...",
            ActionType.Media => $"Media: {((MediaFunction)Function).ToString()}",
            ActionType.Mouse => $"Mouse: {((MouseAction)Action).ToString()}",
            ActionType.Profile => $"Switch to Profile {ProfileId}",
            ActionType.App => string.IsNullOrEmpty(AppPath) ? "Launch App" : $"App: {Path.GetFileName(AppPath)}",
            ActionType.Url => string.IsNullOrEmpty(Url) ? "Open URL" : $"URL: {(Url?.Length > 20 ? Url.Substring(0, 20) + "..." : Url)}",
            ActionType.Macro => string.IsNullOrEmpty(MacroId) ? "Macro" : $"Macro: {MacroId}",
            ActionType.None => "Not Assigned",
            _ => "Unknown"
        };
    }

    private string GetHotkeyString()
    {
        var parts = new List<string>();
        if ((Modifiers & 0x01) != 0) parts.Add("Ctrl");
        if ((Modifiers & 0x02) != 0) parts.Add("Shift");
        if ((Modifiers & 0x04) != 0) parts.Add("Alt");
        if ((Modifiers & 0x08) != 0) parts.Add("Win");
        
        parts.Add(GetKeyName(Key));
        
        return string.Join("+", parts);
    }

    private string GetKeyName(int key)
    {
        // Basic key mapping
        if (key >= 0x04 && key <= 0x1D) return ((char)('A' + key - 0x04)).ToString();
        if (key >= 0x1E && key <= 0x26) return (key - 0x1E + 1).ToString();
        if (key == 0x27) return "0";
        
        return key switch
        {
            0x28 => "Enter",
            0x29 => "Esc",
            0x2A => "Backspace",
            0x2B => "Tab",
            0x2C => "Space",
            _ => $"Key{key:X2}"
        };
    }
}

public class EncoderConfig
{
    [JsonProperty("index")]
    public int Index { get; set; }

    [JsonProperty("acceleration")]
    public bool Acceleration { get; set; }

    [JsonProperty("stepsPerDetent")]
    public int StepsPerDetent { get; set; }
}

public enum ActionType
{
    None = 0,
    Hotkey = 1,
    Macro = 2,
    Text = 3,
    Media = 4,
    Mouse = 5,
    Layer = 6,
    Profile = 7,
    App = 8,
    Url = 9
}

public enum MediaFunction
{
    VolumeUp = 0,
    VolumeDown = 1,
    Mute = 2,
    PlayPause = 3,
    Next = 4,
    Prev = 5,
    Stop = 6
}

public enum MouseAction
{
    Click = 0,
    RightClick = 1,
    MiddleClick = 2,
    ScrollUp = 3,
    ScrollDown = 4
}
