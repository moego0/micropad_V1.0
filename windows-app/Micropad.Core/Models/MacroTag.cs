namespace Micropad.Core.Models;

/// <summary>Single macro tag (e.g. {CTRL}, {F5}) for drag-drop shortcut builder.</summary>
public class MacroTag
{
    public string Tag { get; set; } = string.Empty;
    public string Display { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
}
