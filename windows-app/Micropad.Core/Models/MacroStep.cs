using System.ComponentModel;
using Newtonsoft.Json;

namespace Micropad.Core.Models;

public class MacroStep : INotifyPropertyChanged
{
    /// <summary>keyDown, keyUp, keyPress (tap), delay, textType, mouseClick, mouseWheelV, mouseWheelH, media</summary>
    private string _action = "keyPress";
    [JsonProperty("action")]
    public string Action { get => _action; set { _action = value; OnPropertyChanged(nameof(Action)); OnPropertyChanged(nameof(DisplayText)); } }

    private string? _key;
    [JsonProperty("key")]
    public string? Key { get => _key; set { _key = value; OnPropertyChanged(nameof(Key)); OnPropertyChanged(nameof(DisplayText)); } }

    private int _delayMs;
    [JsonProperty("ms")]
    public int DelayMs { get => _delayMs; set { _delayMs = value; OnPropertyChanged(nameof(DelayMs)); OnPropertyChanged(nameof(DisplayText)); } }

    /// <summary>Windows virtual-key code (VK_*). Stored when recording; converted to HID usage when exporting to device.</summary>
    [JsonProperty("vkCode")]
    public int? VkCode { get; set; }

    private string? _text;
    /// <summary>For textType: Unicode-safe text; may contain variables {clipboard}, {date}, {time}.</summary>
    [JsonProperty("text")]
    public string? Text { get => _text; set { _text = value; OnPropertyChanged(nameof(Text)); OnPropertyChanged(nameof(DisplayText)); } }

    /// <summary>For mouseClick: 0=Left, 1=Right, 2=Middle. For mouseWheelV/H: step value.</summary>
    [JsonProperty("value")]
    public int Value { get; set; }

    /// <summary>For media: MediaFunction value (volume up/down, mute, play, etc.).</summary>
    [JsonProperty("mediaFunction")]
    public int MediaFunction { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public string DisplayText => Action switch
    {
        "keyDown" => $"Key Down: {Key}",
        "keyUp" => $"Key Up: {Key}",
        "keyPress" => $"Key Press: {Key}",
        "delay" => $"Delay: {DelayMs}ms",
        "textType" => $"Text: {(Text?.Length > 20 ? Text.Substring(0, 20) + "..." : Text ?? "")}",
        "mouseClick" => $"Mouse: {(Value == 0 ? "L" : Value == 1 ? "R" : "M")}",
        "mouseWheelV" => $"Scroll V: {Value}",
        "mouseWheelH" => $"Scroll H: {Value}",
        "media" => $"Media: {(MediaFunction)MediaFunction}",
        _ => $"{Action}: {Key}"
    };
}
