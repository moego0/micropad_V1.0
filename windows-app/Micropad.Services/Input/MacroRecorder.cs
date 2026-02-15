using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Micropad.Core.Models;

namespace Micropad.Services.Input;

public class MacroRecorder
{
    private readonly List<MacroStep> _steps = new();
    private readonly Stopwatch _stopwatch = new();
    private IntPtr _hookId = IntPtr.Zero;
    private bool _recording;
    private LowLevelKeyboardProc? _keyboardProc;

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int LLKHF_REPEAT = 0x01;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    /// <summary>Windows VK_* to USB HID usage ID (Keyboard page 0x07). Used when exporting macro to device.</summary>
    private static readonly Dictionary<int, int> VkToHid = new()
    {
        { 0x41, 0x04 }, { 0x42, 0x05 }, { 0x43, 0x06 }, { 0x44, 0x07 }, { 0x45, 0x08 }, { 0x46, 0x09 },
        { 0x47, 0x0A }, { 0x48, 0x0B }, { 0x49, 0x0C }, { 0x4A, 0x0D }, { 0x4B, 0x0E }, { 0x4C, 0x0F },
        { 0x4D, 0x10 }, { 0x4E, 0x11 }, { 0x4F, 0x12 }, { 0x50, 0x13 }, { 0x51, 0x14 }, { 0x52, 0x15 },
        { 0x53, 0x16 }, { 0x54, 0x17 }, { 0x55, 0x18 }, { 0x56, 0x19 }, { 0x57, 0x1A }, { 0x58, 0x1B },
        { 0x59, 0x1C }, { 0x5A, 0x1D },
        { 0x30, 0x27 }, { 0x31, 0x1E }, { 0x32, 0x1F }, { 0x33, 0x20 }, { 0x34, 0x21 }, { 0x35, 0x22 },
        { 0x36, 0x23 }, { 0x37, 0x24 }, { 0x38, 0x25 }, { 0x39, 0x26 },
        { 0x0D, 0x28 },  // Enter
        { 0x1B, 0x29 },  // Esc
        { 0x08, 0x2A },  // Backspace
        { 0x09, 0x2B },  // Tab
        { 0x20, 0x2C },  // Space
        { 0x70, 0x3A }, { 0x71, 0x3B }, { 0x72, 0x3C }, { 0x73, 0x3D }, { 0x74, 0x3E }, { 0x75, 0x3F },
        { 0x76, 0x40 }, { 0x77, 0x41 }, { 0x78, 0x42 }, { 0x79, 0x43 }, { 0x7A, 0x44 }, { 0x7B, 0x45 },  // F1..F12
        { 0x2D, 0x49 },  // Insert
        { 0x24, 0x4A },  // Home
        { 0x21, 0x4B },  // PageUp
        { 0x2E, 0x4C },  // Delete
        { 0x23, 0x4D },  // End
        { 0x22, 0x4E },  // PageDown
        { 0x27, 0x4F },  // Right
        { 0x25, 0x50 },  // Left
        { 0x28, 0x51 },  // Down (VK_DOWN)
        { 0x26, 0x52 },  // Up
        { 0x5B, 0xE3 },  // L Win
        { 0x5C, 0xE7 },  // R Win
    };

