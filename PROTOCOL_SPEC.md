# Micropad BLE Protocol Specification v1.1

## Overview

JSON-based request/response protocol over BLE GATT. The firmware exposes a custom config service alongside the standard HID service, allowing configuration while the device is in use.

## GATT Services

### Config Service
| UUID | Characteristic | Properties | Purpose |
|------|---------------|------------|---------|
| `4fafc201-1fb5-459e-8fcc-c5c9c331914b` | Service | — | Config service |
| `4fafc201-1fb5-459e-8fcc-c5c9c331914c` | CMD | Write, Write NR | JSON commands (or chunk frames) |
| `4fafc201-1fb5-459e-8fcc-c5c9c331914d` | EVT | Notify | JSON responses/events |
| `4fafc201-1fb5-459e-8fcc-c5c9c331914e` | BULK | Write, Write NR | Large writes (same handler as CMD) |

### HID Service (0x1812)
Standard BLE HID with keyboard (Report ID 1), consumer/media (Report ID 2), and mouse (Report ID 3) input reports.

## Message Envelope

### Request (App → Device)
```json
{
  "v": 1,
  "type": "request",
  "id": 42,
  "cmd": "getProfile",
  "profileId": 0
}
```

### Response (Device → App)
```json
{
  "v": 1,
  "type": "response",
  "id": 42,
  "ts": 12345,
  "payload": { ... }
}
```

### Event (Device → App, unsolicited)
```json
{
  "v": 1,
  "type": "event",
  "event": "profileChanged",
  "ts": 12345,
  "payload": { "profileId": 1 }
}
```

## Chunked Transport

Messages larger than 512 bytes are split into chunks.

### App → Device (base64 encoded)
```json
{"chunk": 0, "total": 3, "dataB64": "eyJ2IjoxLC4uLn0="}
{"chunk": 1, "total": 3, "dataB64": "Li4u"}
{"chunk": 2, "total": 3, "dataB64": "fQ=="}
```
- Chunk payload size: ~80 bytes
- Delay between chunks: 300ms
- Retries on GATT errors: up to 2

### Device → App (base64 encoded)
```json
{"chunk": 0, "total": 2, "dataB64": "eyJ2IjoxLC4uLn0="}
{"chunk": 1, "total": 2, "dataB64": "fQ=="}
```
- Raw chunk size: ~360 bytes (480 after base64)
- Small delay between chunks via yield()
- No acknowledgment (web app uses fixed delay)

## Commands

### getDeviceInfo
Returns device identification and status.

**Response payload:**
```json
{
  "deviceId": "ESP32-abcdef",
  "firmwareVersion": "1.0.0",
  "hardwareVersion": "1.0",
  "batteryLevel": 100,
  "capabilities": ["ble", "profiles", "encoders"],
  "uptime": 3600,
  "freeHeap": 150000
}
```

### getCaps
Returns device capabilities. The `supportedActions` array tells the UI which action types are implemented.

**Response payload:**
```json
{
  "maxProfiles": 8,
  "freeBytes": 512000,
  "supportsLayers": false,
  "supportsMacros": true,
  "supportsEncoders": true,
  "maxKeys": 12,
  "maxEncoders": 2,
  "supportedActions": [0, 1, 2, 3, 4, 5, 7]
}
```

Action type IDs: 0=None, 1=Hotkey, 2=Macro, 3=Text, 4=Media, 5=Mouse, 6=Layer, 7=Profile, 8=App, 9=URL

### listProfiles
**Response payload:**
```json
{
  "profiles": [
    {"id": 0, "name": "General", "size": 1024},
    {"id": 1, "name": "VS Code", "size": 2048}
  ]
}
```

### getProfile
**Request:** `{"cmd": "getProfile", "profileId": 0}`

**Response payload:**
```json
{
  "id": 0,
  "name": "General",
  "version": 1,
  "keys": [
    {"index": 0, "type": 1, "modifiers": 1, "key": 6},
    {"index": 1, "type": 3, "text": "hello"},
    {"index": 2, "type": 4, "function": 0},
    {"index": 3, "type": 5, "action": 0, "value": 0},
    {"index": 4, "type": 7, "profileId": 1},
    {"index": 5, "type": 2, "macroSteps": [
      {"stepType": 1, "delayMs": 100, "key": 0, "modifiers": 0, "text": "", "mediaFunction": 0},
      {"stepType": 2, "delayMs": 0, "key": 4, "modifiers": 1, "text": "", "mediaFunction": 0}
    ]}
  ],
  "encoders": [
    {
      "index": 0,
      "cwAction": {"type": 4, "function": 0},
      "ccwAction": {"type": 4, "function": 1},
      "pressAction": {"type": 4, "function": 2},
      "acceleration": true,
      "stepsPerDetent": 4
    }
  ]
}
```

### setProfile
**Request:** `{"cmd": "setProfile", "profile": {...}}`

Profile object has the same format as `getProfile` response. The firmware defers processing to the main loop to avoid BLE callback timeouts.

**Response payload:** `{"success": true}`

### setActiveProfile / getActiveProfile
**Request:** `{"cmd": "setActiveProfile", "profileId": 1}`
**Response:** `{"profileId": 1, "success": true}`

### deleteProfile
**Request:** `{"cmd": "deleteProfile", "profileId": 1}`
**Response:** `{"success": true}` or error if active/last profile

### getStats
**Response payload:**
```json
{
  "keyPresses": [42, 0, 15, 0, 0, 0, 0, 0, 0, 0, 0, 0],
  "encoderTurns": [128, 56],
  "uptime": 3600,
  "freeHeap": 150000
}
```

### getConnectionStatus
**Response payload:**
```json
{
  "configConnected": true,
  "hidHostConnected": true,
  "hidReady": true,
  "advertising": true,
  "clientCount": 2,
  "canAcceptConfigConnection": true,
  "reason": "fully_connected"
}
```

Reason values: `fully_connected`, `config_and_hid`, `config_only`, `hid_only`, `not_connected`

### factoryReset
Erases all profiles and restores defaults.

### reboot
Restarts the device.

## Action Types

| ID | Name | Config Fields |
|----|------|--------------|
| 0 | None | — |
| 1 | Hotkey | `modifiers`, `key` (HID key code) |
| 2 | Macro | `macroSteps[]` (embedded step list) |
| 3 | Text | `text` (max 127 chars) |
| 4 | Media | `function` (0=VolUp, 1=VolDown, 2=Mute, 3=PlayPause, 4=Next, 5=Prev, 6=Stop) |
| 5 | Mouse | `action` (0=Click, 1=RightClick, 2=MiddleClick, 3=ScrollUp, 4=ScrollDown), `value` |
| 6 | Layer | Not implemented |
| 7 | Profile | `profileId` |
| 8 | App | Not implemented |
| 9 | URL | Not implemented |

## Macro Step Format

```json
{
  "stepType": 1,      // 1=delay, 2=keyPress, 3=text, 4=media
  "delayMs": 100,     // for delay steps
  "key": 4,           // HID key code for keyPress
  "modifiers": 1,     // modifier bitmask for keyPress
  "text": "hello",    // for text steps (max 31 chars)
  "mediaFunction": 0  // for media steps
}
```

Max 16 steps per macro. Delays capped at 5000ms. Execution stops if HID connection drops mid-macro.
