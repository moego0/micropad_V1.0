# Micropad Firmware

ESP32 firmware for the Micropad wireless macropad. Supports 12 keys, 2 encoders, BLE HID, profiles on LittleFS, and BLE GATT config for the Windows app.

---

## Quick Start (5 minutes)

### 1. Arduino IDE
- Install [Arduino IDE 2.x](https://www.arduino.cc/en/software).
- **File → Preferences** → Additional Board Manager URLs, add:
  ```
  https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json
  ```
- **Tools → Board → Boards Manager** → install **ESP32 by Espressif Systems** (v2.0.14+).

### 2. Libraries
- **Sketch → Include Library → Manage Libraries**: install **NimBLE-Arduino**, **ArduinoJson**.
- **Add .ZIP Library**: add [ESPAsyncWebServer master](https://github.com/me-no-dev/ESPAsyncWebServer/archive/master.zip) and [AsyncTCP master](https://github.com/me-no-dev/AsyncTCP/archive/master.zip).

### 3. Open & upload
- Open `firmware/Micropad/Micropad.ino`.
- **Tools → Board**: ESP32 Dev Module.
- **Tools → Partition Scheme**: Huge APP (3MB No OTA/1MB SPIFFS).
- **Tools → Port**: your COM port.
- Connect ESP32 via USB → click **Upload**.

### 4. Pair (Windows)
- **Settings → Bluetooth & devices → Add device → Bluetooth** → select **Micropad**.

---

## Hardware

### Pinout
- **Matrix rows:** GPIO 16, 17, 18  
- **Matrix cols:** GPIO 21, 22, 23, 19  
- **Encoder 1:** A=32, B=33, SW=27  
- **Encoder 2:** A=25, B=26, SW=13  

Diodes: cathode (stripe) toward key, anode to column.

### PlatformIO (optional)
```bash
cd firmware
pio run -t upload
pio device monitor -b 115200
```

---

## Profiles

- **8 profile slots** on LittleFS; 4 built-in: General, Media, VS Code, Creative.
- **Switch profiles:** Hold K1+K4 (≈800 ms) → Media; Hold K1+K12 (≈800 ms) → General.
- Last active profile is saved and restored on reboot.

---

## BLE connection troubleshooting

- **Connects then disconnects / driver error:** Update Bluetooth driver (Device Manager → Bluetooth → adapter → Update driver). Remove Micropad from Bluetooth & devices, restart PC, power-cycle Micropad, then pair again. Disable power saving for the Bluetooth adapter (Device Manager → adapter → Power Management → uncheck “turn off to save power”).
- **Only one host:** BLE HID allows one host. If another device (e.g. phone) is paired, forget Micropad on that device, power-cycle Micropad, then pair on PC.
- **USB flashing:** Use CH340/CP2102 USB‑serial driver if the board is not detected.

---

## Testing

1. **Serial monitor** (115200): reset ESP32; you should see “Micropad Firmware 1.0.0” and “Micropad ready!”. Press keys → “Key X pressed”.
2. **Encoders:** Rotate and press; check “Encoder N turned/pressed” in serial.
3. **BLE:** Pair on Windows, open Notepad; K1 = Copy, K2 = Paste, Encoder 1 = volume.

---

## Config (optional)

- **config.h:** `BLE_DEVICE_NAME`, `DEBOUNCE_MS`, `DEBUG_ENABLED`.

---

## BLE only (no Classic SPP)

This firmware uses **NimBLE only**. It does **not** use Bluetooth Classic or SPP (Serial Port Profile). That avoids Windows creating "Standard Serial over Bluetooth link (COMx)" ports; the Windows app connects via BLE GATT only.

## GATT (for Windows app)

- Service: `4fafc201-1fb5-459e-8fcc-c5c9c331914b`  
- CMD (write): `...914c`  
- EVT (notify): `...914d`  

Commands: `getDeviceInfo`, `listProfiles`, `getProfile`, `setProfile`, `setActiveProfile`, `getStats`, `factoryReset`, `reboot`.
