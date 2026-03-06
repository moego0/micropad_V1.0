using System.IO.Compression;
using System.Text;
using Micropad.Core.Interfaces;
using Micropad.Core.Models;
using Micropad.Services.Communication;

namespace Micropad.Services;

public class DiagnosticsService
{
    private readonly IDeviceConnection _connection;
    private readonly ProtocolHandler _protocol;

    public DiagnosticsService(IDeviceConnection connection, ProtocolHandler protocol)
    {
        _connection = connection;
        _protocol = protocol;
    }

    /// <summary>Export diagnostics to a zip file: app logs, device info, firmware version, last BLE errors.</summary>
    public async Task<string> ExportDiagnosticsAsync(string? outputPath = null)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var fileName = $"Micropad-Diagnostics-{stamp}.zip";
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Micropad", "Diagnostics");
        Directory.CreateDirectory(dir);
        var zipPath = outputPath ?? Path.Combine(dir, fileName);

        using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            // App logs
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (Directory.Exists(logDir))
            {
                foreach (var file in Directory.EnumerateFiles(logDir, "*.log"))
                {
                    try
                    {
                        zip.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Fastest);
                    }
                    catch { /* skip locked files */ }
                }
            }

            // summary.txt: connection state, last error, device info if connected
            var sb = new StringBuilder();
            sb.AppendLine("Micropad Diagnostics");
            sb.AppendLine($"Generated: {DateTime.UtcNow:O}");
            sb.AppendLine();
            sb.AppendLine($"Connection state: {_connection.State}");
            sb.AppendLine($"IsConnected: {_connection.IsConnected}");
            sb.AppendLine($"Device name: {_connection.DeviceName}");
            if (!string.IsNullOrEmpty(_connection.LastError))
                sb.AppendLine($"Last BLE error: {_connection.LastError}");
            sb.AppendLine();

            if (_connection.IsConnected)
            {
                try
                {
                    var info = await _protocol.GetDeviceInfoAsync();
                    if (info != null)
                    {
                        sb.AppendLine("Device info:");
                        sb.AppendLine($"  DeviceId: {info.DeviceId}");
                        sb.AppendLine($"  Firmware: {info.FirmwareVersion}");
                        sb.AppendLine($"  Hardware: {info.HardwareVersion}");
                        sb.AppendLine($"  Battery: {info.BatteryLevel}%");
                        sb.AppendLine($"  Uptime: {info.Uptime}s");
                        sb.AppendLine($"  FreeHeap: {info.FreeHeap}");
                    }

                    var caps = await _protocol.GetCapsAsync();
                    if (caps != null)
                    {
                        sb.AppendLine($"Caps: maxProfiles={caps.MaxProfiles}, freeBytes={caps.FreeBytes}, supportsLayers={caps.SupportsLayers}, supportsEncoders={caps.SupportsEncoders}");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Error fetching device info: {ex.Message}");
                }
            }

            var entry = zip.CreateEntry("summary.txt", CompressionLevel.Fastest);
            using (var w = new StreamWriter(entry.Open(), Encoding.UTF8))
                w.Write(sb.ToString());
        }

        return zipPath;
    }
}
