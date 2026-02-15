using Micropad.Core.Interfaces;
using Micropad.Core.Models;
using Micropad.Services.Communication;
using Micropad.Services.Storage;

namespace Micropad.Services;

public class ProfileSyncService
{
    private readonly IDeviceConnection _connection;
    private readonly ProtocolHandler _protocol;
    private readonly LocalProfileStorage _localStorage;

    public ProfileSyncService(
        IDeviceConnection connection,
        ProtocolHandler protocol,
        LocalProfileStorage localStorage)
    {
        _connection = connection;
        _protocol = protocol;
        _localStorage = localStorage;
    }

    public async Task<bool> PushProfileToDeviceAsync(Profile profile)
    {
        if (!_connection.IsConnected)
        {
            return false;
        }

        return await _protocol.SetProfileAsync(profile);
    }

    public async Task<Profile?> PullProfileFromDeviceAsync(int profileId)
    {
        if (!_connection.IsConnected)
        {
            return null;
        }

        return await _protocol.GetProfileAsync(profileId);
    }

    public async Task SyncAllAsync()
    {
        if (!_connection.IsConnected) return;

        var deviceProfiles = await _protocol.ListProfilesAsync();
        if (deviceProfiles == null) return;

        var localProfiles = _localStorage.GetAllProfiles();
        var deviceIds = deviceProfiles.Select(p => p.Id).ToHashSet();

        foreach (var local in localProfiles)
        {
            if (!deviceIds.Contains(local.Id))
            {
                await _protocol.SetProfileAsync(local);
            }
        }
    }

    public void SaveProfileLocally(Profile profile)
    {
        _localStorage.SaveProfile(profile);
    }

    public Profile? LoadProfileLocally(int profileId)
    {
        return _localStorage.LoadProfile(profileId);
    }

    public List<Profile> GetLocalProfiles()
    {
        return _localStorage.GetAllProfiles();
    }

    public void ExportProfile(Profile profile, string filePath)
    {
        _localStorage.ExportProfile(profile, filePath);
    }

    public Profile? ImportProfileFromFile(string filePath)
    {
        return _localStorage.ImportProfile(filePath);
    }
}
