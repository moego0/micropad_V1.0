# Professional Wireless Macropad System - AI Agent Build Prompt

## Executive Summary
Build a production-ready wireless macropad system: ESP32-based hardware + Windows WPF companion app. Target: 10/10 product quality with professional UX, reliable wireless connectivity, and advanced macro capabilities.

---

## Hardware Specification (Fixed)

### Board
- **Controller**: Wemos D1 Mini ESP32
- **Connectivity**: BLE + WiFi
- **Storage**: LittleFS for profiles (4MB+ recommended)

### Input Hardware
```
Matrix Layout (3×4 grid):
┌────┬────┬────┬────┐
│ K1 │ K2 │ K3 │ K4 │  Row 0: GPIO 16
├────┼────┼────┼────┤
│ K5 │ K6 │ K7 │ K8 │  Row 1: GPIO 17
├────┼────┼────┼────┤
│ K9 │K10 │K11 │K12 │  Row 2: GPIO 18
└────┴────┴────┴────┘
Col:  21   22   23   19

Encoders:
- ENC1 (top-left): A=32, B=33, SW=27
- ENC2 (top-right): A=25, B=26, SW=13

Matrix: Per-key diodes present (COL2K orientation)
```

### GPIO Safety Notes
- Avoid: GPIO 0, 2, 12, 15 (boot strapping)
- Safe inputs: 16-19, 21-23, 25-27, 32-33
- If boot issues occur: provide pin remapping guide

---

## System Architecture

### Communication Modes (Both Required)

#### Mode 1: BLE (Primary)
```
Purpose: Actual keyboard/mouse input to PC
Implementation:
  - BLE HID Profile (Keyboard + Consumer Control + Mouse)
  - Custom GATT Service for configuration
  
Service UUID: 4fafc201-1fb5-459e-8fcc-c5c9c331914b
Characteristics:
  - CMD_TX  (Write): 4fafc201-1fb5-459e-8fcc-c5c9c331914c
  - EVT_RX  (Notify): 4fafc201-1fb5-459e-8fcc-c5c9c331914d
  - BULK_TX (Write): 4fafc201-1fb5-459e-8fcc-c5c9c331914e (for large transfers)
```

#### Mode 2: WiFi (Configuration Channel)
```
Purpose: Faster profile transfers, debugging, optional input streaming
Implementation:
  - WebSocket server on ESP32 (port 8765)
  - mDNS: micropad.local
  - Fallback: manual IP entry in app
  - AP mode for initial setup (SSID: Micropad-XXXX)
```

### Protocol Specification (JSON)

#### Message Envelope
```json
{
  "v": 1,                    // Protocol version
  "type": "request|response|event",
  "id": 12345,              // Request ID (for req/resp pairing)
  "ts": 1708012345,         // Unix timestamp
  "payload": {}
}
```

#### Core Messages (Required)

**Device Info**
```json
// Request
{"v":1, "type":"request", "id":1, "cmd":"getDeviceInfo"}

// Response
{
  "v":1, "type":"response", "id":1,
  "payload": {
    "deviceId": "ESP32-A1B2C3",
    "firmwareVersion": "1.0.0",
    "hardwareVersion": "1.0",
    "batteryLevel": 85,
    "capabilities": ["ble", "wifi", "macros", "layers"],
    "uptime": 12345,
    "freeHeap": 180000
  }
}
```

**Profile Management**
```json
// List profiles
{"v":1, "type":"request", "id":2, "cmd":"listProfiles"}
Response: {"profiles": [{"id":0, "name":"General", "size":2048}, ...]}

// Get profile
{"v":1, "type":"request", "id":3, "cmd":"getProfile", "profileId":0}

// Set profile (see Profile Structure below)
{"v":1, "type":"request", "id":4, "cmd":"setProfile", "profile":{...}}

// Switch active profile
{"v":1, "type":"request", "id":5, "cmd":"setActiveProfile", "profileId":1}

// Profile changed event (device → app)
{"v":1, "type":"event", "event":"profileChanged", "profileId":1}
```

**Statistics**
```json
// Get stats
{"v":1, "type":"request", "id":6, "cmd":"getStats"}

// Stats event (periodic push)
{
  "v":1, "type":"event", "event":"stats",
  "payload": {
    "keyPresses": [15, 32, 8, ...],  // 12 keys
    "encoderTurns": [142, 89],       // 2 encoders
    "uptime": 86400
  }
}
```

**System Commands**
```json
// Factory reset
{"v":1, "type":"request", "id":7, "cmd":"factoryReset"}

// Reboot
{"v":1, "type":"request", "id":8, "cmd":"reboot"}

// Enter DFU mode
{"v":1, "type":"request", "id":9, "cmd":"enterDFU"}
```

---

## Profile Data Structure

