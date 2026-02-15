# Micropad - Quick Start Guide

## 📦 What You Need
- Wemos D1 Mini ESP32 with assembled macropad
- USB cable (data capable)
- Computer with Arduino IDE
- Windows PC for Bluetooth pairing

---

## 🚀 5-Minute Setup

### Step 1: Install Arduino IDE (5 min)
1. Download from https://www.arduino.cc/en/software
2. Install and open
3. Go to **File → Preferences**
4. Add to "Additional Board Manager URLs":
   ```
   https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json
   ```
5. **Tools → Board → Boards Manager**
6. Install **"ESP32 by Espressif Systems"** v2.0.14+

### Step 2: Install Libraries (3 min)
**Sketch → Include Library → Manage Libraries**, install:
1. **NimBLE-Arduino** (search, install)
2. **ArduinoJson** (search, install)

**Download and add as ZIP:**
3. https://github.com/me-no-dev/ESPAsyncWebServer/archive/master.zip
4. https://github.com/me-no-dev/AsyncTCP/archive/master.zip
   (**Sketch → Include Library → Add .ZIP Library** for each)

### Step 3: Open & Configure (1 min)
1. Open `firmware/Micropad/Micropad.ino`
2. **Tools → Board**: "ESP32 Dev Module"
3. **Tools → Partition Scheme**: "Huge APP (3MB No OTA/1MB SPIFFS)"
4. **Tools → Port**: Select your COM port

### Step 4: Upload (2 min)
1. Connect ESP32 via USB
2. Click **Upload** (→ button)
3. Wait for "Hard resetting via RTS pin..."
4. **Success!**

### Step 5: Test (2 min)
1. **Tools → Serial Monitor** (set to 115200 baud)
2. Press RESET button on ESP32
3. Should see:
   ```
   Micropad Firmware 1.0.0
   ...
   Micropad ready!
   ```
4. Press keys → Should see "Key X pressed"

### Step 6: Pair (1 min)
1. Windows Settings → Bluetooth & devices
2. Add device → Bluetooth
3. Select "Micropad"
4. Wait for "Connected"

### Step 7: Use! (∞)
Open Notepad and press keys!
- **K1**: Copy (Ctrl+C)
- **K2**: Paste (Ctrl+V)
- **K10**: Play/Pause media
- **Encoder 1**: Volume control

---

## 🎮 Default Key Layout

```
┌─────┬─────┬─────┬─────┐
│  K1 │  K2 │  K3 │  K4 │
│Copy │Paste│Undo │Redo │
├─────┼─────┼─────┼─────┤
│  K5 │  K6 │  K7 │  K8 │
│Tab  │Desk │Shot │Expl │
├─────┼─────┼─────┼─────┤
│  K9 │ K10 │ K11 │ K12 │
│Prev │Play │Next │ --  │
└─────┴─────┴─────┴─────┘

Encoder 1 (Left): Volume ↑↓ / Mute
Encoder 2 (Right): Scroll ↑↓ / Play
```

---

## 🔧 Quick Troubleshooting

**Upload Failed?**
→ Hold BOOT button while clicking Upload

**Keys Not Working?**
→ Check serial monitor for "Key X pressed" messages

**Can't Pair?**
→ Press RESET on ESP32, try again

**Need More Help?**
→ See DEPLOYMENT_GUIDE.md for detailed instructions

---

## 📱 Profile Switching

**Hold K1 + K4 for 1 second** → Switch to Media profile  
**Hold K1 + K12 for 1 second** → Back to General profile

---

## ✅ You're Done!

Your Micropad is ready to use. Enjoy!

For advanced features, see:
- **ARDUINO_IDE_SETUP.md** - Detailed setup
- **DEPLOYMENT_GUIDE.md** - Complete testing
- **TESTING_GUIDE.md** - All test procedures
