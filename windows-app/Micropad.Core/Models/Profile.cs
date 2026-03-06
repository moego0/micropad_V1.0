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

    /// <summary>Optional: Layer1 (Fn1) and Layer2 (Fn2) keys; Layer0 = Keys. When null, single-layer.</summary>
    [JsonProperty("layer1Keys")]
    public List<KeyConfig>? Layer1Keys { get; set; }

    [JsonProperty("layer2Keys")]
    public List<KeyConfig>? Layer2Keys { get; set; }

    /// <summary>Optional: chord combos (two keys together trigger action).</summary>
    [JsonProperty("combos")]
    public List<ComboAssignment>? Combos { get; set; }
}

/// <summary>Two keys pressed together trigger this action.</summary>
public class ComboAssignment
{
    [JsonProperty("key1")]
    public int Key1 { get; set; }

    [JsonProperty("key2")]
    public int Key2 { get; set; }

    [JsonProperty("action")]
    public KeyAction? Action { get; set; }

    public string Display => $"K{Key1 + 1}+K{Key2 + 1}";
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

    /// <summary>Optional: embedded macro steps when assignment mode is Embed (portable snapshot).</summary>
    [JsonProperty("macroSnapshot")]
    public List<MacroStep>? MacroSnapshot { get; set; }

    /// <summary>Tap action (when key is tapped without hold).</summary>
    [JsonProperty("tapAction")]
    public KeyAction? TapAction { get; set; }

    /// <summary>Hold action (when key is held).</summary>
    [JsonProperty("holdAction")]
    public KeyAction? HoldAction { get; set; }

    /// <summary>Double-tap action.</summary>
    [JsonProperty("doubleTapAction")]
    public KeyAction? DoubleTapAction { get; set; }

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

    [JsonProperty("stepSize")]
    public int StepSize { get; set; } = 1;

    [JsonProperty("accelerationCurve")]
    public string AccelerationCurve { get; set; } = "linear"; // linear, exp

    [JsonProperty("smoothing")]
    public bool Smoothing { get; set; }

    /// <summary>Encoder mode index (0=A, 1=B, 2=C); press cycles mode.</summary>
    [JsonProperty("mode")]
    public int Mode { get; set; }

    // Rotate / Press / Hold / Press+Rotate / Hold+Rotate (optional; firmware may use cwAction, ccwAction, pressAction)
    [JsonProperty("cwAction")]
    public EncoderActionConfig? CwAction { get; set; }

    [JsonProperty("ccwAction")]
    public EncoderActionConfig? CcwAction { get; set; }

    [JsonProperty("pressAction")]
    public EncoderActionConfig? PressAction { get; set; }

    [JsonProperty("holdAction")]
    public EncoderActionConfig? HoldAction { get; set; }

    [JsonProperty("pressRotateCwAction")]
    public EncoderActionConfig? PressRotateCwAction { get; set; }

    [JsonProperty("pressRotateCcwAction")]
    public EncoderActionConfig? PressRotateCcwAction { get; set; }

    [JsonProperty("holdRotateCwAction")]
    public EncoderActionConfig? HoldRotateCwAction { get; set; }

    [JsonProperty("holdRotateCcwAction")]
    public EncoderActionConfig? HoldRotateCcwAction { get; set; }
}

/// <summary>Single action for encoder (type + value, e.g. volume, scroll, media).</summary>
public class EncoderActionConfig
{
    [JsonProperty("type")]
    public string Type { get; set; } = "none"; // none, volume, scrollV, scrollH, media, hotkey, profile, etc.

    [JsonProperty("value")]
    public int Value { get; set; }

    [JsonProperty("key")]
    public int Key { get; set; }

    [JsonProperty("modifiers")]
    public int Modifiers { get; set; }

    [JsonProperty("mediaFunction")]
    public int MediaFunction { get; set; }
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
    ScrollDown = 4,
    ScrollLeft = 5,
    ScrollRight = 6
}

/// <summary>Single action for tap/hold/double-tap on a key.</summary>
public class KeyAction
{
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
}
