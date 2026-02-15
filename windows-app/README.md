# Micropad Windows Application

## Overview

Professional WPF application for configuring and managing your Micropad wireless macropad via Bluetooth LE. Implements the full spec: device discovery, profile editor with 3×4 key grid, macro recording, per-app profile switching, and statistics.

## Requirements

- Windows 10/11 (Build 19041 or later)
- .NET 8.0 SDK
- Bluetooth LE support

## Building

```bash
cd windows-app
dotnet restore
dotnet build
dotnet run --project Micropad.App
```

Publish for distribution:

```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
# Output: Micropad.App\bin\Release\net8.0-windows10.0.22621.0\win-x64\publish\Micropad.App.exe
```

## Project Structure

```
Micropad.sln
├── Micropad.App/              # WPF UI Application
│   ├── Views/                 # XAML Pages
│   ├── ViewModels/            # MVVM ViewModels
│   ├── Dialogs/               # Action edit, etc.
│   └── MainWindow.xaml
│
├── Micropad.Core/             # Shared Models & Interfaces
│   ├── Models/
│   │   ├── Profile.cs, KeyConfig, EncoderConfig
│   │   ├── DeviceInfo.cs, ProtocolMessage.cs
│   │   └── MacroStep.cs
│   └── Interfaces/
│       └── IDeviceConnection.cs
│
└── Micropad.Services/         # Business Logic
    ├── Communication/         # BLE, ProtocolHandler
    ├── Storage/               # LocalProfileStorage
    ├── Input/                 # MacroRecorder
    ├── Automation/            # ForegroundMonitor
    └── ProfileSyncService.cs
```

## Features

### Device & connection
- **Device discovery** – Scan for Bluetooth LE devices (Micropad/ESP32)
- **Connect / disconnect** – Pair and connect to the device
- **Device info** – Firmware version, battery, uptime (when supported)

### Profiles
- **Profile list** – Load and list all profiles from the device
- **3×4 key grid** – Click any key (K1–K12) to assign an action
- **Action types** – Hotkey, Text, Media, Mouse, Profile switch, App launch, URL
- **Activate profile** – Switch the active profile on the device
- **Push to device** – Upload the edited profile to the device
- **Save locally** – Store profiles in `%LocalAppData%\Micropad\Profiles`
- **Import / Export** – Load and save profile JSON files

### Macros
- **Record** – Start/stop recording; key down/up and delays are captured
- **Steps list** – Add delay steps, remove steps, clear
- **Use in profiles** – Assign macro to a key in the profile editor (macro type)

### Statistics
- **Refresh** – Request stats from the device (`getStats`)
- **Key presses** – Per-key counts (K1–K12)
- **Encoder turns** – ENC1 and ENC2
- **Uptime** – Device uptime
- **Auto-refresh** – Optional 5-second refresh

### Per-app profile switching
- **Settings → Per-app profile switching** – Map process name to profile ID (e.g. `Code` → 1)
- When the foreground window changes, the app switches to the mapped profile if connected

### Settings
- Auto-connect, Start with Windows, Minimize to tray (UI in place; tray/startup logic can be extended)
- Process-to-profile mapping list (add/remove)

## Usage

1. **Launch** – Run the app (e.g. `dotnet run --project Micropad.App`).
2. **Devices** – Scan, select your Micropad, Connect.
3. **Profiles** – Refresh to load profiles; select one to edit. Click a key to open the action dialog; set type and options, then Save. Use **Push to device** to upload.
4. **Macros** – Start Recording, press keys, Stop Recording. Add/remove steps as needed.
5. **Stats** – Connect, then Refresh (or enable Auto-refresh).
6. **Per-app switching** – In Settings, enter process name (e.g. `Code`) and profile ID, then Add.

## Protocol (BLE GATT)

- **Service UUID**: `4fafc201-1fb5-459e-8fcc-c5c9c331914b`
- **CMD (Write)**: `4fafc201-1fb5-459e-8fcc-c5c9c331914c`
- **EVT (Notify)**: `4fafc201-1fb5-459e-8fcc-c5c9c331914d`

Supported commands: `getDeviceInfo`, `listProfiles`, `getProfile`, `setProfile`, `setActiveProfile`, `getStats`, `factoryReset`, `reboot`.

## Troubleshooting

- **Bluetooth / scan** – Enable Bluetooth (and BLE), grant app access, restart app.
- **Connection fails** – Device on and in range; try forget and re-pair.
- **Profile push fails** – Ensure connection is active; firmware must support `setProfile`.
- **Stats empty** – Device must implement `getStats` and return `keyPresses`, `encoderTurns`, `uptime`.

## License

See main project LICENSE file.
