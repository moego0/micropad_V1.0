using System.Collections.Generic;
using Micropad.Core.Models;

namespace Micropad.Core;

/// <summary>Catalog of all macro tags for the shortcut builder (modifiers, extended, F-keys, media, mouse, etc.).</summary>
public static class MacroTagCatalog
{
    public const string DataFormat = "Micropad.MacroTag";

    public static IReadOnlyList<MacroTag> GetAll()
    {
        var list = new List<MacroTag>();

        // Modifiers
        foreach (var t in new[] { "{CTRL}", "{RCTRL}", "{ALT}", "{RALT}", "{SHIFT}", "{RSHIFT}", "{LWIN}", "{RWIN}", "{APPS}" })
            list.Add(new MacroTag { Tag = t, Display = t.Trim('{', '}'), Group = "Modifiers" });

        // Extended
        foreach (var t in new[] { "{DEL}", "{INS}", "{PGUP}", "{PGDN}", "{HOME}", "{END}", "{RETURN}", "{ESCAPE}", "{BACKSPACE}", "{TAB}", "{PRTSCN}", "{PAUSE}", "{SPACE}", "{CAPSLOCK}", "{NUMLOCK}", "{SCROLLLOCK}", "{BREAK}", "{CTRLBREAK}" })
            list.Add(new MacroTag { Tag = t, Display = t.Trim('{', '}'), Group = "Extended" });

        // Direction
        foreach (var t in new[] { "{UP}", "{DOWN}", "{LEFT}", "{RIGHT}" })
            list.Add(new MacroTag { Tag = t, Display = t.Trim('{', '}'), Group = "Direction" });

        // Function F1-F24
        for (int i = 1; i <= 24; i++)
            list.Add(new MacroTag { Tag = $"{{F{i}}}", Display = $"F{i}", Group = "Function" });

        // Volume / Media
        foreach (var t in new[] { "{VOL+}", "{VOL-}", "{MUTE}", "{MEDIAPLAY}", "{MEDIASTOP}", "{MEDIANEXT}", "{MEDIAPREV}" })
            list.Add(new MacroTag { Tag = t, Display = t.Trim('{', '}'), Group = "Volume/Media" });

        // Mouse
        foreach (var t in new[] { "{LMB}", "{RMB}", "{MMB}", "{MB4/XMB1}", "{MB5/XMB2}", "{LMBD}", "{LMBU}", "{RMBD}", "{RMBU}", "{MWUP}", "{MWDN}", "{TILTL}", "{TILTR}" })
            list.Add(new MacroTag { Tag = t, Display = t.Trim('{', '}'), Group = "Mouse" });

        // NumPad
        foreach (var t in new[] { "{NUM0}", "{NUM1}", "{NUM2}", "{NUM3}", "{NUM4}", "{NUM5}", "{NUM6}", "{NUM7}", "{NUM8}", "{NUM9}", "{NUM+}", "{NUM-}", "{NUM.}", "{NUM/}", "{NUM*}", "{NUMENTER}" })
            list.Add(new MacroTag { Tag = t, Display = t.Replace("NUM", "N"), Group = "NumPad" });

        // Letters A–Z
        for (char c = 'A'; c <= 'Z'; c++)
            list.Add(new MacroTag { Tag = c.ToString(), Display = c.ToString(), Group = "Letters" });
        for (char c = 'a'; c <= 'z'; c++)
            list.Add(new MacroTag { Tag = c.ToString(), Display = c.ToString(), Group = "Letters" });

        // Numbers and common symbols
        foreach (var t in new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", " ", "-", "=", "[", "]", "\\", ";", "'", "`", ",", ".", "/" })
            list.Add(new MacroTag { Tag = t, Display = t == " " ? "Space" : t, Group = "Keys" });

        // Special
        foreach (var t in new[] { "{WAIT:1}", "{WAITMS:100}", "{HOLD:1}", "{HOLDMS:50}", "{CLEAR}", "{PRESS}", "{RELEASE}", "{OD}", "{OU}", "{OR}" })
            list.Add(new MacroTag { Tag = t, Display = t.Trim('{', '}'), Group = "Special" });

        // Toggle
        foreach (var t in new[] { "{NUMLOCKON}", "{NUMLOCKOFF}", "{CAPSLOCKON}", "{CAPSLOCKOFF}", "{SCROLLLOCKON}", "{SCROLLLOCKOFF}" })
            list.Add(new MacroTag { Tag = t, Display = t.Trim('{', '}'), Group = "Toggle" });

        // Web
        foreach (var t in new[] { "{BACK}", "{FORWARD}", "{STOP}", "{REFRESH}", "{WEBHOME}", "{SEARCH}", "{FAVORITES}" })
            list.Add(new MacroTag { Tag = t, Display = t.Trim('{', '}'), Group = "Web" });

        return list;
    }
}
