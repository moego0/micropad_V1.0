using Micropad.Core.Models;
using Micropad.Services.Communication;
using Newtonsoft.Json.Linq;

namespace Micropad.Services;

public class HudService
{
    private readonly ProtocolHandler _protocol;

    /// <summary>Raised when HUD text should be shown (subscribe from App to display overlay).</summary>
    public event EventHandler<string>? ShowRequested;

    public HudService(ProtocolHandler protocol)
    {
        _protocol = protocol;
        _protocol.EventReceived += OnEvent;
    }

    private void OnEvent(object? sender, ProtocolMessage msg)
    {
        if (msg.Event == "profileChanged")
        {
            var idToken = msg.Payload?["profileId"];
            var nameToken = msg.Payload?["name"];
            var id = idToken?.ToObject<int>();
            var name = nameToken?.ToObject<string>();
            RaiseShow($"Profile: {name ?? $"Slot {id}"}");
        }
        else if (msg.Event == "layerChanged")
        {
            var layerToken = msg.Payload?["layer"];
            var layer = layerToken != null ? layerToken.ToObject<int>() : 0;
            RaiseShow($"Layer {layer}");
        }
    }

    private void RaiseShow(string text)
    {
        ShowRequested?.Invoke(this, text);
    }

    public void Show(string line1, string? line2 = null, string? line3 = null)
    {
        var text = line1;
        if (!string.IsNullOrEmpty(line2)) text += "\n" + line2;
        if (!string.IsNullOrEmpty(line3)) text += "\n" + line3;
        RaiseShow(text);
    }

    public void ShowProfileLayerEncoder(string profileName, int? layer = null, string? encoderMode = null)
    {
        var parts = new List<string> { profileName ?? "Profile" };
        if (layer.HasValue) parts.Add($"Layer {layer}");
        if (!string.IsNullOrEmpty(encoderMode)) parts.Add(encoderMode);
        RaiseShow(string.Join(" • ", parts));
    }
}
