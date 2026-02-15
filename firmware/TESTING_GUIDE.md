# Micropad Testing Guide - Phase 1 & 2

## Quick Start Testing

### 1. Upload Firmware
```bash
# In VS Code with PlatformIO
1. Open firmware folder
2. Press Ctrl+Alt+U (Upload)
3. Wait for compilation (~2 min first time)
4. Watch for "SUCCESS"
```

### 2. Open Serial Monitor
```bash
# Press Ctrl+Alt+S in VS Code
# Or use: pio device monitor -b 115200
```

Expected output:
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
Starting BLE HID...
BLE HID started, waiting for connection...
========================================
Active Profile: 0 - General
Micropad ready! Waiting for BLE connection...
========================================
```

## Phase 1 Tests: Core Input

### Test 1: Matrix Scanning ✓
**Goal**: Verify all 12 keys work

1. Press each key K1-K12 in order
2. Watch serial output

Expected:
```
Key 0 pressed
Key 0 released
Key 1 pressed
Key 1 released
...
Key 11 pressed
Key 11 released
```

**✅ PASS**: All 12 keys detected  
**❌ FAIL**: Missing keys → check wiring/solder

---

### Test 2: Encoder Rotation ✓
**Goal**: Verify encoder direction and steps

1. Rotate Encoder 1 clockwise (volume up)
2. Rotate Encoder 1 counter-clockwise (volume down)
3. Repeat for Encoder 2

Expected:
```
Encoder 1 turned: 1
Encoder 1 turned: 1
Encoder 1 turned: -1
Encoder 1 turned: -1
```

**✅ PASS**: Both encoders respond correctly  
**❌ FAIL**: Wrong direction → swap A/B pins

---

### Test 3: Encoder Buttons ✓
**Goal**: Verify encoder switch detection

1. Press Encoder 1 button
2. Press Encoder 2 button

Expected:
```
Encoder 1 pressed
Encoder 2 pressed
```

**✅ PASS**: Both buttons detected  
**❌ FAIL**: Not detected → check SW pin wiring

---

### Test 4: BLE Connection ✓
**Goal**: Pair with Windows PC

1. Open Windows Settings → Bluetooth & devices
2. Click "Add device" → Bluetooth
3. Look for "Micropad"
4. Click to pair

Expected (serial):
```
(Connection status will update automatically)
```

Expected (Windows):
- Device shows as "Micropad"
- Pairs successfully
- Shows "Connected"

**✅ PASS**: Device connects  
**❌ FAIL**: Not visible → check BLE initialization

---

### Test 5: Key Actions via BLE ✓
**Goal**: Verify HID keyboard works

**Setup**: Open Notepad on Windows

1. Press K1 → Should do nothing (Copy needs something selected)
2. Type "Hello World" manually
3. Select all text (Ctrl+A)
4. Press K1 → Should copy (Ctrl+C)
5. Press K2 → Should paste (Ctrl+V)

Expected: "Hello World" duplicated

**✅ PASS**: Keys send correct shortcuts  
**❌ FAIL**: Nothing happens → check BLE connection

---

### Test 6: Media Keys ✓
**Goal**: Verify media control

**Setup**: Open music player (Spotify, YouTube, etc.)

1. Start playing music
2. Press K10 (Play/Pause) → Should pause
3. Press K10 again → Should play
4. Rotate Encoder 1 CW → Volume up
5. Rotate Encoder 1 CCW → Volume down
6. Press Encoder 1 → Mute

**✅ PASS**: Media controls work  
**❌ FAIL**: Nothing happens → check Consumer Control

---

## Phase 2 Tests: Profiles & Storage

### Test 7: Profile Switching (Combo) ✓
**Goal**: Switch profiles via key combo

1. Open Notepad
2. Hold K1 + K4 together for ~1 second
3. Release both keys
4. Watch serial output

Expected (serial):
```
Combo triggered: K1 + K4
Switched to profile 1
Profile loaded: Media
```

5. Now press K1-K7 (should be media controls)
6. Hold K1 + K12 to switch back to General

**✅ PASS**: Profiles switch via combo  
**❌ FAIL**: No switch → hold longer or check combo code

---

### Test 8: In-Profile Switching ✓
**Goal**: Switch via mapped key

1. Switch to Profile 1 (K1 + K4)
2. Press K12 (mapped to switch to Profile 0)

Expected (serial):
```
Key 11 pressed
Switched to profile 0
Profile loaded: General
```

**✅ PASS**: K12 switches back to General  
**❌ FAIL**: Check K12 mapping

---

### Test 9: Profile Persistence ✓
**Goal**: Verify last profile is saved

1. Switch to Profile 1 (K1 + K4)
2. Wait 2 seconds
3. Press ESP32 reset button
4. Watch serial boot messages

Expected (serial):
```
...
Active Profile: 1 - Media
Micropad ready! Waiting for BLE connection...
```

**✅ PASS**: Profile 1 restored after reboot  
**❌ FAIL**: Shows Profile 0 → NVS issue

---

### Test 10: Storage Info ✓
**Goal**: Verify profiles are stored

Add this temporary code to `setup()` after `profileManager.init()`:

```cpp
DEBUG_PRINTF("Profile count: %d\n", profileManager.getProfileCount());
DEBUG_PRINTF("Storage: %d KB total, %d KB used\n",
    profileManager._storage.getTotalSpace() / 1024,
    profileManager._storage.getUsedSpace() / 1024);
