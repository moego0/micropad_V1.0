namespace Micropad.Core.Interfaces;

public enum ConnectionState
{
    Idle,
    Scanning,
    Pairing,
    Connecting,
    Ready,
    Reconnecting,
    Error
}

public interface IDeviceConnection
{
    bool IsConnected { get; }
    string DeviceName { get; }
    ConnectionState State { get; }
    string? LastError { get; }
    
    event EventHandler<string>? MessageReceived;
    event EventHandler? Connected;
    event EventHandler? Disconnected;
    event EventHandler? ConnectionStateChanged;
    
    Task<bool> ConnectAsync(string deviceId);
    Task DisconnectAsync();
    Task SendMessageAsync(string json);
}
