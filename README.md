# Micropad

A wireless BLE macropad with 12 mechanical keys and 2 rotary encoders. Configure it from your browser.

## What's in the box

| Component | Description |
|-----------|-------------|
| **Firmware** (`firmware/`) | ESP32 Arduino sketch — BLE HID keyboard/mouse/media + configuration service |
| **Web App** (`web-app/`) | Browser-based configuration app using Web Bluetooth (React + TypeScript) |
| **Windows App** (`windows-app/`) | .NET 8 WPF companion app (optional) |
| **macOS App** (`macos-app/`) | SwiftUI companion app (optional, partial) |

## Quick Start

### 1. Flash the firmware

1. Open `firmware/Micropad/Micropad.ino` in Arduino IDE
2. Select board: **ESP32 Dev Module**
3. Partition scheme: **Huge APP (3MB No OTA / 1MB SPIFFS)**
4. Install libraries: **NimBLE-Arduino**, **ArduinoJson**, **LittleFS**
5. Flash to your ESP32

### 2. Use the web app

1. Go to the hosted web app (or run locally with `cd web-app && npm install && npm run dev`)
2. Open in **Chrome**, **Edge**, or **Opera** (Web Bluetooth required)
3. Click **Connect Micropad** and select your device from the browser picker
4. Go to **Profiles** to assign keys and encoders
5. Click **Save to device** to push your configuration

### 3. Pair with your PC

The Micropad appears as a Bluetooth keyboard. Pair it in Windows/macOS Bluetooth settings. Once paired, key presses and encoder actions are sent to your PC.

The device supports **two simultaneous BLE connections**: one for HID (keyboard input to PC) and one for configuration (browser app). You can configure the Micropad while using it.

## Hardware

- **MCU:** ESP32 (Wemos D1 Mini ESP32)
- **Keys:** 12 mechanical switches in a 3×4 matrix
- **Encoders:** 2 rotary encoders with push buttons
- **Communication:** BLE 4.2+ (NimBLE)
- **Storage:** LittleFS on internal flash

### Pin Configuration

| Function | Pins |
|----------|------|
| Matrix Rows | GPIO 16, 17, 18 |
| Matrix Cols | GPIO 21, 22, 23, 19 |
| Encoder 1 | A=32, B=33, SW=27 |
| Encoder 2 | A=25, B=26, SW=13 |

## Features

### Supported Actions
- **Keyboard shortcuts** — any key + Ctrl/Shift/Alt/Win modifiers
- **Text typing** — type strings (letters, numbers, basic punctuation)
- **Media controls** — volume, play/pause, next/previous track, mute, stop
- **Mouse actions** — left/right/middle click, scroll up/down
- **Profile switching** — switch between profiles from a key press
- **Macros** — sequences of key presses, text, delays, and media keys (up to 16 steps)

### Profile System
- Up to 8 profiles stored on device
- Create, rename, duplicate, delete profiles
- Import/export profiles as .zip backups
- Active profile persists across reboots

### Encoder Support
- Clockwise, counter-clockwise, and press actions per encoder
- Quick presets: Volume, Scroll, Zoom, Media

### Not Supported (Yet)
- **Layers** — firmware does not support layer switching
- **App Launch / URL Open** — requires a companion app on the host PC
- **Advanced combos** — tap/hold/double-tap behaviors

These features are hidden from the UI to avoid confusion.

## Architecture

```
ESP32 Firmware
├── BLE HID Service (0x1812) — keyboard, media, mouse reports
├── BLE Config Service (4fafc201-...) — JSON protocol over CMD/EVT characteristics
├── Profile Manager — LittleFS storage, up to 8 profiles
├── Action Executor — executes hotkey, text, media, mouse, macro actions
├── Key Matrix — 3×4 matrix scan with debounce
└── Encoders — 2 rotary encoders with acceleration

Web App (Browser)
├── Web Bluetooth — connects to config service
├── Protocol Handler — JSON request/response with chunked message support
├── Profile Editor — visual key grid, encoder presets
├── Macro Editor — step-by-step macro builder
└── IndexedDB — local profile/macro storage
```

## Protocol

JSON messages over BLE GATT. The config service uses three characteristics:

| Characteristic | UUID | Direction | Purpose |
|---------------|------|-----------|---------|
| CMD | `...914c` | App → Device | Commands (or chunked parts) |
| EVT | `...914d` | Device → App | Responses and events (notify) |
| BULK | `...914e` | App → Device | Large writes (same handler as CMD) |

Messages larger than 512 bytes are chunked with base64 encoding:
```json
{"chunk": 0, "total": 3, "dataB64": "eyJ2IjoxLC..."}
```

### Commands
- `getDeviceInfo` — device ID, firmware version, battery, uptime
- `getCaps` — max profiles, free bytes, supported features and action types
- `listProfiles` — list all profiles on device
- `getProfile` — get full profile with keys and encoder actions
- `setProfile` — save profile to device (deferred processing)
- `setActiveProfile` / `getActiveProfile` — switch active profile
- `deleteProfile` — delete a profile
- `getStats` — key press counts, encoder turn counts, uptime
- `getConnectionStatus` — config/HID connection state
- `factoryReset` / `reboot` — device management

## Connection Model

The Micropad supports two simultaneous BLE connections via NimBLE:

1. **HID Host** (PC/Mac) — subscribes to HID report characteristics
2. **Config Client** (Browser) — writes to CMD characteristic

After the first connection, the device restarts advertising so a second client can connect. The firmware distinguishes HID hosts (clients that subscribe to report notifications) from config clients (clients that write to CMD).

### Connection States
- **Not connected** — device is advertising, waiting for connections
- **Config only** — browser connected for configuration, no HID host
- **HID + Config** — both PC and browser connected simultaneously
- **HID ready** — PC keyboard is active and config channel available

## Development

### Firmware
```bash
# Open in Arduino IDE
# Board: ESP32 Dev Module
# Partition: Huge APP (3MB No OTA / 1MB SPIFFS)
```

### Web App
```bash
cd web-app
npm install
npm run dev     # development server
npm run build   # production build
npm test        # run tests
```

## Troubleshooting

- **Device not appearing in browser picker:** Make sure the Micropad is powered on and not already connected to two devices. Try turning it off and on.
- **"Device busy" error:** The Micropad may already be connected to the PC. Use the **Reconnect** button instead of **Connect Micropad**, or disconnect from Windows Bluetooth first.
- **Keys not working after connecting browser:** The HID host (PC) must subscribe to HID reports. This happens automatically when paired via Windows/macOS Bluetooth.
- **Profile save fails:** Large profiles with many macros may take longer. The device processes saves in the main loop to avoid BLE timeouts.
- **Firmware crashes:** Check serial output at 115200 baud. Set `DEBUG_ENABLED true` in `config.h` for verbose logging.

## License

See individual component directories for license information.