### Profile JSON Schema
```json
{
  "id": 0,
  "name": "General",
  "version": 1,
  "keys": [
    {
      "index": 0,
      "type": "hotkey|macro|text|media|mouse|layer|profile|app|url",
      "config": {
        // Type-specific config (see Action Types below)
      },
      "behavior": {
        "tapAction": {...},      // On quick press
        "holdAction": {...},     // On hold (>500ms default)
        "doubleTapAction": {...},// On double tap (<300ms)
        "holdThreshold": 500,    // ms
        "doubleTapWindow": 300,  // ms
        "turbo": false,          // Repeat while held
        "turboInterval": 50      // ms between repeats
      }
    }
    // ... 11 more keys
  ],
  "encoders": [
    {
      "index": 0,
      "cwAction": {...},
      "ccwAction": {...},
      "pressAction": {...},
      "pressCwAction": {...},   // Press + rotate CW
      "pressCcwAction": {...},  // Press + rotate CCW
      "acceleration": true,
      "stepsPerDetent": 4
    },
    {
      "index": 1,
      // ... same structure
    }
  ],
  "layers": [
    {
      "index": 0,
      "name": "Base",
      "keys": [...],  // Same as root keys structure
      "encoders": [...]
    }
    // Up to 4 layers
  ],
  "settings": {
    "debounceMs": 5,
    "activeLayer": 0,
    "enableCombos": true,
    "combos": [
      {
        "keys": [0, 3],        // K1 + K4
        "holdMs": 800,
        "action": {"type": "profile", "profileId": 1}
      }
    ]
  }
}
```

### Action Types

#### 1. Hotkey
```json
{
  "type": "hotkey",
  "config": {
    "modifiers": ["ctrl", "shift"],  // ctrl, shift, alt, win
    "key": "c",                       // Key code or name
    "keys": ["ctrl", "shift", "esc"]  // Alternative: sequence
  }
}
```

#### 2. Macro
```json
{
  "type": "macro",
  "config": {
    "id": "macro_uuid_123",
    "steps": [
      {"action": "keyDown", "key": "ctrl"},
      {"action": "keyPress", "key": "c"},
      {"action": "keyUp", "key": "ctrl"},
      {"action": "delay", "ms": 100},
      {"action": "keyPress", "key": "v"}
    ]
  }
}
```

#### 3. Text
```json
{
  "type": "text",
  "config": {
    "text": "Hello, world!",
    "append": "\n"  // Optional
  }
}
```

#### 4. Media
```json
{
  "type": "media",
  "config": {
    "function": "volumeUp|volumeDown|mute|playPause|next|prev|stop"
  }
}
```

#### 5. Mouse
```json
{
  "type": "mouse",
  "config": {
    "action": "click|rightClick|middleClick|scrollUp|scrollDown|moveX|moveY",
    "value": 10  // For move/scroll
  }
}
```

#### 6. Layer Switch
```json
{
  "type": "layer",
  "config": {
    "layer": 1,
    "mode": "toggle|momentary|switch"
  }
}
```

#### 7. Profile Switch
```json
{
  "type": "profile",
  "config": {
    "profileId": 2
  }
}
```

#### 8. Application Launch
```json
{
  "type": "app",
  "config": {
    "path": "C:\\Program Files\\App\\app.exe",
    "args": "--flag"
  }
}
```

#### 9. URL Open
```json
{
  "type": "url",
  "config": {
    "url": "https://example.com"
  }
}
```

---

## Firmware Implementation (ESP32)

### Project Structure
```
firmware/
├── platformio.ini
├── src/
│   ├── main.cpp
│   ├── config.h                 // Pin definitions, constants
│   ├── input/
│   │   ├── matrix.h/.cpp       // Key matrix scanning
│   │   ├── encoder.h/.cpp      // Rotary encoder handling
│   │   └── debounce.h/.cpp     // Debouncing logic
│   ├── actions/
│   │   ├── action_executor.h/.cpp
│   │   ├── key_actions.h/.cpp
│   │   └── macro_player.h/.cpp
│   ├── profiles/
│   │   ├── profile_manager.h/.cpp
│   │   └── profile_storage.h/.cpp  // LittleFS operations
│   ├── comms/
│   │   ├── ble_hid.h/.cpp      // BLE HID keyboard
│   │   ├── ble_config.h/.cpp   // GATT config service
│   │   ├── wifi_manager.h/.cpp
│   │   └── websocket_server.h/.cpp
│   ├── power/
│   │   └── power_manager.h/.cpp
│   └── utils/
│       ├── logger.h/.cpp
│       └── json_helper.h/.cpp
└── data/
    └── profiles/
        └── default_0.json
```

### Key Implementation Requirements

#### Matrix Scanning (matrix.cpp)
```cpp
class KeyMatrix {
private:
    uint8_t rowPins[3] = {16, 17, 18};
    uint8_t colPins[4] = {21, 22, 23, 19};
    bool keyStates[12];
    uint32_t lastDebounceTime[12];
    uint32_t debounceDelay = 5;  // ms
    
public:
    void init();
    void scan();  // Call in loop, fast as possible
    bool isPressed(uint8_t key);
    bool justPressed(uint8_t key);
    bool justReleased(uint8_t key);
};

// Implementation notes:
// - Use INPUT_PULLUP on column pins
// - Drive rows LOW one at a time
// - Read columns (LOW = pressed)
// - Debounce: require stable state for debounceDelay ms
// - Track previous state for edge detection
```

#### Encoder Handling (encoder.cpp)
```cpp
class RotaryEncoder {
private:
    uint8_t pinA, pinB, pinSW;
    int8_t position;
    bool swPressed;
    uint32_t lastTurnTime;
    float acceleration = 1.0;
    
public:
    void init(uint8_t a, uint8_t b, uint8_t sw);
    void update();  // Call frequently (ISR or fast polling)
    int8_t getDelta();  // Returns steps since last call
    bool isSWPressed();
    bool isSWJustPressed();
    float getAcceleration();  // 1.0 - 5.0 based on turn speed
};

// Implementation notes:
// - Use gray code state machine for reliable direction
// - Acceleration: faster turns (< 50ms between) increase multiplier
// - ISR on pinA/pinB changes OR poll at 1kHz minimum
// - Debounce SW button
```

