using System.IO;
using Micropad.Core.Models;
using Newtonsoft.Json;

namespace Micropad.Services.Storage;

public class LocalProfileStorage
{
    private readonly string _profilesDirectory;
    private const string ProfilesFolder = "Profiles";

    public LocalProfileStorage()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Micropad");
        _profilesDirectory = Path.Combine(appData, ProfilesFolder);

        if (!Directory.Exists(_profilesDirectory))
        {
            Directory.CreateDirectory(_profilesDirectory);
        }
    }

    public string ProfilesDirectory => _profilesDirectory;

    public List<Profile> GetAllProfiles()
    {
        var list = new List<Profile>();
        if (!Directory.Exists(_profilesDirectory)) return list;

        foreach (var file in Directory.EnumerateFiles(_profilesDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var profile = JsonConvert.DeserializeObject<Profile>(json);
                if (profile != null)
                {
                    list.Add(profile);
                }
            }
            catch
            {
                // Skip invalid files
            }
        }

        return list.OrderBy(p => p.Id).ToList();
    }

    public Profile? LoadProfile(int profileId)
    {
        var path = GetProfilePath(profileId);
        if (!File.Exists(path)) return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Profile>(json);
        }
        catch
        {
            return null;
        }
    }

    public void SaveProfile(Profile profile)
    {
        var path = GetProfilePath(profile.Id);
        var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
        File.WriteAllText(path, json);
    }

    public void DeleteProfile(int profileId)
    {
        var path = GetProfilePath(profileId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public void ExportProfile(Profile profile, string filePath)
    {
        var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public Profile? ImportProfile(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        try
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Profile>(json);
        }
        catch
        {
            return null;
        }
    }

    private string GetProfilePath(int profileId)
    {
        return Path.Combine(_profilesDirectory, $"profile_{profileId}.json");
    }
}
