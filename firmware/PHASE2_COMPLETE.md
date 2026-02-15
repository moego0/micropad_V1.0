# Phase 2 Complete: Storage & Profiles ✅

## What Was Implemented

### 1. LittleFS Profile Storage (`profile_storage.h/cpp`)
- **JSON-based storage** on ESP32 flash filesystem
- **Atomic writes** (write to .tmp, then rename)
- **CRUD operations**: Create, Read, Update, Delete profiles
- **Storage info**: Total/used/free space monitoring
- **Format support**: Full filesystem formatting for factory reset

**Key Features:**
```cpp
ProfileStorage storage;
storage.init();
storage.saveProfile(profile);        // Save to flash
storage.loadProfile(id, profile);    // Load from flash
storage.profileExists(id);           // Check if exists
storage.getProfileCount();           // Count stored profiles
storage.format();                    // Factory reset
```

### 2. Profile Manager (`profile_manager.h/cpp`)
- **Centralized profile management**
- **Active profile tracking** with NVS Preferences
- **Automatic profile loading** on boot
- **Profile switching** with validation
- **Factory reset** with default profile recreation
- **4 built-in profile templates**

**Key Features:**
```cpp
ProfileManager manager;
manager.init();                      // Initialize with last active profile
manager.setActiveProfile(id);        // Switch profiles
manager.getCurrentProfile();         // Get active profile pointer
manager.factoryReset();              // Reset all profiles
manager.initializeDefaultProfiles(); // Create default set
```

### 3. Combo Detector (`combo_detector.h/cpp`)
- **Multi-key combination detection**
- **Hold duration threshold** (e.g., hold 800ms)
- **Up to 8 simultaneous combos**
- **Non-blocking detection**

**Key Features:**
```cpp
ComboDetector combo;
combo.addCombo(0, 3, 800);          // K1 + K4, hold 800ms
combo.update(keyStates);             // Check in main loop
if (combo.getTriggeredComboIndex() >= 0) {
    // Combo triggered!
}
```

### 4. Profile Templates (`profile_templates.h`)
Pre-configured professional profiles:
- **VS Code**: Coding shortcuts, debugging, terminal, formatting
- **Creative**: Photoshop/design tools, layers, transforms
- **Media**: (in profile_manager.cpp) Media playback and volume
- **General**: (default_profile.h) General productivity

### 5. NVS Preferences Integration
- **Persistent storage** of last active profile
- **Survives reboots** and power cycles
- **Fast access** (no file I/O for settings)

## File Structure

```
firmware/src/
├── profiles/
│   ├── profile.h                    # Core data structures
│   ├── profile_storage.h/.cpp       # LittleFS I/O
│   ├── profile_manager.h/.cpp       # Profile lifecycle management
│   ├── default_profile.h            # Profile 0: General
│   └── profile_templates.h          # Profiles 2-3: VS Code, Creative
│
├── input/
│   └── combo_detector.h/.cpp        # Two-key combo detection
│
└── main.cpp                         # Updated with profile integration
```

## Built-in Profiles

### Profile 0: General (Default)
- Copy/Paste/Undo/Redo
- Window management (Alt+Tab, Show Desktop)
- Screenshot, Explorer
- Media controls (Prev/Play/Next)
- Volume and scroll encoders

### Profile 1: Media
- Dedicated media playback keys
- Volume controls on keys
- K12 switches back to General
- Same encoder setup

### Profile 2: VS Code
- Save, Find, Quick Open, Command Palette
- Debug (F5), Terminal, Comment
- Format code, console.log()
- Zoom and navigation on encoders
- K12 switches back to General

### Profile 3: Creative (Photoshop/Design)
- Undo/Redo, Save/Save As
- Tool selection (Brush, Eraser)
- Layer management
- Transform, Selection tools
- Brush size and zoom on encoders
- K12 switches back to General

## Profile Switching Methods

