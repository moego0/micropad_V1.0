# Micropad Firmware - Arduino IDE Setup Guide

## Prerequisites

### 1. Install Arduino IDE
- Download from: https://www.arduino.cc/en/software
- Version: 2.0 or later recommended
- Install and launch

### 2. Install ESP32 Board Support

1. Open Arduino IDE
2. Go to **File → Preferences**
3. In "Additional Board Manager URLs", add:
   ```
   https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json
   ```
4. Click **OK**
5. Go to **Tools → Board → Boards Manager**
6. Search for "esp32"
7. Install **"ESP32 by Espressif Systems"** (version 2.0.14 or later)
8. Wait for installation to complete

### 3. Install Required Libraries

Go to **Sketch → Include Library → Manage Libraries**, search and install:

#### Required Libraries:
1. **NimBLE-Arduino** by h2zero (v1.4.1 or later)
   - Search: "NimBLE-Arduino"
   - Click Install

2. **ArduinoJson** by Benoit Blanchon (v6.21.3 or later)
   - Search: "ArduinoJson"
   - Click Install

3. **ESP32 LittleFS** (Built-in with ESP32 core)
   - No separate installation needed

4. **ESPAsyncWebServer** (Manual installation required)
   - Download from: https://github.com/me-no-dev/ESPAsyncWebServer/archive/master.zip
   - In Arduino IDE: **Sketch → Include Library → Add .ZIP Library**
   - Select the downloaded ZIP file

5. **AsyncTCP** (Manual installation required)
   - Download from: https://github.com/me-no-dev/AsyncTCP/archive/master.zip
   - In Arduino IDE: **Sketch → Include Library → Add .ZIP Library**
   - Select the downloaded ZIP file

### 4. Configure Board Settings

1. Connect your Wemos D1 Mini ESP32 via USB
2. In Arduino IDE, go to **Tools** and configure:
   - **Board**: "ESP32 Dev Module"
   - **Upload Speed**: 921600
   - **CPU Frequency**: 240MHz (WiFi/BT)
   - **Flash Frequency**: 80MHz
   - **Flash Mode**: QIO
   - **Flash Size**: 4MB (32Mb)
   - **Partition Scheme**: "Huge APP (3MB No OTA/1MB SPIFFS)"
   - **Core Debug Level**: "None" (or "Info" for debugging)
   - **PSRAM**: "Disabled"
   - **Port**: Select your COM port (e.g., COM3, COM4)

## Project Setup

### Option 1: Single File Sketch (Easier)

I'll create a combined single-file version that's easier to use with Arduino IDE.

### Option 2: Multi-File Sketch (Organized)

Arduino IDE supports multi-file sketches with tabs. The current structure will work.

## Folder Structure for Arduino IDE

```
Micropad/
├── Micropad.ino              # Main sketch (renamed from main.cpp)
├── config.h                  # Configuration
├── matrix.h / matrix.cpp     # Key matrix
├── encoder.h / encoder.cpp   # Rotary encoders
├── ble_hid.h / ble_hid.cpp  # BLE HID
└── ... (other files)
```

## Upload Process

1. **Open Sketch**
   - Open `Micropad.ino` in Arduino IDE

2. **Verify Code**
   - Click the ✓ (Verify) button
   - Wait for compilation to complete
   - Check for errors in the output window

3. **Upload Firmware**
   - Click the → (Upload) button
   - Wait for upload to complete
   - Look for "Hard resetting via RTS pin..."

4. **Open Serial Monitor**
   - Click the 🔍 (Serial Monitor) icon
   - Set baud rate to **115200**
   - Press the RESET button on ESP32
   - Watch for boot messages

## Expected Serial Output

```
========================================
Micropad Firmware 1.0.0
========================================
Initializing input hardware...
Matrix initialized
Encoder initialized on pins A=32, B=33, SW=27
Encoder initialized on pins A=25, B=26, SW=13
Initializing profile manager...
Initializing LittleFS...
LittleFS initialized: 1468 KB total, 0 KB used
Created profiles directory
No profiles found, creating defaults...
  - Profile 0: General
  - Profile 1: Media
  - Profile 2: VS Code
  - Profile 3: Creative
Created 4 default profiles
Profile loaded: General
Initializing protocol handler...
Protocol Handler initialized
Starting BLE Config Service...
BLE Config Service started
WiFi disabled (enable via preferences)
Starting BLE HID...
BLE HID started, waiting for connection...
========================================
Active Profile: 0 - General
Micropad ready! Waiting for BLE connection...
========================================
```

## Troubleshooting

### Problem: Compilation Errors

**"NimBLEDevice.h: No such file or directory"**
- Solution: Install NimBLE-Arduino library (see step 3 above)

**"AsyncWebServer.h: No such file or directory"**
- Solution: Install ESPAsyncWebServer and AsyncTCP (see step 3 above)

**"LittleFS.h: No such file or directory"**
- Solution: Update ESP32 core to version 2.0.0 or later

### Problem: Upload Failed

**"A fatal error occurred: Failed to connect to ESP32"**
- Solution 1: Press and hold BOOT button while clicking Upload
- Solution 2: Try a different USB cable (data cable, not charge-only)
- Solution 3: Install CP2102 or CH340 USB drivers

**"Port not found"**
- Solution: Check Device Manager (Windows) for COM port
- Install drivers if needed

### Problem: ESP32 Not Booting

**Brown-out detector triggered**
- Solution: Use a powered USB hub or better power supply

**Boot loop / constant restarting**
- Solution: Check GPIO pin connections (avoid GPIO 0, 2, 12, 15 during boot)

### Problem: Keys Not Working

**No key presses detected**
- Check serial monitor for "Key X pressed" messages
- Verify matrix wiring (rows: 16, 17, 18 / cols: 21, 22, 23, 19)
- Check diode orientation (cathode to key)

**Keys stuck or ghosting**
- Verify diodes are installed (COL2K orientation)
- Increase debounce time in config.h

### Problem: BLE Not Working

**"BLE HID started" but can't pair**
- Go to Windows Settings → Bluetooth
- Remove any old "Micropad" devices
- Click "Add device" → Bluetooth
- Should see "Micropad" appear

**Connected but keys don't work**
- Check that BLE HID initialized successfully in serial
- Try disconnecting and reconnecting
- Restart both ESP32 and PC

## Next Steps

After successful upload:
1. Test basic functionality (see TESTING_GUIDE.md)
2. Pair via Bluetooth
3. Test key presses
4. Test profile switching
5. Connect Windows app (if using)

## Support

If you encounter issues:
1. Check serial monitor output
2. Verify all pin connections
3. Ensure correct board settings
4. Try the examples in this guide
5. Check the TESTING_GUIDE.md for detailed tests
