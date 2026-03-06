# Micropad BLE Protocol Spec

BLE-only configuration protocol between the **Windows (or macOS) app** and the **Micropad device** (ESP32). All config traffic is JSON over GATT; HID (keyboard/mouse/media) is separate.

---

## GATT Service & Characteristics

| Purpose | UUID | Direction | Notes |
|--------|-----|------------|--------|
| Config Service | `4fafc201-1fb5-459e-8fcc-c5c9c331914b` | — | Custom config |
| CMD (Write) | `4fafc201-1fb5-459e-8fcc-c5c9c331914c` | App → Device | JSON command or chunk |
| EVT (Notify) | `4fafc201-1fb5-459e-8fcc-c5c9c331914d` | Device → App | JSON response or event |

Optional: BULK characteristic (`...914e`) for large writes; current app uses chunked CMD only.

---

## Envelope Format

### Request (App → Device)

Current format (compatible with spec):

```json
{
  "v": 1,
  "type": "request",
  "id": 1,
  "ts": 0,
  "cmd": "getDeviceInfo",
  "payload": {}
}
```

For profile operations, the app may send `profileId` and/or `profile` at top level:

```json
{
  "v": 1,
  "type": "request",
  "id": 2,
  "cmd": "getProfile",
  "profileId": 0
}
```

```json
{
  "v": 1,
  "type": "request",
  "id": 3,
  "cmd": "setProfile",
  "profile": { "id": 0, "name": "...", "version": 1, "keys": [...], "encoders": [...] }
}
```

**Spec-style envelope (target):**

- `type`: `"request"` or command name in unified form (e.g. `PUT_PROFILE`).
- `id`: Correlation ID (number or string); response must echo it.
- `payload`: Optional object for command parameters.

### Response (Device → App)

Current format:

```json
{
  "v": 1,
  "type": "response",
  "id": 1,
  "ts": 12345,
  "payload": { ... }
}
```

**Spec-style response:**

```json
{
  "type": "RESP",
  "id": 1,
  "ok": true,
  "payload": { ... }
}
```

Error:

```json
{
  "type": "RESP",
  "id": 1,
  "ok": false,
  "error": "ERR_PROFILE_FULL",
  "message": "No free profile slot"
}
```

App should support both `type: "response"` and `type: "RESP"`, and check `payload.success` or `ok` for success.

### Event (Device → App)

```json
{
  "v": 1,
  "type": "event",
  "event": "profileChanged",
  "ts": 12345,
  "payload": { "profileId": 0 }
}
```

**Spec-style events:**

- `EVENT_ACTIVE_PROFILE_CHANGED` → `payload: { profileId }`
- `EVENT_LAYER_CHANGED` (optional) → `payload: { layer }`
- `EVENT_STATS` (optional) → stats snapshot

---

## Commands (Minimum Set)

| Command | Request | Response / Notes |
|---------|---------|------------------|
| **GET_CAPS** | `cmd: "getDeviceInfo"` or `"getCaps"` | `maxProfiles`, `freeBytes?`, `supportsLayers`, `supportsMacros`, `supportsEncoders`. Current app uses `getDeviceInfo` with `capabilities` array. |
| **LIST_PROFILES** | `cmd: "listProfiles"` | `profiles: [ { id, name, updatedAt?, sizeBytes? } ]` |
| **GET_PROFILE** | `cmd: "getProfile", profileId` | `payload` = full profile object (id, name, version, keys[], encoders[]) |
| **PUT_PROFILE** | `cmd: "setProfile", profile` | Validate and save atomically; `ok: true` or error. **Firmware: currently "Not implemented yet".** |
| **DELETE_PROFILE** | `cmd: "deleteProfile", profileId` | **To add** in firmware and app. |
| **SET_ACTIVE_PROFILE** | `cmd: "setActiveProfile", profileId` | `ok: true`, then event `profileChanged` |
| **GET_ACTIVE_PROFILE** | `cmd: "getActiveProfile"` | `payload: { profileId }` — **To add** in firmware. |
| **getStats** | `cmd: "getStats"` | `keyPresses[]`, `encoderTurns[]`, `uptime` |
| **factoryReset** | `cmd: "factoryReset"` | Reset profiles to defaults. |
| **reboot** | `cmd: "reboot"` | Device restarts. |

Optional: `LIST_MACROS`, `GET_MACRO`, `PUT_MACRO`, `DELETE_MACRO` if macros are stored on device; otherwise macros live only on PC and are embedded in profile on push.

---

## Profile JSON Schema (Device)

- **id**: number (slot index).
- **name**: string.
- **version**: number (for conflict detection).
- **keys**: array of key configs (index, type, modifiers, key, text, function, action, value, profileId, path, url, macroId, etc. per action type).
- **encoders**: array of encoder configs (index, acceleration, stepsPerDetent; and per action: cw, ccw, press, hold, etc. when extended).

Layers (future): e.g. **layers**: [ { keys[], encoders[] }, ... ] for Layer0, Layer1, Layer2.

---

## Chunking (Large Payloads)

BLE MTU limits single write size (~512 bytes typical). Messages larger than ~512 bytes **UTF-8** must be chunked.

### App → Device (Send)

- Split message by **UTF-8 byte length** (not character count).
- Each chunk is a JSON object:
  - `chunk`: 0-based index
  - `total`: total number of chunks
  - `dataB64`: base64-encoded UTF-8 segment (preferred)
  - Or `data`: escaped string (legacy)
- Chunk payload size ~400 bytes (to stay under MTU with envelope).
- Optional 10 ms delay between chunk writes to avoid overwhelming the device.

### Device → App (Receive)

- If the app receives JSON with `chunk` and `total`, it reassembles:
  - Collect segments in order (from `dataB64` or `data`).
  - Concatenate, decode if base64, then parse the full JSON as one response or event.
- Firmware should send long responses (e.g. full profile) in the same chunk format (e.g. 480-byte chunks with `data` in JSON).

---

## Transport Constraints

- All BLE I/O must be **async**; no blocking in the input path so key/encoder input stays responsive.
- Use **cancellation tokens** for timeouts (e.g. 5 s per request).
- **Checksum:** Optional per-chunk checksum for robustness; not required for initial implementation.
- **Reconnect:** On reconnect, app must re-subscribe to EVT notifications and may re-issue GET_ACTIVE_PROFILE / LIST_PROFILES.

---

## Capabilities Discovery

On connect, the app should call **GET_CAPS** (or getDeviceInfo) and use the result to:

- Show **maxProfiles** / **freeBytes** in the Profiles Manager.
- Hide or disable UI for **layers** if `supportsLayers` is false.
- Hide or disable macro-on-device if `supportsMacros` is false.
- Adjust encoder UI based on `supportsEncoders`.

This allows graceful behavior when connected to older firmware.
