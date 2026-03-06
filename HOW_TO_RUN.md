# How to Run — Micropad

Quick start for **Windows app** and **firmware** (BLE-only; no Wi‑Fi/WebSocket).

---

## 1. Firmware (ESP32)

### Prerequisites

- [Arduino IDE 2.x](https://www.arduino.cc/en/software) or PlatformIO
- **ESP32** board support (Espressif)
- Libraries: **NimBLE-Arduino**, **ArduinoJson**

### Build & Upload

1. Open `firmware/Micropad/Micropad.ino` in Arduino IDE.
2. **Tools → Board:** ESP32 Dev Module  
3. **Tools → Partition Scheme:** Huge APP (3MB No OTA/1MB SPIFFS)  
4. **Tools → Port:** your COM port  
5. Connect ESP32 via USB → **Upload**

### First run

- Serial Monitor (115200): you should see "Micropad Firmware …" and "Micropad ready!"
- Put the device in pairing mode if needed; it advertises with the config service UUID.

**Details:** [firmware/README.md](firmware/README.md)

---

## 2. Windows App (.NET 8 WPF)

### Prerequisites

- Windows 10/11 (Build 19041+)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Bluetooth LE support

### Build & Run

```bash
cd windows-app
dotnet restore
dotnet build
dotnet run --project Micropad.App
```

If you get "file is locked" / "being used by another process", close any running Micropad.App instance, or use:

```powershell
.\run-app.ps1
```

### Publish (single exe)

```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
# Output: Micropad.App\bin\Release\net8.0-windows10.0.22621.0\win-x64\publish\Micropad.App.exe
```

### Usage

1. **Devices** — Scan, select Micropad, Connect.
2. **Profiles** — Load from device, edit keys/encoders, Push to device.
3. **Macros** — Record or edit, assign to keys.
4. **Stats** — View key/encoder counts (if firmware supports).
5. **Settings** — Auto-connect, auto-reconnect, start with Windows, minimize to tray, per-app profile mappings.

**Details:** [windows-app/README.md](windows-app/README.md)

---

## 3. Pairing (Windows)

- **Settings → Bluetooth & devices → Add device → Bluetooth** → select **Micropad**.
- If pairing fails with "Try connecting your device again", see [TROUBLESHOOTING.md](TROUBLESHOOTING.md) and [Windows_Pairing_Fix.md](Windows_Pairing_Fix.md).

---

## 4. Protocol & Docs

- **Protocol spec (commands, events, chunking):** [PROTOCOL_SPEC.md](PROTOCOL_SPEC.md)
- **Requirements vs current implementation:** [REQUIREMENTS_SYNC.md](REQUIREMENTS_SYNC.md)
- **Troubleshooting (BLE, pairing, cache):** [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
