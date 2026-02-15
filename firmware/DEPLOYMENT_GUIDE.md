# Micropad Firmware - Complete Deployment Guide

## 📋 Table of Contents
1. [Hardware Preparation](#hardware-preparation)
2. [Software Installation](#software-installation)
3. [Firmware Upload](#firmware-upload)
4. [Testing Procedures](#testing-procedures)
5. [Pairing with PC](#pairing-with-pc)
6. [Troubleshooting](#troubleshooting)
7. [Production Deployment](#production-deployment)

---

## 🔧 Hardware Preparation

### What You Need
- ✅ Wemos D1 Mini ESP32
- ✅ 12 mechanical switches (Cherry MX compatible)
- ✅ 2 rotary encoders with push buttons
- ✅ Custom PCB with diodes (COL2K orientation)
- ✅ USB cable (data capable, not charge-only)
- ✅ Computer with Arduino IDE

### Pin Verification Checklist

Before uploading, verify connections:

**Matrix Rows (Output, drive LOW):**
- [ ] Row 0 → GPIO 16
- [ ] Row 1 → GPIO 17
- [ ] Row 2 → GPIO 18

**Matrix Columns (Input with pullup, read LOW when pressed):**
- [ ] Col 0 → GPIO 21
- [ ] Col 1 → GPIO 22
- [ ] Col 2 → GPIO 23
- [ ] Col 3 → GPIO 19

**Encoder 1 (Top-Left):**
- [ ] A → GPIO 32
- [ ] B → GPIO 33
- [ ] SW → GPIO 27

**Encoder 2 (Top-Right):**
- [ ] A → GPIO 25
- [ ] B → GPIO 26
- [ ] SW → GPIO 13

**Diodes:**
- [ ] All 12 keys have diodes
- [ ] Cathode (stripe) points toward key
- [ ] Anode connects to column

### Power Check
1. Connect ESP32 to USB
2. Check for power LED
3. Measure voltage on 3.3V pin (should be ~3.3V)
4. Check for heating (shouldn't be hot)

---

## 💾 Software Installation

### Step 1: Install Arduino IDE

1. Download from: https://www.arduino.cc/en/software
2. Install Arduino IDE 2.x
3. Launch Arduino IDE

### Step 2: Add ESP32 Support

1. **File → Preferences**
2. In "Additional Board Manager URLs":
   ```
   https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json
   ```
3. Click **OK**
4. **Tools → Board → Boards Manager**
5. Search: "esp32"
6. Install: **"ESP32 by Espressif Systems"** v2.0.14+
7. Wait for completion (may take 5-10 minutes)

### Step 3: Install Libraries

**Via Library Manager (Sketch → Include Library → Manage Libraries):**

1. **NimBLE-Arduino** v1.4.1+
   - Search: "NimBLE-Arduino"
   - Author: h2zero
   - Click **Install**

2. **ArduinoJson** v6.21.3+
   - Search: "ArduinoJson"
   - Author: Benoit Blanchon
   - Click **Install**

**Manual Installation (Required):**

3. **ESPAsyncWebServer**
   - Download: https://github.com/me-no-dev/ESPAsyncWebServer/archive/master.zip
   - **Sketch → Include Library → Add .ZIP Library**
   - Select downloaded ZIP

4. **AsyncTCP**
   - Download: https://github.com/me-no-dev/AsyncTCP/archive/master.zip
   - **Sketch → Include Library → Add .ZIP Library**
   - Select downloaded ZIP

### Step 4: Open Project

1. Navigate to: `firmware/Micropad/`
2. Double-click `Micropad.ino`
3. Arduino IDE opens with all project files

---

## ⬆️ Firmware Upload

### Configure Board Settings

**Tools Menu:**
```
Board: "ESP32 Dev Module"
Upload Speed: 921600
CPU Frequency: 240MHz (WiFi/BT)
Flash Frequency: 80MHz
Flash Mode: QIO
Flash Size: 4MB (32Mb)
Partition Scheme: "Huge APP (3MB No OTA/1MB SPIFFS)"
Core Debug Level: "None"
PSRAM: "Disabled"
Port: COM X (select your port)
```

### Upload Process

1. **Connect ESP32** via USB
2. **Select Port** (Tools → Port → COM X)
3. **Verify** (✓ button)
   - Wait for compilation (1-2 minutes first time)
   - Check for "Done compiling" message
4. **Upload** (→ button)
   - Watch progress bar
   - Look for "Hard resetting via RTS pin..."
5. **Success!** Upload complete

### If Upload Fails:

**Method 1: Boot Button**
1. Hold **BOOT** button on ESP32
2. Click **Upload** in Arduino IDE
3. Release **BOOT** when "Connecting..." appears

**Method 2: Manual Reset**
1. Click **Upload**
2. When "Connecting..." appears:
   - Press and hold **BOOT**
   - Press and release **RESET**
   - Release **BOOT**

---

## 🧪 Testing Procedures

### Test 1: Serial Monitor Check

1. **Open Serial Monitor** (Ctrl+Shift+M or 🔍 icon)
2. Set baud rate: **115200**
3. Press **RESET** button on ESP32
4. Verify boot sequence:

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
LittleFS initialized: 1468 KB total, 16 KB used
Profile loaded: General
...
Active Profile: 0 - General
Micropad ready! Waiting for BLE connection...
========================================
```

**✅ PASS if:** All initialization messages appear, no errors
**❌ FAIL if:** Error messages, boot loops, crashes

### Test 2: Matrix Scanning

1. Serial Monitor open at 115200 baud
2. Press each key K1 through K12 in order
3. Watch for messages:

```
Key 0 pressed
Key 0 released
Key 1 pressed
Key 1 released
...
Key 11 pressed
Key 11 released
```

**Test Matrix:**
```
┌────┬────┬────┬────┐
│ K1 │ K2 │ K3 │ K4 │  Should show: Key 0, 1, 2, 3
├────┼────┼────┼────┤
│ K5 │ K6 │ K7 │ K8 │  Should show: Key 4, 5, 6, 7
├────┼────┼────┼────┤
│ K9 │K10 │K11 │K12 │  Should show: Key 8, 9, 10, 11
└────┴────┴────┴────┘
```

**✅ PASS if:** All 12 keys detected
**❌ FAIL if:** Keys missing, wrong order, ghost presses

**Fixes:**
- Check matrix wiring
- Verify diode orientation
- Test continuity with multimeter

### Test 3: Rotary Encoders

**Encoder 1 (Top-Left):**
1. Rotate clockwise slowly
   - Should see: `Encoder 1 turned: 1` (or 2, 4 depending on stepsPerDetent)
2. Rotate counter-clockwise
   - Should see: `Encoder 1 turned: -1` (or -2, -4)
3. Press encoder button
   - Should see: `Encoder 1 pressed`

**Encoder 2 (Top-Right):**
1. Repeat same tests
2. Look for: `Encoder 2 turned: X` and `Encoder 2 pressed`

**✅ PASS if:** Both encoders respond correctly
**❌ FAIL if:** 
- No response → Check wiring
- Wrong direction → Swap A and B pins
- Erratic → Check for bouncing, reduce steps

### Test 4: Profile System

**Test Profile Storage:**
1. Serial Monitor shows profile count: `Created 4 default profiles`
2. Verify active profile: `Active Profile: 0 - General`

**Test Profile Switching:**
1. Hold K1 + K4 for 1 second (don't release)
2. Should see: `Combo triggered: K1 + K4` then `Switched to profile 1`
3. Hold K1 + K12 for 1 second
4. Should see: `Switched to profile 0`

**✅ PASS if:** Profiles switch correctly
**❌ FAIL if:** No switching → Check combo timing, verify combo setup

---

## 📱 Pairing with PC

### Windows 10/11 Pairing

1. **Open Bluetooth Settings**
   - Settings → Bluetooth & devices
   - Or: Win+I → Bluetooth & devices

2. **Add Device**
   - Click "Add device"
   - Select "Bluetooth"

3. **Find Micropad**
   - Wait for "Micropad" to appear in list
   - Click on "Micropad"

4. **Wait for Pairing**
   - Should connect automatically
   - May show "Connected" status

5. **Verify in Serial Monitor**
   ```
   BLE HID client connected
   ```

### Test Basic HID Functions

**Open Notepad:**

1. Type: "Hello World"
2. Select all text (Ctrl+A)
3. Press **K1** on Micropad → Should copy (Ctrl+C)
4. Press **K2** → Should paste (Ctrl+V)
5. Result: "Hello WorldHello World"

**Test Media Keys:**

1. Open YouTube or Spotify
2. Start playing music
3. Press **K10** → Should pause/play
4. Rotate **Encoder 1** clockwise → Volume up
5. Rotate counter-clockwise → Volume down
6. Press **Encoder 1** → Mute

**✅ PASS if:** All keys send correct commands
**❌ FAIL if:** No response → Check BLE connection, try re-pairing

---

## 🔍 Troubleshooting

### Problem: Won't Compile

**Error: "NimBLEDevice.h not found"**
```
Solution: Install NimBLE-Arduino library
1. Sketch → Include Library → Manage Libraries
2. Search "NimBLE-Arduino"
3. Install
```

**Error: "AsyncWebServer.h not found"**
```
Solution: Manually install ESPAsyncWebServer and AsyncTCP
See "Software Installation" section above
```

**Error: "Multiple libraries found for X"**
```
Solution: Remove duplicate libraries
1. Close Arduino IDE
2. Navigate to Documents/Arduino/libraries/
3. Delete duplicate folder
4. Reopen Arduino IDE
```

### Problem: Upload Failed

**Error: "Failed to connect to ESP32"**
```
Method 1: Use BOOT button
- Hold BOOT while clicking Upload
- Release when "Connecting..." appears

Method 2: Check USB cable
- Try different USB cable
- Ensure it's a DATA cable, not charge-only

Method 3: Install drivers
- CP2102: Search "CP2102 driver download"
- CH340: Search "CH340 driver download"
- Install for your OS
```

**Error: "Port not found"**
```
Solution:
1. Check Device Manager (Windows)
2. Look under "Ports (COM & LPT)"
3. Find "USB-SERIAL CH340" or "CP2102"
4. Note the COM port number
5. Select in Arduino IDE: Tools → Port → COM X
```

### Problem: ESP32 Boots Then Crashes

**Brownout Detector**
```
Error in serial: "Brownout detector was triggered"
Solution:
- Use powered USB hub
- Better power supply
- Shorter/better USB cable
```

**Boot Loop**
```
ESP32 keeps restarting
Solution:
- Check pin connections (avoid GPIO 0, 2, 12, 15 at boot)
- Disconnect all peripherals
- Upload with only USB connected
- Reconnect peripherals after boot
```

### Problem: Keys Not Working

**No Key Presses Detected**
```
Checklist:
□ Verify pins: Rows (16,17,18), Cols (21,22,23,19)
□ Check diodes installed
□ Measure continuity with multimeter
□ Check solder joints
```

**Keys Stuck or Bouncing**
```
Solution:
1. Open config.h
2. Find: #define DEBOUNCE_MS 5
3. Change to: #define DEBOUNCE_MS 10
4. Re-upload firmware
```

**Ghost Keys**
```
Pressing one key triggers others
Solution:
□ Verify ALL diodes installed
□ Check diode orientation (cathode to key)
□ Test each diode with multimeter
```

### Problem: BLE Won't Pair

**Device Not Appearing**
```
Solutions:
1. Restart ESP32 (press RESET button)
2. Restart Windows Bluetooth service
3. Check serial: "BLE HID started" message
4. Try from another device (phone/tablet)
```

**Can't Connect After Pairing**
```
Solutions:
1. Windows Settings → Bluetooth
2. Find "Micropad"
3. Click ... → Remove device
4. Press RESET on ESP32
5. Re-pair from scratch
```

**Connected But Keys Don't Work**
```
Solutions:
1. Check serial for "BLE HID client connected"
2. Verify BLE initialized without errors
3. Try typing in Notepad (test)
4. Restart both ESP32 and PC
```

---

## 🚀 Production Deployment

### Pre-Deployment Checklist

**Hardware:**
- [ ] All 12 keys soldered and tested
- [ ] Both encoders working
- [ ] Diodes installed correctly
- [ ] No short circuits
- [ ] Clean solder joints
- [ ] Secure connections

**Firmware:**
- [ ] Latest version uploaded
- [ ] All profiles working
- [ ] BLE pairing successful
- [ ] Keys send correct commands
- [ ] Encoders function properly
- [ ] No serial errors

**Testing:**
- [ ] All Test 1-4 passed
- [ ] 1 hour stress test (continuous use)
- [ ] Multiple profile switches
- [ ] BLE reconnect after sleep
- [ ] Power cycle test

### Multiple Unit Deployment

**For each unit:**

1. **Flash Firmware**
   - Upload latest firmware
   - Verify boot messages

2. **Basic Test**
   - Test all 12 keys
   - Test both encoders
   - Test profile switching

3. **Label Device**
   - Note: Unit number
   - Date: Deployment date
   - MAC: Last 6 digits of device ID

4. **Document**
   ```
   Unit #: ___
   Date: ______
   MAC: ____________
   Tests: PASS / FAIL
   Notes: ___________
   ```

5. **Package**
   - Include quick start guide
   - Include troubleshooting card
   - Include USB cable

### Quick Start Card (Print for Users)

```
┌─────────────────────────────────────┐
│    MICROPAD - QUICK START GUIDE     │
├─────────────────────────────────────┤
│                                     │
│ 1. PAIRING:                         │
│    - Settings → Bluetooth           │
│    - Add Device → Bluetooth         │
│    - Select "Micropad"              │
│                                     │
│ 2. DEFAULT KEYS:                    │
│    K1: Copy    | K7: Screenshot     │
│    K2: Paste   | K8: Explorer       │
│    K3: Undo    | K9: Prev Track     │
│    K4: Redo    | K10: Play/Pause    │
│    K5: Alt+Tab | K11: Next Track    │
│    K6: Desktop | K12: Profile       │
│                                     │
│ 3. ENCODERS:                        │
│    Left: Volume Control             │
│    Right: Scroll Wheel              │
│                                     │
│ 4. PROFILE SWITCHING:               │
│    Hold K1+K4: Media Profile        │
│    Hold K1+K12: General Profile     │
│                                     │
│ 5. SUPPORT:                         │
│    [Your support email/URL]         │
│                                     │
└─────────────────────────────────────┘
```

---

## 📝 Final Notes

### Backup Configuration

Before mass deployment, save:
```
□ firmware/Micropad/ folder (entire sketch)
□ Arduino IDE version used
□ Library versions used
□ Board manager URL
□ Partition scheme used
```

### Version Control

Document each deployment:
```
Version: 1.0.0
Date: YYYY-MM-DD
Units Deployed: X
Changes: Initial release
Known Issues: None
```

### Support Resources

Create:
- [ ] FAQ document
- [ ] Video tutorial
- [ ] Troubleshooting flowchart
- [ ] Firmware update procedure
- [ ] Contact information

---

## ✅ Success Criteria

**Deployment is successful when:**
- ✅ Firmware uploads without errors
- ✅ All serial tests pass
- ✅ BLE pairs first time
- ✅ All 12 keys work correctly
- ✅ Both encoders respond properly
- ✅ Profile switching functions
- ✅ No errors after 1 hour use
- ✅ Reconnects after PC sleep
- ✅ User can operate without instructions

---

## 🎉 Congratulations!

Your Micropad is now fully deployed and ready to use!

For advanced features:
- See PHASE2_COMPLETE.md for profile customization
- See PHASE3_COMPLETE.md for Windows app connection
- See TESTING_GUIDE.md for detailed testing procedures

Enjoy your professional wireless macropad! 🚀
