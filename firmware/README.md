# Micropad Firmware

## Quick Start

### 1. Install PlatformIO

If you haven't already:
- Install [VS Code](https://code.visualstudio.com/)
- Install the [PlatformIO extension](https://platformio.org/install/ide?install=vscode)

### 2. Open Project

1. Open VS Code
2. File → Open Folder
3. Select the `firmware` folder

### 3. Flash to ESP32

1. Connect your Wemos D1 Mini ESP32 via USB
2. Click the PlatformIO icon in the sidebar (alien head)
3. Click "Upload" (or press Ctrl+Alt+U)
4. Wait for compilation and upload to complete

### 4. Monitor Serial Output

1. Click "Monitor" in PlatformIO sidebar (or press Ctrl+Alt+S)
2. You should see debug output showing:
   - Initialization messages
   - Key presses
   - Encoder turns
   - BLE connection status

## Hardware Connections

### Key Matrix
- **Rows**: GPIO 16, 17, 18
- **Columns**: GPIO 21, 22, 23, 19

### Encoders
- **Encoder 1** (Top-left): A=32, B=33, SW=27
- **Encoder 2** (Top-right): A=25, B=26, SW=13

## Built-in Profiles

The firmware includes 4 pre-configured profiles stored in LittleFS:

### Profile 0: General (Default)
**Keys:**
- K1: Ctrl+C (Copy)
- K2: Ctrl+V (Paste)
- K3: Ctrl+Z (Undo)
- K4: Ctrl+Y (Redo)
- K5: Alt+Tab (Switch Window)
- K6: Win+D (Show Desktop)
- K7: Win+Shift+S (Screenshot)
- K8: Win+E (Explorer)
- K9: Previous Track
- K10: Play/Pause
- K11: Next Track
- K12: (Switch to Profile 1 via combo)

**Encoder 1:** Volume Up/Down, Press=Mute  
**Encoder 2:** Scroll Up/Down, Press=Play/Pause

### Profile 1: Media
Dedicated media control profile with volume, playback controls, and profile switching back to General.

### Profile 2: VS Code
Optimized for coding with shortcuts like Save, Find, Command Palette, Debug, Terminal, Comment, Format, etc.

### Profile 3: Creative (Photoshop/etc)
Tools for creative apps: Undo/Redo, Brush controls, Layer management, Transform, Selection tools, etc.

## Profile Switching

**Two-Key Combos:**
- Hold K1 + K4 for 800ms → Switch to Profile 1 (Media)
- Hold K1 + K12 for 800ms → Switch to Profile 0 (General)

**In-Profile Switching:**
- Many profiles have K12 mapped to switch back to General profile

**Persistent:**
- Last active profile is saved and restored on reboot

## Testing Steps

### 1. Test Serial Output
```bash
# Open monitor and press keys
pio device monitor -b 115200
```

You should see:
```
========================================
Micropad Firmware 1.0.0
========================================
Initializing input hardware...
Matrix initialized
Encoder initialized on pins A=32, B=33, SW=27
Encoder initialized on pins A=25, B=26, SW=13
Loading default profile...
Profile loaded: General
Starting BLE HID...
BLE HID started, waiting for connection...
========================================
Micropad ready! Waiting for BLE connection...
========================================
```

### 2. Test Matrix Scanning

Press each key and verify you see:
```
Key 0 pressed
Key 0 released
```

### 3. Test Encoders

Rotate encoders and press them:
```
Encoder 1 turned: 1
Encoder 1 pressed
```

### 4. Test BLE Connection

**On Windows:**
1. Go to Settings → Bluetooth & devices
2. Click "Add device"
3. Look for "Micropad"
4. Click to pair

**On Your PC:**
Once connected, test the keys:
- Press K1 (should copy)
- Press K2 (should paste)
- Rotate encoder 1 (should change volume)

## Troubleshooting

### ESP32 Won't Boot
If the ESP32 fails to boot:
- Check that no GPIO 0, 2, 12, or 15 are being used
- Verify all connections are correct
- Try holding BOOT button while powering on

### Keys Not Working
1. Check serial output to verify keys are being detected
2. Verify diode orientation (COL2K - cathode to key)
3. Check for cold solder joints

### Encoders Not Responding
1. Verify pin connections
2. Check that encoders have pullups enabled
3. Try swapping A and B pins if direction is reversed

### BLE Not Connecting
1. Make sure another BLE device isn't already connected
2. Restart the ESP32
3. Clear Bluetooth cache on PC and try pairing again
4. Check that NimBLE library is properly installed

### Compilation Errors

If you get library errors:
```bash
# Clean and rebuild
pio run -t clean
pio run
```

## Advanced Configuration

### Change BLE Name
Edit `src/config.h`:
```cpp
#define BLE_DEVICE_NAME "MyCustomName"
```

### Adjust Debounce Time
Edit `src/config.h`:
```cpp
#define DEBOUNCE_MS 5  // Increase if keys are bouncing
```

### Enable/Disable Debug Output
Edit `src/config.h`:
```cpp
#define DEBUG_ENABLED true  // Set to false to disable
```

## Phase 2 Features (Implemented)

✅ **Profile Storage**: Profiles saved to LittleFS flash storage  
✅ **Profile Manager**: Load, save, switch between 8 profiles  
✅ **JSON Serialization**: Profiles stored as human-readable JSON  
✅ **Persistent Settings**: Last active profile saved via NVS Preferences  
✅ **Key Combos**: Two-key combinations for quick profile switching  
✅ **4 Built-in Templates**: General, Media, VS Code, Creative  
✅ **Factory Reset**: Restore all default profiles

## Storage Information

The firmware uses ESP32's LittleFS filesystem for profile storage:
- **Location**: Flash partition (separate from program)
- **Format**: JSON files in `/profiles/` directory
- **Naming**: `profile_0.json` through `profile_7.json`
- **Atomic Writes**: Safe profile updates (writes to .tmp then renames)
- **Capacity**: Up to 8 profiles (expandable)

## Next Steps

**Phase 2 Complete!** ✅

Now move to:
1. **Phase 3**: BLE GATT config service for app communication
2. **Phase 3**: WebSocket server for WiFi configuration
3. **Phase 4**: Build Windows WPF application
4. **Phase 5**: Profile editor UI and sync

## Support

Check the main project README for:
- Full feature documentation
- Windows app development
- Profile customization
- Advanced features (macros, layers, etc.)