    /// <summary>HID usage ID to display name (for Key field and UI).</summary>
    private static readonly Dictionary<int, string> HidToName = new()
    {
        { 0x04, "A" }, { 0x05, "B" }, { 0x06, "C" }, { 0x07, "D" }, { 0x08, "E" }, { 0x09, "F" },
        { 0x0A, "G" }, { 0x0B, "H" }, { 0x0C, "I" }, { 0x0D, "J" }, { 0x0E, "K" }, { 0x0F, "L" },
        { 0x10, "M" }, { 0x11, "N" }, { 0x12, "O" }, { 0x13, "P" }, { 0x14, "Q" }, { 0x15, "R" },
        { 0x16, "S" }, { 0x17, "T" }, { 0x18, "U" }, { 0x19, "V" }, { 0x1A, "W" }, { 0x1B, "X" },
        { 0x1C, "Y" }, { 0x1D, "Z" },
        { 0x1E, "1" }, { 0x1F, "2" }, { 0x20, "3" }, { 0x21, "4" }, { 0x22, "5" }, { 0x23, "6" },
        { 0x24, "7" }, { 0x25, "8" }, { 0x26, "9" }, { 0x27, "0" },
        { 0x28, "Enter" }, { 0x29, "Esc" }, { 0x2A, "Backspace" }, { 0x2B, "Tab" }, { 0x2C, "Space" },
        { 0x3A, "F1" }, { 0x3B, "F2" }, { 0x3C, "F3" }, { 0x3D, "F4" }, { 0x3E, "F5" }, { 0x3F, "F6" },
        { 0x40, "F7" }, { 0x41, "F8" }, { 0x42, "F9" }, { 0x43, "F10" }, { 0x44, "F11" }, { 0x45, "F12" },
        { 0x49, "Insert" }, { 0x4A, "Home" }, { 0x4B, "PageUp" }, { 0x4C, "Delete" }, { 0x4D, "End" },
        { 0x4E, "PageDown" }, { 0x4F, "Right" }, { 0x50, "Left" }, { 0x51, "Down" }, { 0x52, "Up" },
        { 0xE3, "Win" }, { 0xE7, "Win" },
    };

    public bool IsRecording => _recording;
    public IReadOnlyList<MacroStep> Steps => _steps;

    /// <summary>Convert Windows VK code to USB HID usage (for device export). Returns null if unknown.</summary>
    public static int? VkToHidUsage(int vkCode)
    {
        return VkToHid.TryGetValue(vkCode, out var hid) ? hid : null;
    }

    /// <summary>Convert to display/key name from VK (uses VK→HID→name so device and UI agree).</summary>
    private static string VkToKeyName(int vkCode)
    {
        if (VkToHid.TryGetValue(vkCode, out var hid) && HidToName.TryGetValue(hid, out var name))
            return name;
        return $"0x{vkCode:X2}";
    }

    public void StartRecording()
    {
        _steps.Clear();
        _stopwatch.Restart();
        _recording = true;
        _keyboardProc = KeyboardHookCallback;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, IntPtr.Zero, 0);
    }

    public List<MacroStep> StopRecording()
    {
        _recording = false;
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }

        return ConvertToDelays(_steps);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0 || !_recording) return CallNextHookEx(_hookId, nCode, wParam, lParam);

        bool isKeyDown = wParam == (IntPtr)WM_KEYDOWN;
        bool isKeyUp = wParam == (IntPtr)WM_KEYUP;
        if (!isKeyDown && !isKeyUp) return CallNextHookEx(_hookId, nCode, wParam, lParam);

        var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
        var vkCode = (int)kbd.vkCode;
        bool isRepeat = (kbd.flags & LLKHF_REPEAT) != 0;

        // Skip key repeat unless we want to record repeats (currently we do not)
        if (isRepeat) return CallNextHookEx(_hookId, nCode, wParam, lParam);

        var keyName = VkToKeyName(vkCode);

        _steps.Add(new MacroStep
        {
            Action = isKeyDown ? "keyDown" : "keyUp",
            Key = keyName,
            VkCode = vkCode,
            DelayMs = (int)_stopwatch.ElapsedMilliseconds
        });
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static List<MacroStep> ConvertToDelays(List<MacroStep> raw)
    {
        var result = new List<MacroStep>();
        int lastTime = 0;

        foreach (var step in raw)
        {
            var delay = step.DelayMs - lastTime;
            if (delay > 0)
            {
                result.Add(new MacroStep { Action = "delay", DelayMs = delay });
            }

            result.Add(new MacroStep
            {
                Action = step.Action,
                Key = step.Key,
                VkCode = step.VkCode,
                DelayMs = 0
            });
            lastTime = step.DelayMs;
        }

        return result;
    }

    /// <summary>Returns steps with Key as display name; use VkToHidUsage(step.VkCode) when exporting HID to device.</summary>
    public static int? GetHidUsageForStep(MacroStep step)
    {
        if (step.VkCode.HasValue) return VkToHidUsage(step.VkCode.Value);
        if (!string.IsNullOrEmpty(step.Key) && step.Key.StartsWith("0x") && step.Key.Length <= 6)
        {
            if (int.TryParse(step.Key.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out var vk))
                return VkToHidUsage(vk);
        }
        return null;
    }
}
