# Requirements Sync: Professional Micropad / Stream Deck Pro

This document maps the **product requirements** (Stream Deck / macro pad pro experience) to the **current codebase** and lists **gaps** to implement. Work in-place; extend existing MVVM/services and firmware.

---

## A) Windows App — Features

### A1) Macro Library (Reusable)

| Requirement | Current State | Gap / Action |
|-------------|---------------|--------------|
| **Macros as saved assets** | `LocalMacroStorage` saves by **name** only; no GUID, tags, CreatedAt, UpdatedAt, Version | **Core:** Add `MacroAsset` model: `MacroId` (GUID), `Name`, `Tags[]`, `Steps[]`, `CreatedAt`, `UpdatedAt`, `Version`. Migrate `LocalMacroStorage` to store by ID; keep name-based lookup for compatibility. |
| **Create macro manually** | `MacrosView` + `MacrosViewModel` + macro editor (steps list) exist | Extend editor for full step types (see below). |
| **Record macro** | `MacroRecorder` exists (key down/up + delays) | Improve: ensure delays, optional text/media/mouse if feasible. |
| **Save macro to library** | Save by name to `%LocalAppData%\Micropad\Macros\*.json` | Save as `MacroAsset` with GUID; add Tags, timestamps. |
| **Search/filter by name/tags** | Macros listed by name only | Add search box and tag filter in `MacrosView` / `MacrosViewModel`. |
| **Drag & drop macro onto key** | Key assignment supports `macroId` in `KeyConfig` | Ensure UI: macro library panel → drag onto key in profile grid; store `MacroId`. |
| **Export/import macros** | No export/import for macros | Add: Export single macro or "pack" (JSON); Import from file. |
| **Reference vs Embed** | Profile stores `macroId` (string) only | **Reference:** keep `macroId`; editing macro updates all profiles. **Embed:** on assign, option "Embed copy"; store snapshot of steps in profile (new field e.g. `macroSnapshot` or extended key payload). |
| **Macro steps: KeyDown/KeyUp, TapKey, TextType, DelayMs** | `MacroStep`: `action` (keyDown, keyUp, keyPress, delay), `key`, `ms`, `vkCode` | Add: `TapKey` (single tap), `TextType` with Unicode-safe string; ensure DelayMs is first-class. |
| **Mouse click (L/R/M), wheel vertical/horizontal** | `MouseAction` in profile has Click, RightClick, MiddleClick, ScrollUp/Down | Add macro step types: `mouseClick` (L/R/M), `mouseWheelV`, `mouseWheelH` with value. |
| **Media keys** | Profile has Media keys | Add macro step types: volume up/down, mute, play, etc. |
| **Variables: {clipboard}, {date}, {time}** | Not present | Add optional variable expansion in TextType (and possibly in key action) at playback time. |

**Files to extend:**  
`Micropad.Core/Models/MacroStep.cs`, new `MacroAsset.cs` (or extend existing); `Micropad.Services/Storage/LocalMacroStorage.cs`; `MacrosViewModel.cs`, `MacrosView.xaml`; macro editor dialog/view.

---

### A2) Profiles Library + Device Storage + Sync

| Requirement | Current State | Gap / Action |
|-------------|---------------|--------------|
| **Profiles Manager view: Local vs Device** | `ProfilesView` + `ProfilesViewModel`; `ProfileSyncService` has Push/Pull; `LocalProfileStorage` for PC; device list via `listProfiles` | Add clear **Profiles Manager** (or refactor Profiles tab): two lists — **Local profiles** (PC library) and **Device profiles** (from device). |
| **Create / duplicate / rename / delete local** | Local save/load exists; no explicit create/duplicate/rename/delete UI | Add: New profile, Duplicate, Rename, Delete for local library. |
| **Push to device (slot or auto)** | `PushProfileToDeviceAsync(profile)`; device uses slot = profile Id | Support "choose slot" or "auto slot"; show device slots. |
| **Pull from device** | `PullProfileFromDeviceAsync(profileId)` exists | Expose in UI: "Pull" per device profile. |
| **Delete profile from device** | Not implemented | Add protocol `DELETE_PROFILE(profileId)`; firmware + app. |
| **Reorder device profiles** | Device has fixed slots; no reorder in app | Add reorder (e.g. swap slots) and/or "Reorder" UI; firmware may need slot swap or rename. |
| **Device capacity: usedSlots / maxSlots, free bytes** | `listProfiles` returns id, name, size | Add **GET_CAPS** (or extend `getDeviceInfo`) → `maxProfiles`, `freeBytes`; show in UI. |
| **Current active profile on device** | `setActiveProfile` + event `profileChanged` | On connect, call **GET_ACTIVE_PROFILE**; show in UI; subscribe to event. |
| **Conflict: same ProfileId, different versions** | No conflict handling | On push/pull: if local and device both have same Id but different Version → prompt: **Pull** (device wins), **Push** (PC wins), **Cancel**. |

