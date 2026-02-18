using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Micropad.App.ViewModels;
using Micropad.App.Views;
using Micropad.Core.Interfaces;
using Micropad.Services.Communication;
using Serilog;

namespace Micropad.App;

public partial class App : Application
{
    private IHost? _host;

    public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Host not initialized");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Setup logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/micropad-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Build host
        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Services
                services.AddSingleton<IDeviceConnection, BleConnection>();
                services.AddSingleton<ProtocolHandler>();
                services.AddSingleton<Micropad.Services.Storage.LocalProfileStorage>();
                services.AddSingleton<Micropad.Services.Storage.LocalMacroStorage>();
                services.AddSingleton<Micropad.Services.Storage.SettingsStorage>();
                services.AddSingleton<Micropad.Services.ProfileSyncService>();
                services.AddSingleton<Micropad.Services.Input.MacroRecorder>();

                // ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddTransient<DevicesViewModel>();
                services.AddTransient<ProfilesViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<MacrosViewModel>();
                services.AddTransient<StatsViewModel>();
                services.AddSingleton<Micropad.Services.Automation.ForegroundMonitor>();

                // Views/Pages
                services.AddTransient<DevicesView>();
                services.AddTransient<ProfilesView>();
                services.AddTransient<MacrosView>();
                services.AddTransient<StatsView>();
                services.AddTransient<SettingsView>();

                // Main Window
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // Apply persisted settings: AutoReconnect and optional auto-connect to last device
        var settingsStorage = _host.Services.GetRequiredService<Micropad.Services.Storage.SettingsStorage>();
        var settings = settingsStorage.Load();
        if (_host.Services.GetRequiredService<Micropad.Core.Interfaces.IDeviceConnection>() is Micropad.Services.Communication.BleConnection bleConnection)
        {
            bleConnection.AutoReconnect = settings.AutoReconnect;
        }
        if (settings.AutoConnect && !string.IsNullOrEmpty(settings.LastDeviceId))
        {
            var connection = _host.Services.GetRequiredService<Micropad.Core.Interfaces.IDeviceConnection>();
            async Task TryAutoConnect()
            {
                await Task.Delay(800);
                try { await connection.ConnectAsync(settings.LastDeviceId!); } catch { /* ignore */ }
            }
            _ = TryAutoConnect();
        }

        // Show main window
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
