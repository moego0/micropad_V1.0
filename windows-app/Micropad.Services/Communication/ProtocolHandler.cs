using System.Collections.Concurrent;
using Micropad.Core.Interfaces;
using Micropad.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Micropad.Services.Communication;

public class ProtocolHandler
{
    private readonly IDeviceConnection _connection;
    private int _nextRequestId;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<ProtocolMessage>> _pendingRequests = new();

    // Chunked receive: reassemble { "type": "chunk", ... } into full JSON before parsing
    private readonly List<string> _chunkBuffer = new();
    private int _chunkTotal = -1;
    private readonly object _chunkLock = new();

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

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cts.Token.Register(() => tcs.TrySetCanceled());

            return await tcs.Task;
        }
        catch
        {
            _pendingRequests.TryRemove(message.Id, out _);
            return null;
        }
    }

    private void OnMessageReceived(object? sender, string json)
    {
        try
        {
            // Check for chunk envelope: { "type": "chunk", "chunk": i, "total": N, "data": "..." } or "dataB64": "..."
            if (json.IndexOf("\"chunk\":", StringComparison.Ordinal) >= 0 &&
                json.IndexOf("\"total\":", StringComparison.Ordinal) >= 0)
            {
                ProcessChunk(json);
                return;
            }

            ProcessMessage(json);
        }
        catch
        {
            // Ignore malformed messages
        }
    }

    private void ProcessChunk(string chunkJson)
    {
        lock (_chunkLock)
        {
            JObject? obj;
            try
            {
                obj = JObject.Parse(chunkJson);
            }
            catch
            {
                return;
            }

            var chunkIndex = obj["chunk"]?.Value<int>() ?? 0;
            var total = obj["total"]?.Value<int>() ?? 0;
            if (total <= 0) return;

            string payload;
            var dataB64 = obj["dataB64"]?.Value<string>();
            if (!string.IsNullOrEmpty(dataB64))
            {
                try
                {
                    var bytes = Convert.FromBase64String(dataB64);
                    payload = System.Text.Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                    return;
                }
            }
            else
            {
                payload = obj["data"]?.Value<string>() ?? "";
                payload = payload.Replace("\\\"", "\"").Replace("\\\\", "\\");
            }

            if (chunkIndex == 0)
            {
                _chunkBuffer.Clear();
                _chunkTotal = total;
            }

            if (_chunkTotal != total) return;
            while (_chunkBuffer.Count <= chunkIndex)
                _chunkBuffer.Add("");
            _chunkBuffer[chunkIndex] = payload;

            if (_chunkBuffer.Count != _chunkTotal) return;

            var full = string.Concat(_chunkBuffer);
            _chunkBuffer.Clear();
            _chunkTotal = -1;
            ProcessMessage(full);
        }
    }

    private void ProcessMessage(string json)
    {
        var message = JsonConvert.DeserializeObject<ProtocolMessage>(json);
        if (message == null) return;

        if (message.Type == "response")
        {
            if (_pendingRequests.TryRemove(message.Id, out var tcs))
                tcs.TrySetResult(message);
        }
        else if (message.Type == "event")
        {
            EventReceived?.Invoke(this, message);
        }
    }

    private int GetNextRequestId()
    {
        return Interlocked.Increment(ref _nextRequestId);
    }
}
