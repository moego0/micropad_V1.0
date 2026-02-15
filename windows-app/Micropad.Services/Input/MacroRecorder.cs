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

    public bool IsRecording => _recording;
    public IReadOnlyList<MacroStep> Steps => _steps;

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
        if (nCode >= 0 && _recording && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP))
        {
            var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var keyName = VirtualKeyToName((int)kbd.vkCode);

            _steps.Add(new MacroStep
            {
                Action = wParam == (IntPtr)WM_KEYDOWN ? "keyDown" : "keyUp",
                Key = keyName,
                DelayMs = (int)_stopwatch.ElapsedMilliseconds
            });
        }

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
                DelayMs = 0
            });
            lastTime = step.DelayMs;
        }

        return result;
    }

    private static string VirtualKeyToName(int vkCode)
    {
        if (vkCode >= 0x04 && vkCode <= 0x1D) return ((char)('A' + vkCode - 0x04)).ToString();
        if (vkCode >= 0x1E && vkCode <= 0x26) return (vkCode - 0x1E + 1).ToString();
        if (vkCode == 0x27) return "0";
        return vkCode switch
        {
            0x28 => "Enter",
            0x29 => "Esc",
            0x2A => "Backspace",
            0x2B => "Tab",
            0x2C => "Space",
            0x3A => "F1", 0x3B => "F2", 0x3C => "F3", 0x3D => "F4", 0x3E => "F5",
            0x3F => "F6", 0x40 => "F7", 0x41 => "F8", 0x42 => "F9", 0x43 => "F10",
            0x44 => "F11", 0x45 => "F12",
            0x4A => "Home", 0x4B => "PageUp", 0x4C => "Delete", 0x4D => "End", 0x4E => "PageDown",
            0x4F => "Right", 0x50 => "Left", 0x51 => "Down", 0x52 => "Up",
            _ => $"0x{vkCode:X2}"
        };
    }
}
