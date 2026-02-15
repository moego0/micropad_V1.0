using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Micropad.Services.Automation;

public class ForegroundMonitor
{
    private readonly System.Timers.Timer _timer;
    private string _lastProcessName = "";
    private readonly Dictionary<string, int> _processToProfile = new();

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
            var hwnd = GetForegroundWindow();
            GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0) return;

            using var process = Process.GetProcessById((int)pid);
            var name = process.ProcessName;

            if (name == _lastProcessName) return;
            _lastProcessName = name;

            ProcessChanged?.Invoke(this, name);

            if (_processToProfile.TryGetValue(name, out var profileId))
            {
                ProfileSwitchRequested?.Invoke(this, profileId);
            }
        }
        catch
        {
            // Ignore access denied or invalid process
        }
    }
}
