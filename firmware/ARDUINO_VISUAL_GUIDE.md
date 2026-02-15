# Arduino IDE Visual Upload Guide

This guide shows you exactly what to click in Arduino IDE to upload your firmware.

---

## Step 1: Open Arduino IDE

After installation, you should see:
```
┌─────────────────────────────────────────────────────┐
│ File  Edit  Sketch  Tools  Help             [×][□][-]│
├─────────────────────────────────────────────────────┤
│ ↶ ↷  ✓  →  Serial Monitor 🔍                       │
├─────────────────────────────────────────────────────┤
│                                                     │
│  // Your code will appear here                      │
│                                                     │
│                                                     │
└─────────────────────────────────────────────────────┘
```

**Important Buttons:**
- **✓** = Verify (compile code)
- **→** = Upload to ESP32
- **🔍** = Serial Monitor (view output)

---

## Step 2: Add ESP32 Support

### Open Preferences
```
File → Preferences
┌─────────────────────────────────────────┐
│ Preferences                      [×]    │
├─────────────────────────────────────────┤
│                                         │
│ Additional Board Manager URLs:          │
│ ┌─────────────────────────────────────┐│
│ │https://raw.githubusercontent.com... ││  ← Paste here
│ └─────────────────────────────────────┘│
│                                         │
│                        [Cancel]  [OK]   │
└─────────────────────────────────────────┘
```

**Paste this URL:**
```
https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json
```

### Install ESP32 Boards
```
Tools → Board → Boards Manager
┌─────────────────────────────────────────┐
│ Boards Manager                   [×]    │
├─────────────────────────────────────────┤
│ Search: [esp32____________]      [🔍]   │
├─────────────────────────────────────────┤
│                                         │
│ ○ ESP32 by Espressif Systems            │
│   Arduino core for ESP32                │
│   Version: 2.0.14  [Install] [Select]  │  ← Click Install
│                                         │
└─────────────────────────────────────────┘
```

Wait 5-10 minutes for installation.

---

## Step 3: Install Libraries

### Open Library Manager
```
Sketch → Include Library → Manage Libraries
┌─────────────────────────────────────────┐
│ Library Manager                  [×]    │
├─────────────────────────────────────────┤
│ Search: [NimBLE___________]      [🔍]   │
├─────────────────────────────────────────┤
│                                         │
│ ○ NimBLE-Arduino by h2zero              │
│   Version: 1.4.1         [Install]      │  ← Click
│                                         │
└─────────────────────────────────────────┘
```

**Repeat for:**
1. **NimBLE-Arduino** by h2zero
2. **ArduinoJson** by Benoit Blanchon

### Add ZIP Libraries

Download these files:
- https://github.com/me-no-dev/ESPAsyncWebServer/archive/master.zip
- https://github.com/me-no-dev/AsyncTCP/archive/master.zip

Then:
```
Sketch → Include Library → Add .ZIP Library
┌─────────────────────────────────────────┐
│ Select ZIP File              [×]        │
├─────────────────────────────────────────┤
│ Look in: [Downloads ▼]                  │
│                                         │
│ ○ ESPAsyncWebServer-master.zip          │  ← Select
│ ○ AsyncTCP-master.zip                   │  ← Then this
│                                         │
│              [Cancel]        [Open]     │
└─────────────────────────────────────────┘
```

---

## Step 4: Open Project

```
File → Open
┌─────────────────────────────────────────┐
│ Open                         [×]        │
├─────────────────────────────────────────┤
│ Look in: [code_V1\firmware\Micropad ▼] │
│                                         │
│ 📁 actions/                              │
│ 📁 comms/                                │
│ 📁 input/                                │
│ 📁 profiles/                             │
│ 📄 config.h                              │
│ 📄 Micropad.ino          ← Select this  │
│                                         │
│              [Cancel]        [Open]     │
└─────────────────────────────────────────┘
```

Arduino IDE opens with multiple tabs:
```
┌─────────────────────────────────────────────────────┐
│ Micropad.ino  config.h  matrix.h  encoder.h  ...    │
├─────────────────────────────────────────────────────┤
│ /*                                                  │
│  * Micropad - Professional Wireless Macropad        │
│  * Version: 1.0.0                                   │
│  */                                                 │
│                                                     │
│ #include <Arduino.h>                                │
│ ...                                                 │
└─────────────────────────────────────────────────────┘
```

---

## Step 5: Configure Board

### Connect ESP32
Plug in via USB. Should see COM port appear.

### Select Board
```
Tools → Board → ESP32 Arduino → ESP32 Dev Module

Tools Menu Should Look Like:
┌──────────────────────────────────────┐
│ ✓ Board: "ESP32 Dev Module"         │  ← Select
│   Upload Speed: 921600               │  ← Select
│   CPU Frequency: 240MHz (WiFi/BT)   │  ← Select
│   Flash Frequency: 80MHz             │  ← Select
│   Flash Mode: QIO                    │  ← Select
│   Flash Size: 4MB (32Mb)             │  ← Select
│   Partition Scheme: "Huge APP..."   │  ← IMPORTANT!
│   Core Debug Level: None             │  ← Select
│   PSRAM: Disabled                    │  ← Select
│   Port: COM3 ✓                       │  ← Your COM port
└──────────────────────────────────────┘
```

**Most Important:**
- **Board:** ESP32 Dev Module
- **Partition Scheme:** Huge APP (3MB No OTA/1MB SPIFFS)
- **Port:** Your COM port (e.g., COM3, COM4)

---

## Step 6: Upload Firmware