```

Expected:
```
Profile count: 4
Storage: 1468 KB total, 16 KB used
```

**✅ PASS**: 4 profiles stored  
**❌ FAIL**: 0 profiles → LittleFS issue

---

## Advanced Tests (Optional)

### Test 11: Profile Templates ✓
Test each profile's functionality:

**Profile 0 (General)**: Already tested above  
**Profile 1 (Media)**: K1=Prev, K2=Play, K3=Next, K5=Vol-, K6=Mute, K7=Vol+  
**Profile 2 (VS Code)**: Open VS Code, test shortcuts  
**Profile 3 (Creative)**: Open Photoshop, test tools  

---

### Test 12: Combo Hold Time ✓
**Goal**: Verify timing threshold

1. Press K1 + K4 together
2. Release after 0.5 seconds (too short)
   → Should NOT switch

3. Press K1 + K4 together
4. Hold for 1 second (long enough)
   → Should switch

**✅ PASS**: Requires ~800ms hold  
**❌ FAIL**: Switches instantly → check threshold

---

### Test 13: Multiple Keys Simultaneously ✓
**Goal**: Verify matrix can handle multiple keys

1. Press K1, K2, K3 all at once
2. Check serial output

Expected:
```
Key 0 pressed
Key 1 pressed
Key 2 pressed
```

**✅ PASS**: All keys detected  
**❌ FAIL**: Ghosting → check diodes

---

## Common Issues & Fixes

### Issue: Keys stuck or repeating
**Cause**: Debounce too short or wiring issue  
**Fix**: Increase `DEBOUNCE_MS` in config.h (try 10-20ms)

### Issue: Encoder misses steps
**Cause**: Polling too slow or bad encoder  
**Fix**: Check `stepsPerDetent` setting (try 2 or 1)

### Issue: BLE disconnects frequently
**Cause**: Power issue or interference  
**Fix**: Use powered USB hub, reduce WiFi interference

### Issue: "Failed to initialize LittleFS"
**Cause**: Corrupted partition  
**Fix**: Erase flash completely:
```bash
pio run -t erase
pio run -t upload
```

### Issue: Profiles not saving
**Cause**: NVS or LittleFS issue  
**Fix**: Factory reset via code:
```cpp
profileManager.factoryReset();
```

---

## Test Results Checklist

**Phase 1 - Core Input:**
- [ ] All 12 keys detected
- [ ] Both encoders rotate correctly
- [ ] Both encoder buttons work
- [ ] BLE pairs with PC
- [ ] Keyboard shortcuts work
- [ ] Media controls work

**Phase 2 - Profiles:**
- [ ] Profile switching via combo (K1+K4)
- [ ] In-profile switching (K12)
- [ ] Profile persists after reboot
- [ ] 4 profiles stored in flash
- [ ] Each profile template works

**Overall:**
- [ ] No compile errors
- [ ] No runtime crashes
- [ ] Stable BLE connection
- [ ] Low latency (<50ms)

---

## Performance Benchmarks

### Expected Performance:
- **Key scan rate**: ~1000 Hz (1ms loop)
- **Key latency**: <10ms (press to HID)
- **Encoder response**: <5ms
- **Profile switch**: <100ms
- **Storage write**: <500ms
- **Boot time**: ~3-5 seconds

### Measuring Latency:
Add timestamps to test:
```cpp
uint32_t start = millis();
actionExecutor.execute(action);
DEBUG_PRINTF("Action took %dms\n", millis() - start);
```

---

## Ready for Phase 3?

Once all tests pass:
✅ Hardware is working perfectly  
✅ Input is reliable and fast  
✅ Profiles are persistent  
✅ Ready for BLE config service!

**Next**: Implement BLE GATT service for Windows app communication 🚀
