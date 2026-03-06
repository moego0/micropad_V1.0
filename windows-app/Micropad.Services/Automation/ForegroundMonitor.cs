using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Micropad.Services.Automation;

public class ForegroundMonitor
{
    private readonly System.Timers.Timer _timer;
    private string _lastProcessName = "";
    private string _pendingProcessName = "";
    private int? _pendingProfileId;
    private readonly System.Timers.Timer _debounceTimer;
    private readonly Dictionary<string, int> _processToProfile = new();

    /// <summary>When true, auto profile switching is paused (manual lock).</summary>
    public bool ManualLock { get; set; }

    /// <summary>Profile to switch to when no rule matches (optional).</summary>
    public int? DefaultProfileId { get; set; }

    /// <summary>Debounce delay in ms before switching profile (avoid rapid toggling).</summary>
    public int DebounceMs { get; set; } = 800;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    public event EventHandler<string>? ProcessChanged;
    public event EventHandler<int>? ProfileSwitchRequested;

    public ForegroundMonitor()
    {
        _timer = new System.Timers.Timer(500);
        _timer.Elapsed += OnTick;
        _debounceTimer = new System.Timers.Timer(1);
        _debounceTimer.Elapsed += OnDebounceElapsed;
        _debounceTimer.AutoReset = false;
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public void SetProcessProfileMapping(string processName, int profileId)
    {
        _processToProfile[processName] = profileId;
    }

    public void RemoveProcessMapping(string processName)
    {
        _processToProfile.Remove(processName);
    }

    public IReadOnlyDictionary<string, int> GetMappings() => _processToProfile;

    private void OnTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            if (ManualLock) return;

            var hwnd = GetForegroundWindow();
            GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0) return;

            using var process = Process.GetProcessById((int)pid);
            var name = process.ProcessName;

            if (name == _pendingProcessName) return;
            _pendingProcessName = name;

            ProcessChanged?.Invoke(this, name);

            if (_processToProfile.TryGetValue(name, out var profileId))
            {
                _pendingProfileId = profileId;
                _debounceTimer.Interval = Math.Max(1, DebounceMs);
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
            else if (DefaultProfileId.HasValue)
            {
                _pendingProfileId = DefaultProfileId.Value;
                _debounceTimer.Interval = Math.Max(1, DebounceMs);
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
        }
        catch
        {
            // Ignore access denied or invalid process
        }
    }

    private void OnDebounceElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (ManualLock) return;
        if (_pendingProfileId is int profileId)
        {
            _lastProcessName = _pendingProcessName;
            _pendingProfileId = null;
            ProfileSwitchRequested?.Invoke(this, profileId);
        }
    }
}