#### Profile Manager (profile_manager.cpp)
```cpp
class ProfileManager {
private:
    Profile profiles[8];
    uint8_t activeProfile = 0;
    LittleFS storage;
    
public:
    bool loadProfile(uint8_t id);
    bool saveProfile(uint8_t id, const Profile& profile);
    bool setActiveProfile(uint8_t id);
    uint8_t getActiveProfile();
    Profile* getCurrentProfile();
    void factoryReset();
};

// File naming: /profiles/profile_0.json to profile_7.json
// Atomic writes: write to .tmp, then rename
// Load at boot: restore last active from NVS/Preferences
```

#### BLE Implementation (ble_hid.cpp + ble_config.cpp)
```cpp
// Use NimBLE for lower memory footprint
// Two separate services:
// 1. Standard HID (keyboard/mouse/consumer)
// 2. Custom GATT for config

class BLEKeyboard {
public:
    void begin(const char* deviceName);
    void sendKeyPress(uint8_t keycode);
    void sendKeyCombo(uint8_t* modifiers, uint8_t key);
    void sendConsumerControl(uint16_t code);  // Media keys
    void sendMouseClick(uint8_t button);
};

class BLEConfigService {
private:
    BLECharacteristic* cmdChar;
    BLECharacteristic* evtChar;
    BLECharacteristic* bulkChar;
    String rxBuffer;  // For multi-packet messages
    
public:
    void begin();
    void sendEvent(const String& jsonEvent);
    void handleCommand(const String& jsonCmd);
    void setCommandCallback(void (*cb)(const String&));
};

// Chunking:
// - Split messages > 512 bytes
// - First packet: {"chunk":0, "total":3, "data":"..."}
// - Reassemble on both sides
```

#### WiFi WebSocket (wifi_manager.cpp + websocket_server.cpp)
```cpp
class WiFiManager {
public:
    void startAP(const char* ssid, const char* password);
    void connectSTA(const char* ssid, const char* password);
    bool isConnected();
    String getIP();
};

class WebSocketServer {
private:
    AsyncWebSocket ws;
    
public:
    void begin(uint16_t port);
    void broadcast(const String& message);
    void sendTo(uint32_t clientId, const String& message);
    void onMessage(void (*callback)(const String&));
};

// Use AsyncWebSocket from ESPAsyncWebServer
// Enable mDNS: micropad.local
```

#### Power Management (power_manager.cpp)
```cpp
class PowerManager {
private:
    uint32_t lastActivityTime;
    uint32_t sleepTimeout = 300000;  // 5 min
    bool isAsleep = false;
    
public:
    void init();
    void tickActivity();  // Call on any input
    void update();        // Call in loop
    void sleep();
    void wake();
    
    // Implementation:
    // - Reduce BLE advertising interval when idle
    // - Slow down matrix scan (1Hz vs 1kHz)
    // - Light sleep between scans
    // - Wake on any GPIO interrupt (if possible)
};
```

#### Action Executor (action_executor.cpp)
```cpp
class ActionExecutor {
public:
    void execute(const Action& action);
    void executeHotkey(const HotkeyAction& action);
    void executeMacro(const MacroAction& action);
    void executeText(const TextAction& action);
    void executeMedia(const MediaAction& action);
    // ... etc
    
private:
    BLEKeyboard* bleKeyboard;
    ProfileManager* profileManager;
};

// Macro playback:
// - Non-blocking (use millis() timing)
// - Pause between steps
// - Allow abort on new input
```

### Firmware Boot Sequence
```cpp
void setup() {
    Serial.begin(115200);
    
    // 1. Initialize storage
    LittleFS.begin();
    
    // 2. Load preferences (last profile, WiFi creds)
    preferences.begin("micropad", false);
    uint8_t lastProfile = preferences.getUChar("profile", 0);
    
    // 3. Initialize hardware
    matrix.init();
    encoder1.init(32, 33, 27);
    encoder2.init(25, 26, 13);
    
    // 4. Load active profile
    profileManager.loadProfile(lastProfile);
    
    // 5. Start BLE
    bleKeyboard.begin("Micropad");
    bleConfig.begin();
    bleConfig.setCommandCallback(handleBLECommand);
    
    // 6. Start WiFi (if configured)
    if (preferences.getBool("wifiEnabled", false)) {
        String ssid = preferences.getString("wifiSSID", "");
        String pass = preferences.getString("wifiPass", "");
        wifiManager.connectSTA(ssid.c_str(), pass.c_str());
        wsServer.begin(8765);
    }
    
    // 7. Start mDNS
    MDNS.begin("micropad");
    MDNS.addService("ws", "tcp", 8765);
    
    Serial.println("Micropad ready!");
}

void loop() {
    // High-priority input polling
    matrix.scan();
    encoder1.update();
    encoder2.update();
    
    // Process input events
    processKeys();
    processEncoders();
    
    // Handle communication
    bleConfig.update();
    wsServer.update();
    
    // Power management
    powerManager.update();
    
    // Stats collection
    statsCollector.update();
}
```