### Method 1: Key Combos (Always Available)
Hold two keys together for 800ms:
- **K1 + K4**: Switch to Profile 1 (Media)
- **K1 + K12**: Switch to Profile 0 (General)

### Method 2: Profile Switch Action
Any key can be mapped to switch profiles:
```cpp
action.type = ACTION_PROFILE;
action.config.profile.profileId = 2;  // Switch to VS Code
```

### Method 3: Programmatic (via app later)
```cpp
profileManager.setActiveProfile(3);  // Switch to Creative
```

## Storage Format

Profiles are stored as JSON in LittleFS:

**Path**: `/profiles/profile_X.json` (X = 0-7)

**Example** (`profile_0.json`):
```json
{
  "id": 0,
  "name": "General",
  "version": 1,
  "keys": [
    {
      "index": 0,
      "type": 1,
      "modifiers": 1,
      "key": 6
    },
    ...
  ],
  "encoders": [
    {
      "index": 0,
      "cwAction": {...},
      "ccwAction": {...},
      "pressAction": {...},
      "acceleration": true,
      "stepsPerDetent": 4
    },
    ...
  ]
}
```

## Testing Phase 2

### 1. First Boot Test
```
1. Flash firmware
2. Watch serial output for:
   ✓ "Initializing LittleFS..."
   ✓ "No profiles found, creating defaults..."
   ✓ "Created 4 default profiles"
   ✓ "Active Profile: 0 - General"
```

### 2. Profile Switching Test
```
1. Hold K1 + K4 for 1 second
   → Serial: "Switched to Profile 1"
   → K1-K7 should now be media controls

2. Hold K1 + K12 for 1 second
   → Serial: "Switched to Profile 0"
   → Back to general shortcuts
```

### 3. Persistence Test
```
1. Switch to Profile 1 (K1 + K4)
2. Reset ESP32 (press button)
3. Check serial output
   → Should say "Active Profile: 1 - Media"
   → Profile 1 restored after reboot!
```

### 4. In-Profile Switching Test
```
1. Switch to Profile 1 (Media)
2. Press K12
   → Should switch back to Profile 0 (General)
```

### 5. Storage Info Test
Add this to setup() to see storage stats:
```cpp
DEBUG_PRINTF("Total: %d KB, Used: %d KB, Free: %d KB\n",
    profileManager._storage.getTotalSpace() / 1024,
    profileManager._storage.getUsedSpace() / 1024,
    profileManager._storage.getFreeSpace() / 1024);
```

## Serial Commands (Optional Enhancement)

You can add serial commands for testing:

```cpp
// In main loop:
if (Serial.available()) {
    char cmd = Serial.read();
    if (cmd >= '0' && cmd <= '7') {
        uint8_t id = cmd - '0';
        profileManager.setActiveProfile(id);
        DEBUG_PRINTF("Switched to profile %d\n", id);
    }
}
```

Then type `0-7` in serial monitor to switch profiles!

## Known Limitations

1. **Max 8 profiles**: Can be increased by changing `MAX_PROFILES` in config.h
2. **No macro support yet**: Phase 6 feature
3. **No layer support yet**: Phase 5 feature (V1.1)
4. **Fixed combo mapping**: Combos are hardcoded in main.cpp (will be configurable later)

## Troubleshooting

### "Failed to initialize storage"
- LittleFS mount failed
- Try: `profileManager.factoryReset()` in setup
- Or: Erase flash and re-upload

### Profile not switching
- Check serial output for "Switched to profile X"
- Verify combo hold time (800ms is quite long)
- Try in-profile switching via K12 first

### Profiles not persisting
- NVS partition may be corrupted
- Erase flash completely and re-upload
- Check for `preferences.begin()` errors in serial

## Next Steps → Phase 3

With profiles working, we can now:
1. **Add BLE GATT config service** for Windows app communication
2. **Implement profile upload/download** via BLE
3. **Add WiFi WebSocket server** for faster transfers
4. **Build protocol handler** for command processing

Ready to start Phase 3? 🚀
