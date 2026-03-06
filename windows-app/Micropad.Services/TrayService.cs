using System.Windows.Forms;
using Micropad.Core.Interfaces;
using Micropad.Core.Models;
using Micropad.Services.Communication;
using Micropad.Services.Automation;
using Micropad.Services.Storage;
using Newtonsoft.Json.Linq;

namespace Micropad.Services;

public class TrayService
{
    private NotifyIcon? _notifyIcon;
    private readonly IDeviceConnection _connection;
    private readonly ProtocolHandler _protocol;
    private readonly ForegroundMonitor _foregroundMonitor;
    private readonly SettingsStorage _settingsStorage;
    private Action? _showMainWindow;
    private Action? _shutdownApp;
    private int? _activeProfileId;
    private string _activeProfileName = "";

    public TrayService(IDeviceConnection connection, ProtocolHandler protocol, ForegroundMonitor foregroundMonitor, SettingsStorage settingsStorage)
    {
        _connection = connection;
        _protocol = protocol;
        _foregroundMonitor = foregroundMonitor;
        _settingsStorage = settingsStorage;
        _connection.Connected += (_, _) => UpdateTray();
        _connection.Disconnected += (_, _) => UpdateTray();
        _connection.ConnectionStateChanged += (_, _) => UpdateTray();
        _protocol.EventReceived += OnProtocolEvent;
    }

    /// <summary>Call from App after main window is created. Required for Show and Exit.</summary>
    public void SetCallbacks(Action showMainWindow, Action shutdownApp)
    {
        _showMainWindow = showMainWindow;
        _shutdownApp = shutdownApp;
    }

    private void OnProtocolEvent(object? sender, ProtocolMessage msg)
    {
        if (msg.Event == "profileChanged")
        {
            var id = msg.Payload?["profileId"]?.Value<int>();
            if (id.HasValue)
            {
                _activeProfileId = id;
                _activeProfileName = ""; // could resolve from list
                UpdateTray();
                RefreshProfileMenu(); // so tray menu checkmark matches device
            }
        }
    }

    public void Initialize()
    {
        if (_notifyIcon != null) return;

        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "Micropad — Disconnected"
        };
        UpdateTray();
        BuildContextMenu();
        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var show = new ToolStripMenuItem("Show Micropad");
        show.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(show);
        menu.Items.Add(new ToolStripSeparator());

        for (int i = 0; i <= 7; i++)
        {
            var id = i;
            var item = new ToolStripMenuItem($"Profile {id}" + (id == _activeProfileId ? " ✓" : ""));
            item.Click += async (_, _) =>
            {
                try { await _protocol.SetActiveProfileAsync(id); } catch { }
            };
            menu.Items.Add(item);
        }
        menu.Items.Add(new ToolStripSeparator());

        var returnToAuto = new ToolStripMenuItem("Return to Auto");
        returnToAuto.Click += (_, _) => { _foregroundMonitor.ManualLock = false; };
        menu.Items.Add(returnToAuto);

        menu.Items.Add(new ToolStripSeparator());
        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, _) =>
        {
            _notifyIcon?.Dispose();
            _notifyIcon = null;
            _shutdownApp?.Invoke();
        };
        menu.Items.Add(exit);

        _notifyIcon!.ContextMenuStrip = menu;
    }

    public async void UpdateTray()
    {
        if (_notifyIcon == null) return;

        if (_connection.IsConnected && !_activeProfileId.HasValue)
        {
            try
            {
                var id = await _protocol.GetActiveProfileAsync();
                if (id.HasValue) _activeProfileId = id;
            }
            catch { /* ignore */ }
        }

        var status = _connection.State switch
        {
            ConnectionState.Ready => "Connected",
            ConnectionState.Connecting => "Connecting…",
            ConnectionState.Reconnecting => "Reconnecting…",
            ConnectionState.Error => "Error",
            _ => "Disconnected"
        };
        var profile = _activeProfileId.HasValue ? $" • Profile {_activeProfileId}" : "";
        _notifyIcon.Text = $"Micropad — {status}{profile}";
    }

    public void RefreshProfileMenu()
    {
        BuildContextMenu();
    }

    public void SetActiveProfile(int? profileId, string? name = null)
    {
        _activeProfileId = profileId;
        _activeProfileName = name ?? "";
        UpdateTray();
        RefreshProfileMenu();
    }

    private void ShowMainWindow()
    {
        _showMainWindow?.Invoke();
    }

    public bool MinimizeToTrayEnabled => _settingsStorage.Load().MinimizeToTray;

    public void Dispose()
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;
    }
}
