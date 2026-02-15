using Micropad.Core.Interfaces;
using Micropad.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Micropad.Services.Communication;

public class ProtocolHandler
{
    private readonly IDeviceConnection _connection;
    private int _nextRequestId = 1;
    private readonly Dictionary<int, TaskCompletionSource<ProtocolMessage>> _pendingRequests = new();

    public event EventHandler<ProtocolMessage>? EventReceived;

    public ProtocolHandler(IDeviceConnection connection)
    {
        _connection = connection;
        _connection.MessageReceived += OnMessageReceived;
    }

    public async Task<DeviceInfo?> GetDeviceInfoAsync()
    {
        var response = await SendRequestAsync("getDeviceInfo");
        if (response?.Payload == null) return null;

        return response.Payload.ToObject<DeviceInfo>();
    }

    public async Task<List<Profile>?> ListProfilesAsync()
    {
        var response = await SendRequestAsync("listProfiles");
        if (response?.Payload?["profiles"] == null) return null;

        var profiles = new List<Profile>();
        foreach (var item in response.Payload["profiles"]!)
        {
            var id = item["id"]?.Value<int>() ?? 0;
            var name = item["name"]?.Value<string>() ?? "Unknown";
            var size = item["size"]?.Value<int>() ?? 0;

            profiles.Add(new Profile
            {
                Id = id,
                Name = name
            });
        }

        return profiles;
    }

    public async Task<Profile?> GetProfileAsync(int profileId)
    {
        var message = new ProtocolMessage
        {
            Version = 1,
            Type = "request",
            Id = GetNextRequestId(),
            Command = "getProfile",
            ProfileId = profileId
        };

        var response = await SendRequestAsync(message);
        if (response?.Payload == null) return null;

        return response.Payload.ToObject<Profile>();
    }

    public async Task<bool> SetActiveProfileAsync(int profileId)
    {
        var message = new ProtocolMessage
        {
            Version = 1,
            Type = "request",
            Id = GetNextRequestId(),
            Command = "setActiveProfile",
            ProfileId = profileId
        };

        var response = await SendRequestAsync(message);
        return response?.Payload?["success"]?.Value<bool>() ?? false;
    }

    public async Task<bool> SetProfileAsync(Profile profile)
    {
        var message = new ProtocolMessage
        {
            Version = 1,
            Type = "request",
            Id = GetNextRequestId(),
            Command = "setProfile",
            Profile = JObject.FromObject(profile)
        };

        var response = await SendRequestAsync(message);
        return response?.Payload?["success"]?.Value<bool>() ?? false;
    }

    public async Task<JObject?> GetStatsAsync()
    {
        var response = await SendRequestAsync("getStats");
        return response?.Payload;
    }

    private async Task<ProtocolMessage?> SendRequestAsync(string command, JObject? payload = null)
    {
        var message = new ProtocolMessage
        {
            Version = 1,
            Type = "request",
            Id = GetNextRequestId(),
            Command = command,
            Payload = payload
        };

        return await SendRequestAsync(message);
    }

    private async Task<ProtocolMessage?> SendRequestAsync(ProtocolMessage message)
    {
        var tcs = new TaskCompletionSource<ProtocolMessage>();
        _pendingRequests[message.Id] = tcs;

        try
        {
            var json = JsonConvert.SerializeObject(message);
            await _connection.SendMessageAsync(json);

            // Wait for response with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cts.Token.Register(() => tcs.TrySetCanceled());

            return await tcs.Task;
        }
        catch
        {
            _pendingRequests.Remove(message.Id);
            return null;
        }
    }

    private void OnMessageReceived(object? sender, string json)
    {
        try
        {
            var message = JsonConvert.DeserializeObject<ProtocolMessage>(json);
            if (message == null) return;

            if (message.Type == "response")
            {
                // Handle response
                if (_pendingRequests.TryGetValue(message.Id, out var tcs))
                {
                    tcs.SetResult(message);
                    _pendingRequests.Remove(message.Id);
                }
            }
            else if (message.Type == "event")
            {
                // Handle event
                EventReceived?.Invoke(this, message);
            }
        }
        catch
        {
            // Ignore malformed messages
        }
    }

    private int GetNextRequestId()
    {
        return Interlocked.Increment(ref _nextRequestId);
    }
}
