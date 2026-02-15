using System.IO;
using Micropad.Core.Models;
using Newtonsoft.Json;

namespace Micropad.Services.Storage;

public class LocalMacroStorage
{
    private readonly string _macrosDirectory;

    public LocalMacroStorage()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Micropad");
        _macrosDirectory = Path.Combine(appData, "Macros");
        if (!Directory.Exists(_macrosDirectory))
            Directory.CreateDirectory(_macrosDirectory);
    }

    public List<string> GetMacroNames()
    {
        if (!Directory.Exists(_macrosDirectory)) return new List<string>();
        var list = new List<string>();
        foreach (var file in Directory.EnumerateFiles(_macrosDirectory, "*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (!string.IsNullOrEmpty(name)) list.Add(name);
        }
        list.Sort(StringComparer.OrdinalIgnoreCase);
        return list;
    }

    public void SaveMacro(string name, List<MacroStep> steps)
    {
        var safe = string.Join("_", (name ?? "Unnamed").Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrEmpty(safe)) safe = "Unnamed";
        var path = Path.Combine(_macrosDirectory, safe + ".json");
        var json = JsonConvert.SerializeObject(new { name, steps }, Formatting.Indented);
        File.WriteAllText(path, json);
    }

    public List<MacroStep>? LoadMacro(string name)
    {
        var safe = string.Join("_", (name ?? "").Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(_macrosDirectory, safe + ".json");
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            var obj = JsonConvert.DeserializeObject<MacroFile>(json);
            return obj?.Steps;
        }
        catch { return null; }
    }

    private class MacroFile
    {
        public string? Name { get; set; }
        public List<MacroStep>? Steps { get; set; }
    }
}