### Default Profile (Slot 0)
```json
{
  "id": 0,
  "name": "General",
  "keys": [
    {"index": 0, "type": "hotkey", "config": {"modifiers": ["ctrl"], "key": "c"}},
    {"index": 1, "type": "hotkey", "config": {"modifiers": ["ctrl"], "key": "v"}},
    {"index": 2, "type": "hotkey", "config": {"modifiers": ["ctrl"], "key": "z"}},
    {"index": 3, "type": "hotkey", "config": {"modifiers": ["ctrl"], "key": "y"}},
    {"index": 4, "type": "hotkey", "config": {"modifiers": ["alt"], "key": "tab"}},
    {"index": 5, "type": "hotkey", "config": {"modifiers": ["win"], "key": "d"}},
    {"index": 6, "type": "hotkey", "config": {"modifiers": ["win", "shift"], "key": "s"}},
    {"index": 7, "type": "app", "config": {"path": "explorer.exe"}},
    {"index": 8, "type": "media", "config": {"function": "prev"}},
    {"index": 9, "type": "media", "config": {"function": "playPause"}},
    {"index": 10, "type": "media", "config": {"function": "next"}},
    {"index": 11, "type": "profile", "config": {"profileId": 1}}
  ],
  "encoders": [
    {
      "index": 0,
      "cwAction": {"type": "media", "config": {"function": "volumeUp"}},
      "ccwAction": {"type": "media", "config": {"function": "volumeDown"}},
      "pressAction": {"type": "media", "config": {"function": "mute"}}
    },
    {
      "index": 1,
      "cwAction": {"type": "mouse", "config": {"action": "scrollDown", "value": 3}},
      "ccwAction": {"type": "mouse", "config": {"action": "scrollUp", "value": 3}},
      "pressAction": {"type": "media", "config": {"function": "playPause"}}
    }
  ]
}
```

---

## Windows Application (WPF .NET 8)

### Project Structure
```
Micropad.sln
├── Micropad.App/              # WPF UI project
│   ├── App.xaml
│   ├── MainWindow.xaml
│   ├── Views/
│   │   ├── DevicesView.xaml
│   │   ├── ProfilesView.xaml
│   │   ├── MacrosView.xaml
│   │   ├── StatsView.xaml
│   │   └── SettingsView.xaml
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   ├── DevicesViewModel.cs
│   │   ├── ProfilesViewModel.cs
│   │   └── ...
│   ├── Controls/
│   │   ├── KeyGridControl.xaml
│   │   ├── KeyButton.xaml
│   │   └── MacroEditor.xaml
│   ├── Converters/
│   ├── Styles/
│   │   └── DarkTheme.xaml
│   └── Resources/
│
├── Micropad.Core/             # Shared models & logic
│   ├── Models/
│   │   ├── Profile.cs
│   │   ├── Action.cs
│   │   ├── DeviceInfo.cs
│   │   └── ProtocolMessage.cs
│   ├── Enums/
│   │   ├── ActionType.cs
│   │   └── KeyBehavior.cs
│   └── Interfaces/
│       ├── IDeviceConnection.cs
│       └── IProfileStorage.cs
│
├── Micropad.Services/         # Business logic
│   ├── Communication/
│   │   ├── BleConnection.cs
│   │   ├── WifiConnection.cs
│   │   └── ProtocolHandler.cs
│   ├── Input/
│   │   ├── MacroRecorder.cs
│   │   ├── KeyInjector.cs
│   │   └── GlobalHooks.cs
│   ├── Automation/
│   │   ├── ForegroundMonitor.cs
│   │   └── ProfileSwitcher.cs
│   ├── Storage/
│   │   └── LocalProfileStorage.cs
│   └── Statistics/
│       └── StatsCollector.cs
│
└── Micropad.Tests/
    └── ProtocolTests.cs
```

### Required NuGet Packages
```xml
<!-- Micropad.App -->
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
<PackageReference Include="Wpf.Ui" Version="3.0.4" />
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />

<!-- Micropad.Services -->
<PackageReference Include="Windows.Devices.Bluetooth" Version="10.0.19041.31" />
<PackageReference Include="Websocket.Client" Version="5.0.0" />
<PackageReference Include="Zeroconf" Version="3.6.11" />  <!-- mDNS discovery -->
```

### UI Theme Specifications

#### Color Palette
```xaml
<Color x:Key="BackgroundPrimary">#2B2F33</Color>
<Color x:Key="BackgroundSecondary">#353A40</Color>
<Color x:Key="BackgroundTertiary">#3E4349</Color>
<Color x:Key="BorderColor">#4A5157</Color>
<Color x:Key="TextPrimary">#E8E8E8</Color>
<Color x:Key="TextSecondary">#B0B0B0</Color>
<Color x:Key="AccentBlue">#0078D4</Color>
<Color x:Key="AccentHover">#1084D8</Color>
<Color x:Key="Success">#28A745</Color>
<Color x:Key="Warning">#FFC107</Color>
<Color x:Key="Error">#DC3545</Color>
```

#### Typography
```xaml
<FontFamily x:Key="MainFont">Segoe UI</FontFamily>
<FontFamily x:Key="MonoFont">Cascadia Code, Consolas</FontFamily>

<system:Double x:Key="FontSizeSmall">11</system:Double>
<system:Double x:Key="FontSizeNormal">13</system:Double>
<system:Double x:Key="FontSizeMedium">15</system:Double>
<system:Double x:Key="FontSizeLarge">18</system:Double>
<system:Double x:Key="FontSizeTitle">24</system:Double>
```