### Verify First (Optional)
Click **✓** button
```
┌─────────────────────────────────────────┐
│ Output                                  │
├─────────────────────────────────────────┤
│ Compiling sketch...                     │
│ █████████████████████░░░░░░░  75%       │
│                                         │
│ Sketch uses 856432 bytes (65%)          │
│ Global variables use 45632 bytes (13%)  │
│                                         │
│ Done compiling.                         │  ← Success!
└─────────────────────────────────────────┘
```

### Upload
Click **→** button
```
┌─────────────────────────────────────────┐
│ Output                                  │
├─────────────────────────────────────────┤
│ Sketch uses 856432 bytes (65%)          │
│ Connecting.....                         │
│ Chip is ESP32-D0WDQ6 (revision 1)      │
│ Writing at 0x00010000... (10%)          │
│ Writing at 0x00020000... (20%)          │
│ ...                                     │
│ Writing at 0x000e0000... (100%)         │
│ Wrote 856432 bytes in 12.3 seconds     │
│                                         │
│ Hard resetting via RTS pin...           │  ← Success!
└─────────────────────────────────────────┘
```

**If Upload Fails, See "Upload Failed" Section Below**

---

## Step 7: Open Serial Monitor

Click **🔍** (Serial Monitor) button

### Configure Monitor
```
┌─────────────────────────────────────────────────────┐
│ Serial Monitor                                      │
├─────────────────────────────────────────────────────┤
│ Port: COM3         Baud: [115200 ▼]                 │  ← Set to 115200
├─────────────────────────────────────────────────────┤
│                                                     │
│ ========================================             │
│ Micropad Firmware 1.0.0                             │
│ ========================================             │
│ Initializing input hardware...                      │
│ Matrix initialized                                  │
│ Encoder initialized on pins A=32, B=33, SW=27      │
│ Encoder initialized on pins A=25, B=26, SW=13      │
│ ...                                                 │
│ Micropad ready! Waiting for BLE connection...      │
│ ========================================             │
│                                                     │
└─────────────────────────────────────────────────────┘
```

Press **RESET** button on ESP32 to see boot messages.

---

## Common Issues & Solutions

### Issue 1: Upload Failed

```
Output:
A fatal error occurred: Failed to connect to ESP32
```

**Solution 1: Use BOOT Button**
```
1. Hold BOOT button on ESP32
2. Click Upload (→) in Arduino IDE
3. Wait for "Connecting..."
4. Release BOOT button
```

**Solution 2: Check Cable**
- Try different USB cable
- Must be DATA cable, not charge-only

**Solution 3: Check Port**
```
Tools → Port
┌──────────────────────────┐
│ ○ COM1                   │
│ ○ COM3 (USB-SERIAL CH340)│  ← Select this
│ ○ COM4                   │
└──────────────────────────┘
```

### Issue 2: No COM Port Visible

**Install USB Driver:**
- For CH340: Google "CH340 driver download"
- For CP2102: Google "CP2102 driver download"
- Install driver
- Restart Arduino IDE
- Port should appear

**Check Device Manager (Windows):**
```
Device Manager
├─ Ports (COM & LPT)
│  └─ USB-SERIAL CH340 (COM3)  ← Should see this
```

### Issue 3: Compilation Error

```
Output:
NimBLEDevice.h: No such file or directory
```

**Missing Library!**
1. Go to Library Manager
2. Search for the library name
3. Install it
4. Try again

### Issue 4: ESP32 Rebooting Loop

```
Serial Monitor:
Brownout detector was triggered
Guru Meditation Error
...
```

**Power Issue:**
- Use powered USB hub
- Try different USB port
- Use USB 2.0 port (not USB 3.0)

---

## Success Indicators

### ✅ Upload Successful
```
Output:
Hard resetting via RTS pin...
```

### ✅ Serial Monitor Working
```
Serial Monitor:
========================================
Micropad Firmware 1.0.0
========================================
Micropad ready!
```

### ✅ Keys Working
```
Serial Monitor:
Key 0 pressed
Key 0 released
Key 1 pressed
Key 1 released
```

### ✅ BLE Working
```
Serial Monitor:
BLE HID started, waiting for connection...
(After pairing in Windows)
BLE HID client connected
```

---

## Button Cheat Sheet

```
Arduino IDE Top Bar:

↶  = Undo
↷  = Redo
✓  = Verify/Compile (check for errors)
→  = Upload to ESP32
🔍 = Serial Monitor (view debug output)
```

**Keyboard Shortcuts:**
- **Ctrl+R** = Verify
- **Ctrl+U** = Upload
- **Ctrl+Shift+M** = Serial Monitor

---

## Next Steps

After successful upload:

1. ✅ Verify serial output
2. ✅ Test all 12 keys
3. ✅ Test both encoders
4. ✅ Pair via Bluetooth
5. ✅ Test in Notepad
6. 🎉 **Ready to use!**

For detailed testing, see **DEPLOYMENT_GUIDE.md**

---

## Need More Help?

**Documentation:**
- QUICK_START.md - Fast setup
- ARDUINO_IDE_SETUP.md - Detailed setup
- DEPLOYMENT_GUIDE.md - Complete guide
- TESTING_GUIDE.md - All tests

**Can't Find COM Port?**
→ Install USB drivers (CH340 or CP2102)

**Upload Keeps Failing?**
→ Hold BOOT button during upload

**Keys Not Working?**
→ Check serial monitor for debug messages

**BLE Won't Pair?**
→ Press RESET, remove old pairing, try again