**Files:**  
`ProfilesView.xaml`, `ProfilesViewModel.cs`; `ProfileSyncService.cs`; `ProtocolHandler.cs`; `LocalProfileStorage.cs`; firmware `protocol_handler.cpp`, `profile_manager` / storage.

---

### A3) App-based Auto Profile Switching

| Requirement | Current State | Gap / Action |
|-------------|---------------|--------------|
| **Rules: EXE name, optional window title, priority, default** | `ForegroundMonitor` + mappings: process name → profile ID (stored in `AppSettings.ForegroundMonitorMappings`) | Extend to: **EXE name** (primary), optional **window title contains**; **priority order**; **default profile** when no rule matches. |
| **Auto Mode ON** | Per-app mapping applied when foreground changes | Explicit "Auto Mode" toggle; when ON, apply rules. |
| **Manual Lock** | Not present | "Manual Lock": user picks a profile and auto switching pauses until "Return to Auto". |
| **Stable, no rapid toggling** | No debounce | Add debounce (e.g. 500–1500 ms) before switching profile. |

**Files:**  
`ForegroundMonitor.cs`; `SettingsViewModel.cs` / Settings view; `AppSettings.cs`; optionally a small rules editor (EXE, title, priority, default).

---

### A4) Layers (3) + Combos + Tap/Hold/Double

| Requirement | Current State | Gap / Action |
|-------------|---------------|--------------|
| **3 layers: Layer0 base, Layer1 Fn1, Layer2 Fn2** | `ActionType.Layer = 6` in Core; firmware `ACTION_LAYER` in `profile.h`; no runtime layer stack | **Firmware:** Implement layer stack (currentLayer, Fn1/Fn2 keys). **App:** Profile model: keys/encoders **per layer** (e.g. `keys[layer][index]` or `layers[3]` with keys array). |
| **Fn: Momentary / Toggle / One-shot** | Not implemented | Define Fn key behavior per layer; firmware: momentary (hold), toggle (tap), one-shot (next key only). |
| **Combo: chord (two keys together)** | `combo_detector.cpp/h` exists in firmware | Wire combo detection to "special action"; app: **Combo Editor** — assign action to key pair. |
| **Leader key + sequence** | Not in codebase | Optional: Leader key then sequence → action; firmware + app. |
| **Per-key: Tap / Hold / Double-tap** | Single action per key | Extend key config: `tapAction`, `holdAction`, `doubleTapAction` (each optional). Firmware: timing for hold/double-tap. |
| **UI: Layer tabs in key editor** | Single key grid | Key editor: tabs **Layer0 / Layer1 / Layer2**; edit assignments per layer. |
| **Combo Editor UI** | None | New UI: list combos (key A + key B) and assign action. |

**Files:**  
`Micropad.Core/Models/Profile.cs` (layers, per-key tap/hold/double); `KeyConfig`; profile editor view (layer tabs); new Combo Editor; firmware `profile.h`, `action_executor`, `matrix`, `combo_detector`.

---

### A5) Encoders — Pro Grade