#### Animations
```xaml
<!-- Fade in -->
<Storyboard x:Key="FadeIn">
    <DoubleAnimation Storyboard.TargetProperty="Opacity"
                     From="0" To="1" Duration="0:0:0.2" />
</Storyboard>

<!-- Slide from right -->
<Storyboard x:Key="SlideFromRight">
    <DoubleAnimation Storyboard.TargetProperty="RenderTransform.(TranslateTransform.X)"
                     From="50" To="0" Duration="0:0:0.3">
        <DoubleAnimation.EasingFunction>
            <QuadraticEase EasingMode="EaseOut"/>
        </DoubleAnimation.EasingFunction>
    </DoubleAnimation>
</Storyboard>

<!-- Hover glow (subtle) -->
<Storyboard x:Key="HoverGlow">
    <ColorAnimation Storyboard.TargetProperty="(Border.BorderBrush).(SolidColorBrush.Color)"
                    To="#1084D8" Duration="0:0:0.15" />
</Storyboard>
```

### Key UI Components

#### Main Window Layout
```
┌─────────────────────────────────────────────┐
│  [Micropad]                    [-] [□] [×]  │
├──────────┬──────────────────────────────────┤
│          │                                   │
│ Devices  │         Content Area              │
│ Profiles │                                   │
│ Macros   │                                   │
│ Stats    │                                   │
│ Settings │                                   │
│          │                                   │
│          │                                   │
│          │                                   │
├──────────┴──────────────────────────────────┤
│ Status: Connected | Battery: 85% | v1.0.0   │
└─────────────────────────────────────────────┘
```

#### Key Grid Control (ProfilesView)
```xaml
<Grid x:Name="KeyGrid" Margin="20">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
        <RowDefinition Height="*"/>
        <RowDefinition Height="*"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    
    <!-- Each key is a KeyButton custom control -->
    <local:KeyButton Grid.Row="0" Grid.Column="0" 
                     KeyIndex="0"
                     AssignedAction="{Binding Profile.Keys[0]}"
                     AllowDrop="True"
                     Drop="KeyButton_Drop"/>
    <!-- ... 11 more keys -->
</Grid>

<!-- Encoders shown separately above/below grid -->
```

#### KeyButton Control (Custom)
```xaml
<Border x:Name="Border"
        Background="{StaticResource BackgroundSecondary}"
        BorderBrush="{StaticResource BorderColor}"
        BorderThickness="1"
        CornerRadius="8"
        Padding="10"
        Cursor="Hand">
    <StackPanel>
        <TextBlock Text="{Binding KeyName}" 
                   FontSize="11"
                   Foreground="{StaticResource TextSecondary}"/>
        <TextBlock Text="{Binding AssignedAction.DisplayName}"
                   FontSize="13"
                   FontWeight="SemiBold"
                   Foreground="{StaticResource TextPrimary}"
                   TextWrapping="Wrap"/>
        <ContentPresenter Content="{Binding AssignedAction.Icon}"
                          Margin="0,5,0,0"/>
    </StackPanel>
</Border>

<!-- Behaviors -->
<i:Interaction.Triggers>
    <i:EventTrigger EventName="MouseEnter">
        <i:InvokeCommandAction Command="{Binding HoverCommand}"/>
    </i:EventTrigger>
</i:Interaction.Triggers>
```

#### Macro Editor Dialog
```
┌─────────────────────────────────────┐
│ Edit Macro: "Copy and Search"      │
├─────────────────────────────────────┤
│                                     │
│ Steps:                              │
│ ┌─────────────────────────────────┐ │
│ │ 1. Key Down: Ctrl         [×]   │ │
│ │ 2. Key Press: C           [×]   │ │
│ │ 3. Key Up: Ctrl           [×]   │ │
│ │ 4. Delay: 100ms           [×]   │ │
│ │ 5. Key Press: Win+S       [×]   │ │
│ └─────────────────────────────────┘ │
│                                     │
│ [+Add Step] [Record] [Test]         │
│                                     │
│              [Cancel] [Save]        │
└─────────────────────────────────────┘
```

### Core Service Implementations

#### BLE Connection Service
```csharp
public class BleConnection : IDeviceConnection
{
    private GattDeviceService _configService;
    private GattCharacteristic _cmdChar;
    private GattCharacteristic _evtChar;
    
    public async Task<bool> ConnectAsync(string deviceId)
    {
        var device = await BluetoothLEDevice.FromIdAsync(deviceId);
        var services = await device.GetGattServicesForUuidAsync(ConfigServiceUuid);
        
        _configService = services.Services[0];
        _cmdChar = await GetCharacteristicAsync(CmdCharUuid);
        _evtChar = await GetCharacteristicAsync(EvtCharUuid);
        
        // Subscribe to notifications
        await _evtChar.WriteClientCharacteristicConfigurationDescriptorAsync(
            GattClientCharacteristicConfigurationDescriptorValue.Notify);
        _evtChar.ValueChanged += OnNotificationReceived;
        
        return true;
    }
    
    public async Task SendCommandAsync(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        
        // Chunk if needed
        if (bytes.Length > 512)
        {
            await SendChunked(bytes);
        }
        else
        {
            await _cmdChar.WriteValueAsync(bytes.AsBuffer());
        }
    }
    
    private void OnNotificationReceived(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        var json = Encoding.UTF8.GetString(args.CharacteristicValue.ToArray());
        MessageReceived?.Invoke(this, json);
    }
}
```

