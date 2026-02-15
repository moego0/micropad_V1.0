using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Micropad.Services.Communication;
using Newtonsoft.Json.Linq;

namespace Micropad.App.ViewModels;

public partial class StatsViewModel : ObservableObject
{
    private readonly ProtocolHandler _protocol;
    private readonly DispatcherTimer _refreshTimer;

    [ObservableProperty]
    private string _statusText = "Connect to device and click Refresh.";

    [ObservableProperty]
    private long _uptimeSeconds;

    [ObservableProperty]
    private ObservableCollection<int> _keyPressCounts = new();

    [ObservableProperty]
    private ObservableCollection<int> _encoderTurns = new();

    [ObservableProperty]
    private bool _autoRefresh = true;

    [ObservableProperty]
    private int _totalKeypresses;

    public StatsViewModel(ProtocolHandler protocol)
    {
        _protocol = protocol;
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _refreshTimer.Tick += (_, _) => _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            var payload = await _protocol.GetStatsAsync();
            if (payload == null)
            {
                StatusText = "No stats (device may not support getStats).";
                return;
            }

            UptimeSeconds = payload["uptime"]?.Value<long>() ?? 0;
            var keyPresses = payload["keyPresses"] as JArray;
            var encoderTurnsArray = payload["encoderTurns"] as JArray;

            KeyPressCounts.Clear();
            if (keyPresses != null)
            {
                foreach (var v in keyPresses)
                {
                    KeyPressCounts.Add(v.Value<int>());
                }
            }
            while (KeyPressCounts.Count < 12)
            {
                KeyPressCounts.Add(0);
            }

            EncoderTurns.Clear();
            if (encoderTurnsArray != null)
            {
                foreach (var v in encoderTurnsArray)
                {
                    EncoderTurns.Add(v.Value<int>());
                }
            }
            while (EncoderTurns.Count < 2)
            {
                EncoderTurns.Add(0);
            }

            TotalKeypresses = KeyPressCounts.Sum();
            StatusText = $"Total key presses: {TotalKeypresses} | Uptime: {FormatUptime(UptimeSeconds)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    partial void OnAutoRefreshChanged(bool value)
    {
        if (value)
            _refreshTimer.Start();
        else
            _refreshTimer.Stop();
    }

    public void StartAutoRefresh()
    {
        if (AutoRefresh) _refreshTimer.Start();
    }

    public void StopAutoRefresh()
    {
        _refreshTimer.Stop();
    }

    private static string FormatUptime(long seconds)
    {
        var span = TimeSpan.FromSeconds(seconds);
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}h {span.Minutes}m";
        if (span.TotalMinutes >= 1)
            return $"{(int)span.TotalMinutes}m {span.Seconds}s";
        return $"{span.Seconds}s";
    }
}