| Requirement | Current State | Gap / Action |
|-------------|---------------|--------------|
| **Map: Rotate CW / CCW, Press, Hold, Press+Rotate, Hold+Rotate** | Firmware `EncoderConfig`: `cwAction`, `ccwAction`, `pressAction`; Windows `EncoderConfig`: acceleration, stepsPerDetent (no CW/CCW/press in Core model) | **Core:** Extend `EncoderConfig` with CW/CCW/Press/Hold and shifted (Press+Rotate, Hold+Rotate). **Firmware:** Already has cw/ccw/press; add hold and shifted modes. |
| **Acceleration: step size + curve + smoothing** | `acceleration` bool, `stepsPerDetent` | Add: step size, acceleration curve (e.g. linear/exp), smoothing; settings per encoder. |
| **Common actions: Volume, Scroll, Timeline jog, Zoom, Tab switch** | Encoder actions are generic (Action type) | Provide presets in UI: Volume, Scroll V/H, Timeline jog, Zoom, Tab switch. |
| **Encoder press cycles Mode A/B/C (HUD)** | Not present | Encoder "mode" (A/B/C) cycled by press; show current mode in HUD. |

**Files:**  
`Micropad.Core/Models/Profile.cs` (`EncoderConfig`); profile editor (encoder section); firmware `encoder.cpp`, `profile.h`; HUD (see A7).

---

### A6) Preset Profile Gallery

| Requirement | Current State | Gap / Action |
|-------------|---------------|--------------|
| **Templates / Presets screen** | None | New view or tab: **Templates / Presets**. |
| **VS Code, GitHub, Fusion 360, Figma, Adobe Premiere presets** | Firmware has `profile_templates.h` / `default_profile.h` (e.g. General, Media, VS Code, Creative) | Define one preset profile per group: VS Code, GitHub (optional `gh`), Fusion 360, Figma, Adobe Premiere — sensible keys + encoders + layers. |
| **Editable after import** | N/A | Import creates a normal profile; user can edit. |
| **"Requires app" badge** | None | Detect if app (e.g. Code, Figma, Premiere) is installed; show badge if not. |