#### Macro Recorder Service
```csharp
public class MacroRecorder
{
    private LowLevelKeyboardProc _keyboardHookProc;
    private LowLevelMouseProc _mouseHookProc;
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private IntPtr _mouseHookId = IntPtr.Zero;
    
    private List<MacroStep> _recordedSteps = new();
    private Stopwatch _timer = new();
    private bool _isRecording = false;
    
    public void StartRecording()
    {
        _recordedSteps.Clear();
        _timer.Restart();
        _isRecording = true;
        
        // Install hooks
        _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardHookProc, IntPtr.Zero, 0);
        _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, IntPtr.Zero, 0);
    }
    
    public List<MacroStep> StopRecording()
    {
        _isRecording = false;
        UnhookWindowsHookEx(_keyboardHookId);
        UnhookWindowsHookEx(_mouseHookId);
        
        // Convert timestamps to delays
        return ProcessSteps(_recordedSteps);
    }
    
    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _isRecording)
        {
            var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var step = new MacroStep
            {
                Type = wParam == WM_KEYDOWN ? "keyDown" : "keyUp",
                Key = ((Keys)kb.vkCode).ToString(),
                Timestamp = _timer.ElapsedMilliseconds
            };
            _recordedSteps.Add(step);
        }
        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }
    
    // Similar for mouse hook...
}
```

#### Foreground App Monitor
```csharp
public class ForegroundMonitor
{
    private DispatcherTimer _timer;
    private string _lastProcessName = "";
    private Dictionary<string, int> _processToProfile = new();
    
    public event EventHandler<string> ProcessChanged;
    
    public void Start()
    {
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(250);
        _timer.Tick += CheckForeground;
        _timer.Start();
    }
    
    private void CheckForeground(object sender, EventArgs e)
    {
        var hwnd = GetForegroundWindow();
        GetWindowThreadProcessId(hwnd, out var processId);
        
        var process = Process.GetProcessById((int)processId);
        var processName = process.ProcessName;
        
        if (processName != _lastProcessName)
        {
            _lastProcessName = processName;
            ProcessChanged?.Invoke(this, processName);
            
            // Auto-switch profile if mapped
            if (_processToProfile.TryGetValue(processName, out var profileId))
            {
                _ = SwitchProfileAsync(profileId);
            }
        }
    }
    
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
```

#### Profile Sync Service
```csharp
public class ProfileSyncService
{
    private IDeviceConnection _device;
    private LocalProfileStorage _localStorage;
    
    public async Task PushProfileToDevice(Profile profile)
    {
        var json = JsonConvert.SerializeObject(profile);
        var message = new ProtocolMessage
        {
            Version = 1,
            Type = "request",
            Id = GenerateId(),
            Payload = new { cmd = "setProfile", profile }
        };
        
        await _device.SendCommandAsync(JsonConvert.SerializeObject(message));
        
        // Wait for response
        var response = await WaitForResponse(message.Id, timeout: 5000);
        if (response.Payload.success == false)
        {
            throw new Exception($"Failed to push profile: {response.Payload.error}");
        }
    }
    
    public async Task<Profile> PullProfileFromDevice(int profileId)
    {
        var message = new ProtocolMessage
        {
            Version = 1,
            Type = "request",
            Id = GenerateId(),
            Payload = new { cmd = "getProfile", profileId }
        };
        
        await _device.SendCommandAsync(JsonConvert.SerializeObject(message));
        var response = await WaitForResponse(message.Id, timeout: 5000);
        
        return JsonConvert.DeserializeObject<Profile>(response.Payload.profile.ToString());
    }
    
    public async Task SyncAll()
    {
        // Pull all device profiles
        var deviceProfiles = await GetDeviceProfileList();
        
        // Push local profiles that don't exist on device
        var localProfiles = _localStorage.GetAllProfiles();
        
        foreach (var local in localProfiles)
        {
            if (!deviceProfiles.Any(d => d.Id == local.Id))
            {
                await PushProfileToDevice(local);
            }
        }
    }
}
```

### Application Boot Sequence
```csharp
public partial class App : Application
{
    private IHost _host;
    
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Logging
                services.AddSerilog(config => config
                    .WriteTo.File("logs/micropad.log", rollingInterval: RollingInterval.Day));
                
                // Core services
                services.AddSingleton<IDeviceConnection, BleConnection>();
                services.AddSingleton<LocalProfileStorage>();
                services.AddSingleton<ProfileSyncService>();
                services.AddSingleton<MacroRecorder>();
                services.AddSingleton<ForegroundMonitor>();
                services.AddSingleton<StatsCollector>();
                
                // ViewModels
                services.AddTransient<MainViewModel>();
                services.AddTransient<DevicesViewModel>();
                services.AddTransient<ProfilesViewModel>();
                services.AddTransient<MacrosViewModel>();
                services.AddTransient<StatsViewModel>();
                services.AddTransient<SettingsViewModel>();
                
                // Main window
                services.AddSingleton<MainWindow>();
            })
            .Build();
        
        await _host.StartAsync();
        
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
    
    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
```

---

## Pre-Built Profile Templates

