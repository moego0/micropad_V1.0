namespace Micropad.Core.Interfaces;

public interface IDeviceConnection
{
    bool IsConnected { get; }
    string DeviceName { get; }
    
    event EventHandler<string>? MessageReceived;
    event EventHandler? Connected;
    event EventHandler? Disconnected;
    
    Task<bool> ConnectAsync(string deviceId);
    Task DisconnectAsync();
    Task SendMessageAsync(string json);
}