**Files:**  
New `PresetsView` / `PresetsViewModel`; preset definitions (JSON or C#); optional app detection (registry/path or process list).

---

### A7) HUD Overlay + Tray Companion

| Requirement | Current State | Gap / Action |
|-------------|---------------|--------------|
| **Tray mode** | Settings: "Minimize to Tray"; `MinimizeToTray` in `AppSettings` | Ensure main window close → minimize to tray (NotifyIcon); implement if only setting exists. |
| **Tray: connection state + active profile** | Not implemented | Tray icon/tooltip: show Connected/Disconnected and active profile name. |
| **Quick profile switch menu** | None | Tray context menu: list profiles; click to set active. |
| **HUD overlay** | None | When profile/layer/encoder mode changes: small overlay (top-right, 1–2 s): "Profile name • Layer • Encoder mode". Topmost, click-through, no focus steal. |

**Files:**  
`App.xaml.cs` or main window (tray, minimize behavior); new `TrayService` or extend main VM; new `HudOverlay` window (WPF transparent topmost). Firmware/Protocol: events for layer and encoder mode (see B).

---

### A8) Reliability + Diagnostics

| Requirement | Current State | Gap / Action |
|-------------|---------------|--------------|
| **Connection state machine** | `BleConnection`: IsConnected, Connected/Disconnected events; no explicit states | Introduce **ConnectionState**: Idle → Scanning → Pairing → Connecting → Ready → Reconnecting → Error. Expose in VM; UI can show state. |
| **Auto reconnect with exponential backoff** | `TryAutoReconnectAsync`: 1s, 2s, … up to 30s | Already present; ensure re-subscribe to EVT on reconnect. |
| **Re-subscribe to notifications on reconnect** | On connect, app subscribes to EVT | Verify subscription is re-done after reconnect (currently re-connect path may need explicit subscribe again). |
| **"Fix Connection" button** | None | Add: Disconnect cleanly, clear watchers, rescan, reconnect. |
| **Export Diagnostics** | None | Zip: app logs, device info, firmware version, last BLE errors; save as diagnostics zip. |

**Files:**  
`BleConnection.cs` (state enum, re-subscribe); `DevicesViewModel` (Fix Connection, state binding); logging (Serilog); new `DiagnosticsService` or static export method.

---

## B) Firmware — BLE Protocol Support

| Requirement | Current State | Gap / Action |
|-------------|---------------|--------------|
| **Single JSON envelope: type, id, payload** | App sends `v`, `type` (request), `id`, `cmd`, `payload`/`profileId`/`profile`; firmware expects `request`, `id`, `cmd` | Align with spec: **PUT_PROFILE** etc. (see below). Response: `type: "RESP"`, `id`, `ok`, `payload` or `error`/`message`. |
| **GET_CAPS** | `getDeviceInfo` returns deviceId, firmwareVersion, capabilities array | Add **GET_CAPS** (or extend getDeviceInfo): `maxProfiles`, `freeBytes`, `supportsLayers`, `supportsMacros`, `supportsEncoders`. |
| **LIST_PROFILES** | `listProfiles` → profiles[] (id, name, size) | Add `updatedAt` if needed; keep compatible. |
| **GET_PROFILE(profileId)** | Implemented | Keep. |
| **PUT_PROFILE(profileObject)** | **handleSetProfile** returns "Not implemented yet" | **Implement:** validate profile JSON, save atomically to LittleFS (e.g. `/profiles/<id>`). |
| **DELETE_PROFILE(profileId)** | Not implemented | Add command; free slot / delete file. |
| **SET_ACTIVE_PROFILE(profileId)** | `setActiveProfile` implemented | Keep. |
| **GET_ACTIVE_PROFILE** | Not implemented | Add; return current profile id. |
| **LIST_MACROS / GET/PUT/DELETE_MACRO** | No macro storage on device | Optional: implement for device-stored macros; or "macros only on PC, embed on push". |
| **Events: EVENT_ACTIVE_PROFILE_CHANGED, EVENT_LAYER_CHANGED, EVENT_STATS** | `profileChanged` event sent | Rename/align to **EVENT_ACTIVE_PROFILE_CHANGED**; add **EVENT_LAYER_CHANGED**, **EVENT_STATS** (optional). |
| **Chunking for > 2–5 KB** | Firmware receives chunked (chunk/total/dataB64); sends chunked for long payloads | Ensure chunk size and reassembly handle large profiles (e.g. 4–8 KB); avoid blocking in input path. |

**Firmware files:**  
`protocol_handler.cpp/h`, `ble_config.cpp` (send chunked); `profile_storage.cpp` (atomic write); `profile_manager.cpp`.

---

## Implementation Order (Suggested)

1. **Firmware:** Implement `PUT_PROFILE` (validate + save), `GET_ACTIVE_PROFILE`, `DELETE_PROFILE`, `GET_CAPS` (or extend getDeviceInfo).
2. **App — Profiles Manager:** Local vs Device split, push/pull/delete, conflict handling, capacity/active profile.
3. **App — Macro Library:** MacroAsset model, GUID/tags, export/import, reference vs embed, extended step types.
4. **App — Connection & diagnostics:** State machine, Fix Connection, Export Diagnostics.
5. **App — Tray + HUD:** Tray behavior, quick profile menu, HUD overlay.
6. **App — Auto profile:** Debounce, Manual Lock, rules (EXE + optional title, priority, default).http://localhost:5173/
7. **Firmware + App — Layers:** Layer stack, Fn behaviors, per-layer keys in profile; layer tabs in UI.
8. **Firmware + App — Encoders:** Pro mappings (hold, press+rotate, etc.), acceleration settings, mode cycle + HUD.
9. **Combos + Tap/Hold/Double-tap:** Combo editor, per-key tap/hold/double in model and firmware.
10. **Preset gallery:** Presets screen, preset definitions, "Requires app" badge.

---

## Document References

- **How to run:** See [HOW_TO_RUN.md](HOW_TO_RUN.md) (root) and [windows-app/README.md](windows-app/README.md), [firmware/README.md](firmware/README.md).
- **Protocol:** See [PROTOCOL_SPEC.md](PROTOCOL_SPEC.md).
- **Troubleshooting:** See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) and [Windows_Pairing_Fix.md](Windows_Pairing_Fix.md), [BLE_Not_Appearing_Fix.md](BLE_Not_Appearing_Fix.md).