### Template 1: VS Code
```json
{
  "name": "VS Code",
  "keys": [
    {"index": 0, "type": "hotkey", "config": {"modifiers": ["ctrl"], "key": "s"}},
    {"index": 1, "type": "hotkey", "config": {"modifiers": ["ctrl", "shift"], "key": "f"}},
    {"index": 2, "type": "hotkey", "config": {"modifiers": ["ctrl"], "key": "p"}},
    {"index": 3, "type": "hotkey", "config": {"modifiers": ["ctrl", "shift"], "key": "p"}},
    {"index": 4, "type": "hotkey", "config": {"key": "f5"}},
    {"index": 5, "type": "hotkey", "config": {"modifiers": ["ctrl", "shift"], "key": "c"}},
    {"index": 6, "type": "hotkey", "config": {"modifiers": ["ctrl"], "key": "/"}},
    {"index": 7, "type": "hotkey", "config": {"modifiers": ["ctrl", "shift"], "key": "k"}},
    {"index": 8, "type": "text", "config": {"text": "console.log();", "append": ""}},
    {"index": 9, "type": "hotkey", "config": {"modifiers": ["ctrl"], "key": "b"}},
    {"index": 10, "type": "hotkey", "config": {"modifiers": ["ctrl"], "key": "`"}},
    {"index": 11, "type": "hotkey", "config": {"modifiers": ["alt", "shift"], "key": "f"}}
  ]
}
```

### Template 2: Adobe Premiere Pro
```json
{
  "name": "Premiere Pro",
  "keys": [
    {"index": 0, "type": "hotkey", "config": {"key": "c"}},  // Cut
    {"index": 1, "type": "hotkey", "config": {"modifiers": ["shift"], "key": "delete"}},  // Ripple delete
    {"index": 2, "type": "hotkey", "config": {"key": "m"}},  // Add marker
    {"index": 3, "type": "hotkey", "config": {"modifiers": ["ctrl"], "key": "k"}},  // Cut at playhead
    {"index": 4, "type": "hotkey", "config": {"key": "l"}},  // Speed up
    {"index": 5, "type": "hotkey", "config": {"key": "j"}},  // Slow down
    {"index": 6, "type": "hotkey", "config": {"key": "space"}},  // Play/pause
    {"index": 7, "type": "hotkey", "config": {"modifiers": ["ctrl"], "key": "z"}},  // Undo
    {"index": 8, "type": "hotkey", "config": {"key": "i"}},  // In point
    {"index": 9, "type": "hotkey", "config": {"key": "o"}},  // Out point
    {"index": 10, "type": "hotkey", "config": {"modifiers": ["ctrl"], "key": "s"}},  // Save
    {"index": 11, "type": "hotkey", "config": {"modifiers": ["ctrl", "shift"], "key": "h"}}  // Export
  ],
  "encoders": [
    {
      "index": 0,
      "cwAction": {"type": "hotkey", "config": {"key": "="}}  // Zoom in timeline
    },
    {
      "index": 1,
      "cwAction": {"type": "hotkey", "config": {"key": "right"}}  // Scrub forward
    }
  ]
}
```

---

## Build & Deployment

### Firmware Build
```bash
# Using Arduino IDE:
1. Install ESP32 board support (v2.0.14+)
2. Install libraries:
   - NimBLE-Arduino
   - ArduinoJson
   - ESPAsyncWebServer
   - AsyncTCP
   - LittleFS_esp32
3. Open firmware/src/main.cpp
4. Set board: "ESP32 Dev Module"
5. Upload

# Or using PlatformIO:
cd firmware
pio run -t upload
pio device monitor -b 115200
```

### Windows App Build
```bash
cd windows-app
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# Output: bin/Release/net8.0-windows/win-x64/publish/Micropad.exe
```

### Installer (Optional)
```bash
# Using Inno Setup or Advanced Installer
# Or MSIX packaging for Microsoft Store

dotnet pack -c Release
# Creates NuGet package for distribution
```

---

## Testing & Quality Gates

### Firmware Tests
- [ ] Matrix scan reliability (no ghost keys, no stuck keys)
- [ ] Encoder direction correct, no missed steps at normal speed
- [ ] Debouncing effective (5-10ms stable state required)
- [ ] Profile switching via combo works offline
- [ ] BLE reconnect after PC sleep/wake
- [ ] WiFi reconnect after router reboot
- [ ] Profile writes are atomic (no corruption on power loss)
- [ ] Power management reduces idle current
- [ ] All 12 keys + 4 encoder actions work simultaneously
- [ ] Macro playback timing accurate (±10ms)

### Windows App Tests
- [ ] BLE pairing wizard completes successfully
- [ ] WiFi discovery finds device via mDNS
- [ ] Drag-drop key assignment works
- [ ] Macro recording captures all events
- [ ] Macro editor timeline displays correctly
- [ ] Foreground app detection responds within 500ms
- [ ] Profile sync (push/pull) preserves all data
- [ ] Stats collection doesn't affect performance
- [ ] App handles device disconnect gracefully
- [ ] No memory leaks during extended use (run 24h)

### Integration Tests
- [ ] Profile change on device updates app UI
- [ ] Profile change in app updates device immediately
- [ ] Per-app auto-switching triggers within 500ms
- [ ] Macro execution matches recorded timing
- [ ] All 14 input sources work (12 keys + 2 encoders)
- [ ] Simultaneous BLE HID + config channel works
- [ ] Large profile transfers (>4KB) complete successfully
- [ ] Battery level updates in app UI

---

## Documentation Requirements

### README.md
```markdown
# Micropad - Professional Wireless Macropad

