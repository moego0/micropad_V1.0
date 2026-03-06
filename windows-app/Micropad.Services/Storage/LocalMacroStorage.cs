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

    // --- MacroAsset (GUID-based library) ---

    public void SaveMacroAsset(MacroAsset asset)
    {
        asset.UpdatedAt = DateTime.UtcNow;
        var path = Path.Combine(_macrosDirectory, asset.MacroId + ".json");
        var json = JsonConvert.SerializeObject(asset, Formatting.Indented);
        File.WriteAllText(path, json);
    }

    public MacroAsset? LoadMacroAsset(string macroId)
    {
        var path = Path.Combine(_macrosDirectory, macroId + ".json");
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<MacroAsset>(json);
        }
        catch { return null; }
    }

    public List<MacroAsset> GetAllMacroAssets()
    {
        var list = new List<MacroAsset>();
        if (!Directory.Exists(_macrosDirectory)) return list;
        foreach (var file in Directory.EnumerateFiles(_macrosDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var asset = JsonConvert.DeserializeObject<MacroAsset>(json);
                if (asset != null) list.Add(asset);
            }
            catch { }
        }
        return list.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public List<MacroAsset> SearchMacros(string? searchText, IReadOnlyList<string>? tagFilter)
    {
        var all = GetAllMacroAssets();
        if (string.IsNullOrWhiteSpace(searchText) && (tagFilter == null || tagFilter.Count == 0))
            return all;
        return all.Where(m =>
        {
            if (!string.IsNullOrWhiteSpace(searchText) &&
                m.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0 &&
                !m.Tags.Any(t => t.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0))
                return false;
            if (tagFilter != null && tagFilter.Count > 0 && !tagFilter.Any(t => m.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
                return false;
            return true;
        }).ToList();
    }

    public void DeleteMacroAsset(string macroId)
    {
        var path = Path.Combine(_macrosDirectory, macroId + ".json");
        if (File.Exists(path)) File.Delete(path);
    }

    public void ExportMacroAsset(MacroAsset asset, string filePath)
    {
        var json = JsonConvert.SerializeObject(asset, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public MacroAsset? ImportMacroAsset(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        try
        {
            var json = File.ReadAllText(filePath);
            var asset = JsonConvert.DeserializeObject<MacroAsset>(json);
            if (asset != null)
            {
                asset.MacroId = Guid.NewGuid().ToString("N");
                asset.CreatedAt = DateTime.UtcNow;
                asset.UpdatedAt = DateTime.UtcNow;
                SaveMacroAsset(asset);
            }
            return asset;
        }
        catch { return null; }
    }
}
