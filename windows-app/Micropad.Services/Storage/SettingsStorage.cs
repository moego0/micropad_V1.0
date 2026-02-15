using System.IO;
using Newtonsoft.Json;

namespace Micropad.Services.Storage;

public class SettingsStorage
{
    private readonly string _settingsPath;
    private readonly object _lock = new();

    public SettingsStorage()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Micropad");
        if (!Directory.Exists(appData))
            Directory.CreateDirectory(appData);
        _settingsPath = Path.Combine(appData, "settings.json");
    }

    public AppSettings Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_settingsPath))
                return new AppSettings();
            try
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }
    }

    public void Save(AppSettings settings)
    {
        lock (_lock)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(_settingsPath, json);
        }
    }
}