## Hardware Requirements
- Wemos D1 Mini ESP32
- 12× mechanical switches (Cherry MX compatible)
- 2× rotary encoders with push
- Custom PCB (see /hardware folder)

## Quick Start
1. Flash firmware: `pio run -t upload`
2. Install Windows app: Run Micropad.exe
3. Pair device via BLE
4. Start customizing!

## Features
- Wireless (BLE + WiFi)
- 8 on-device profiles
- Macro recording & playback
- Per-app auto profile switching
- Real-time stats
- Low latency (<10ms)

## Troubleshooting
...
```

### User Manual (in-app Help section)
- First-time setup wizard
- How to create profiles
- Macro recording tutorial
- Per-app switching setup
- Troubleshooting common issues

---

## Success Criteria

### Must Have (MVP)
✅ 12 keys + 2 encoders all functional
✅ BLE HID input works reliably
✅ 4+ profiles stored on-device
✅ Profile switching via two-key combo
✅ Windows app connects and syncs profiles
✅ Key mapping via drag-drop
✅ Macro recording and playback
✅ Per-app auto profile switching
✅ Dark theme UI

### Should Have (V1.1)
- WiFi config channel working
- 8 profiles supported
- Layer support (4 layers per profile)
- Advanced key behaviors (tap/hold/double)
- Encoder acceleration
- Stats dashboard
- Profile import/export

### Could Have (Future)
- OTA firmware updates
- Cloud profile sync
- Mobile app (Android/iOS)
- RGB lighting control
- Wireless charging
- Community profile library
- Plugin system

---

## IMPLEMENTATION PRIORITY ORDER

### Phase 1: Core Input (Week 1)
1. Matrix scanning + debouncing
2. Encoder handling
3. BLE HID keyboard
4. Basic profile structure
5. Serial debugging

### Phase 2: Storage & Profiles (Week 2)
1. LittleFS profile storage
2. Profile manager
3. Action executor
4. Default profile
5. Two-key profile switching

### Phase 3: Communication (Week 3)
1. BLE GATT config service
2. Protocol handler
3. WebSocket server (WiFi)
4. mDNS discovery

### Phase 4: Windows App Foundation (Week 4)
1. Project structure + DI
2. BLE connection service
3. Main window UI
4. Device list view
5. Profile editor grid

### Phase 5: Profile Management (Week 5)
1. Drag-drop key assignment
2. Action type dialogs
3. Profile sync service
4. Local storage
5. Import/export

### Phase 6: Macros (Week 6)
1. Global hooks (keyboard/mouse)
2. Macro recorder
3. Timeline editor UI
4. Macro player (device-side)
5. Macro player (app-side fallback)

### Phase 7: Automation (Week 7)
1. Foreground app monitor
2. Process-to-profile mapping UI
3. Auto-switching logic
4. Stats collection

### Phase 8: Polish (Week 8)
1. Animations & transitions
2. Error handling & validation
3. Power management
4. Stats dashboard
5. Settings page
6. Documentation
7. Testing & bug fixes

---

## CODE STYLE & CONVENTIONS

### C++ (Firmware)
```cpp
// File naming: lowercase_with_underscores.cpp
// Class naming: PascalCase
// Function naming: camelCase
// Constants: UPPER_CASE
// Private members: _camelCase prefix

class ProfileManager {
private:
    Profile _profiles[MAX_PROFILES];
    uint8_t _activeProfile;
    
public:
    bool loadProfile(uint8_t id);
    Profile* getCurrentProfile();
};

// Use const references for read-only params
void processAction(const Action& action);

// Prefer explicit types
uint32_t timestamp = millis();  // Not: auto timestamp = millis();
```

### C# (Windows App)
```csharp
// File naming: PascalCase.cs
// Class/Interface naming: PascalCase
// Method naming: PascalCase
// Private fields: _camelCase
// Properties: PascalCase
// Local variables: camelCase

public class ProfileManager
{
    private readonly IDeviceConnection _connection;
    private ObservableCollection<Profile> _profiles;
    
    public async Task<Profile> LoadProfileAsync(int id)
    {
        // Implementation
    }
}

// Use async/await consistently
// Use nullable reference types
public Profile? FindProfile(int id) { }

// Use LINQ where appropriate
var activeProfiles = profiles.Where(p => p.IsActive).ToList();
```

---

## FINAL NOTES

This is a comprehensive specification for building a professional-grade wireless macropad system. The implementation should follow this order:

1. **Start with firmware basics** - Get input working reliably first
2. **Add storage & profiles** - Make device functional standalone
3. **Implement communication** - Connect device to PC
4. **Build Windows app foundation** - Basic UI and connection
5. **Add profile management** - Sync and editing capabilities
6. **Implement macros** - Recording and playback
7. **Add automation** - Per-app switching and stats
8. **Polish everything** - UI, UX, error handling, documentation

**Priority**: Reliability > Features. A macropad that misses keystrokes or has laggy input is useless, regardless of how many features it has.

**Testing**: Test continuously during development. Don't wait until the end to discover the matrix scanning isn't reliable or BLE drops connections.

**Documentation**: Write docs as you go. Future you (and users) will thank you.

Good luck! 🚀
