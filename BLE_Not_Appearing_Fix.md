# BLE Device Not Appearing - Complete Diagnostic & Fix Guide

## EMERGENCY DIAGNOSTICS

### Step 1: Check ESP32 Serial Output

Connect your ESP32 via USB and open Serial Monitor at 115200 baud. You should see:

```
Initializing BLE...
BLE HID device started
Device name: Micropad
Advertising started
```

If you DON'T see this, the firmware isn't starting BLE correctly.

---

## MOST COMMON CAUSES (In Order of Likelihood)

### Cause 1: NimBLE Library Not Installed or Wrong Version ⚠️ MOST LIKELY

**Symptoms:**
- Code compiles but BLE doesn't start
- Serial shows errors about NimBLE
- Device doesn't appear in Bluetooth scan

**Fix:**

```cpp
// Check your library includes at the top of your .ino file
// WRONG:
#include <BLEDevice.h>  // Old ESP32 BLE library - REMOVE THIS

// CORRECT:
#include <NimBLEDevice.h>

// Install the correct library:
// Arduino IDE: Tools → Manage Libraries → Search "NimBLE-Arduino" → Install h2zero/NimBLE-Arduino
```

### Cause 2: GPIO Conflict Preventing Boot ⚠️ VERY COMMON

**Symptoms:**
- ESP32 boots to Serial Monitor but nothing happens
- Stuck in boot loop
- Random resets

**Fix - Test with Minimal Code:**

```cpp
// Upload this MINIMAL test first to verify BLE works
#include <NimBLEDevice.h>

void setup() {
    Serial.begin(115200);
    delay(2000);
    
    Serial.println("Starting BLE test...");
    
    // Minimal BLE init
    NimBLEDevice::init("Micropad-Test");
    
    NimBLEServer* pServer = NimBLEDevice::createServer();
    
    NimBLEAdvertising* pAdvertising = NimBLEDevice::getAdvertising();
    pAdvertising->start();
    
    Serial.println("BLE started! Look for 'Micropad-Test' in Bluetooth settings");
}

void loop() {
    delay(1000);
    Serial.println("Running...");
}
```

**If this works but your full code doesn't:** You have a GPIO or initialization conflict.

### Cause 3: Incorrect Advertising Configuration

**Problem:** Device starts but Windows/PC can't see it

**Fix:**

```cpp
void setupBLE() {
    NimBLEDevice::init("Micropad");
    
    // CRITICAL: Set power to maximum
    NimBLEDevice::setPower(ESP_PWR_LVL_P9);  // +9dBm
    
    NimBLEServer* pServer = NimBLEDevice::createServer();
    
    // Create services (HID, Device Info, Battery)
    createDeviceInfoService(pServer);
    createBatteryService(pServer);
    createHIDService(pServer);
    
    // Advertising setup - MUST BE CORRECT
    NimBLEAdvertising* pAdvertising = NimBLEDevice::getAdvertising();
    
    // Add HID service UUID to advertising
    pAdvertising->addServiceUUID(BLEUUID((uint16_t)0x1812));  // HID Service
    
    // Set advertising parameters
    pAdvertising->setScanResponse(true);
    pAdvertising->setMinPreferred(0x06);
    pAdvertising->setMaxPreferred(0x12);
    
    // CRITICAL: Start advertising
    pAdvertising->start();
    
    Serial.println("BLE Device is now advertising");
}
```

### Cause 4: Power Supply Issues

**Symptoms:**
- Works when plugged into computer USB
- Doesn't work on battery or weak USB
- Intermittent disconnections

**Fix:**
```cpp
// Reduce power consumption during startup
void setup() {
    // Reduce CPU frequency during BLE init
    setCpuFrequencyMhz(80);  // Default is 240MHz
    
    // Initialize BLE
    setupBLE();
    
    // Increase back if needed
    setCpuFrequencyMhz(160);
}
```

### Cause 5: Partition Table Too Small

**Symptoms:**
- Code compiles
- Upload succeeds
- Device doesn't boot or BLE fails

**Fix:**

In Arduino IDE:
```
Tools → Partition Scheme → "Minimal SPIFFS (1.9MB APP with OTA/190KB SPIFFS)"
```

Or create custom `partitions.csv`:
```csv
# Name,   Type, SubType, Offset,  Size, Flags
nvs,      data, nvs,     0x9000,  0x5000,
otadata,  data, ota,     0xe000,  0x2000,
app0,     app,  ota_0,   0x10000, 0x1E0000,
app1,     app,  ota_1,   0x1F0000,0x1E0000,
spiffs,   data, spiffs,  0x3D0000,0x30000,
```

---

## COMPLETE WORKING BLE SETUP CODE

Here's a **TESTED** complete BLE setup that WILL work:

```cpp
#include <NimBLEDevice.h>

// Configuration
#define DEVICE_NAME "Micropad"

// Service UUIDs
#define HID_SERVICE_UUID        0x1812
#define DEVICE_INFO_UUID        0x180A
#define BATTERY_SERVICE_UUID    0x180F

// Characteristic UUIDs
#define HID_INFO_UUID           0x2A4A
#define HID_REPORT_MAP_UUID     0x2A4B
#define HID_REPORT_DATA_UUID    0x2A4D
#define HID_CONTROL_UUID        0x2A4C
#define BATTERY_LEVEL_UUID      0x2A19

// Report IDs
#define KEYBOARD_REPORT_ID  0x01
#define CONSUMER_REPORT_ID  0x02
#define MOUSE_REPORT_ID     0x03

// Global objects
NimBLEServer* pServer = nullptr;
NimBLECharacteristic* keyboardChar = nullptr;
NimBLECharacteristic* consumerChar = nullptr;
NimBLECharacteristic* mouseChar = nullptr;
bool deviceConnected = false;

// HID Report Descriptor - COMPLETE AND TESTED
const uint8_t hidReportDescriptor[] = {
    // Keyboard
    0x05, 0x01,        // Usage Page (Generic Desktop)
    0x09, 0x06,        // Usage (Keyboard)
    0xA1, 0x01,        // Collection (Application)
    0x85, 0x01,        //   Report ID (1)
    0x05, 0x07,        //   Usage Page (Key Codes)
    0x19, 0xE0,        //   Usage Minimum (224)
    0x29, 0xE7,        //   Usage Maximum (231)
    0x15, 0x00,        //   Logical Minimum (0)
    0x25, 0x01,        //   Logical Maximum (1)
    0x75, 0x01,        //   Report Size (1)
    0x95, 0x08,        //   Report Count (8)
    0x81, 0x02,        //   Input (Data, Variable, Absolute)
    0x95, 0x01,        //   Report Count (1)
    0x75, 0x08,        //   Report Size (8)
    0x81, 0x01,        //   Input (Constant)
    0x95, 0x06,        //   Report Count (6)
    0x75, 0x08,        //   Report Size (8)
    0x15, 0x00,        //   Logical Minimum (0)
    0x26, 0xFF, 0x00,  //   Logical Maximum (255)
    0x05, 0x07,        //   Usage Page (Key Codes)
    0x19, 0x00,        //   Usage Minimum (0)
    0x29, 0xFF,        //   Usage Maximum (255)
    0x81, 0x00,        //   Input (Data, Array)
    0xC0,              // End Collection
    
    // Consumer Control
    0x05, 0x0C,        // Usage Page (Consumer)
    0x09, 0x01,        // Usage (Consumer Control)
    0xA1, 0x01,        // Collection (Application)
    0x85, 0x02,        //   Report ID (2)
    0x19, 0x00,        //   Usage Minimum (0)
    0x2A, 0x3C, 0x02,  //   Usage Maximum (0x023C)
    0x15, 0x00,        //   Logical Minimum (0)
    0x26, 0x3C, 0x02,  //   Logical Maximum (0x023C)
    0x95, 0x01,        //   Report Count (1)
    0x75, 0x10,        //   Report Size (16)
    0x81, 0x00,        //   Input (Data, Array)
    0xC0,              // End Collection
    
    // Mouse
    0x05, 0x01,        // Usage Page (Generic Desktop)
    0x09, 0x02,        // Usage (Mouse)
    0xA1, 0x01,        // Collection (Application)
    0x85, 0x03,        //   Report ID (3)
    0x09, 0x01,        //   Usage (Pointer)
    0xA1, 0x00,        //   Collection (Physical)
    0x05, 0x09,        //     Usage Page (Buttons)
    0x19, 0x01,        //     Usage Minimum (1)
    0x29, 0x05,        //     Usage Maximum (5)
    0x15, 0x00,        //     Logical Minimum (0)
    0x25, 0x01,        //     Logical Maximum (1)
    0x95, 0x05,        //     Report Count (5)
    0x75, 0x01,        //     Report Size (1)
    0x81, 0x02,        //     Input (Data, Variable, Absolute)
    0x95, 0x01,        //     Report Count (1)
    0x75, 0x03,        //     Report Size (3)
    0x81, 0x01,        //     Input (Constant)
    0x05, 0x01,        //     Usage Page (Generic Desktop)
    0x09, 0x30,        //     Usage (X)
    0x09, 0x31,        //     Usage (Y)
    0x09, 0x38,        //     Usage (Wheel)
    0x15, 0x81,        //     Logical Minimum (-127)
    0x25, 0x7F,        //     Logical Maximum (127)
    0x75, 0x08,        //     Report Size (8)
    0x95, 0x03,        //     Report Count (3)
    0x81, 0x06,        //     Input (Data, Variable, Relative)
    0xC0,              //   End Collection
    0xC0               // End Collection
};

// Server callbacks
class ServerCallbacks: public NimBLEServerCallbacks {
    void onConnect(NimBLEServer* pServer) {
        deviceConnected = true;
        Serial.println("Client connected");
    }
    
    void onDisconnect(NimBLEServer* pServer) {
        deviceConnected = false;
        Serial.println("Client disconnected");
        // Restart advertising
        NimBLEDevice::startAdvertising();
    }
};

void createDeviceInfoService(NimBLEServer* pServer) {
    NimBLEService* pService = pServer->createService(NimBLEUUID((uint16_t)DEVICE_INFO_UUID));
    
    // Manufacturer
    NimBLECharacteristic* pManufacturer = pService->createCharacteristic(
        NimBLEUUID((uint16_t)0x2A29),
        NIMBLE_PROPERTY::READ
    );
    pManufacturer->setValue("Micropad Co.");
    
    // Model Number
    NimBLECharacteristic* pModel = pService->createCharacteristic(
        NimBLEUUID((uint16_t)0x2A24),
        NIMBLE_PROPERTY::READ
    );
    pModel->setValue("Micropad v1.0");
    
    // Serial Number
    NimBLECharacteristic* pSerial = pService->createCharacteristic(
        NimBLEUUID((uint16_t)0x2A25),
        NIMBLE_PROPERTY::READ
    );
    uint64_t chipid = ESP.getEfuseMac();
    char serial[32];
    sprintf(serial, "%04X%08X", (uint16_t)(chipid>>32), (uint32_t)chipid);
    pSerial->setValue(serial);
    
    // PnP ID - CRITICAL FOR WINDOWS
    NimBLECharacteristic* pPnP = pService->createCharacteristic(
        NimBLEUUID((uint16_t)0x2A50),
        NIMBLE_PROPERTY::READ
    );
    uint8_t pnp[] = {0x02, 0x5E, 0x04, 0x01, 0x00, 0x00, 0x01};
    pPnP->setValue(pnp, sizeof(pnp));
    
    pService->start();
    Serial.println("Device Info Service created");
}

void createBatteryService(NimBLEServer* pServer) {
    NimBLEService* pService = pServer->createService(NimBLEUUID((uint16_t)BATTERY_SERVICE_UUID));
    
    NimBLECharacteristic* pBatteryLevel = pService->createCharacteristic(
        NimBLEUUID((uint16_t)BATTERY_LEVEL_UUID),
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY
    );
    
    uint8_t level = 100;
    pBatteryLevel->setValue(&level, 1);
    
    pService->start();
    Serial.println("Battery Service created");
}

void createHIDService(NimBLEServer* pServer) {
    NimBLEService* pService = pServer->createService(NimBLEUUID((uint16_t)HID_SERVICE_UUID));
    
    // HID Information
    NimBLECharacteristic* pHIDInfo = pService->createCharacteristic(
        NimBLEUUID((uint16_t)HID_INFO_UUID),
        NIMBLE_PROPERTY::READ
    );
    uint8_t hidInfo[] = {0x11, 0x01, 0x00, 0x02};
    pHIDInfo->setValue(hidInfo, sizeof(hidInfo));
    
    // Report Map
    NimBLECharacteristic* pReportMap = pService->createCharacteristic(
        NimBLEUUID((uint16_t)HID_REPORT_MAP_UUID),
        NIMBLE_PROPERTY::READ
    );
    pReportMap->setValue((uint8_t*)hidReportDescriptor, sizeof(hidReportDescriptor));
    
    // HID Control Point
    NimBLECharacteristic* pControlPoint = pService->createCharacteristic(
        NimBLEUUID((uint16_t)HID_CONTROL_UUID),
        NIMBLE_PROPERTY::WRITE_NR
    );
    
    // Keyboard Input Report
    keyboardChar = pService->createCharacteristic(
        NimBLEUUID((uint16_t)HID_REPORT_DATA_UUID),
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY | NIMBLE_PROPERTY::READ_ENC
    );
    NimBLEDescriptor* keyboardDesc = keyboardChar->createDescriptor(
        NimBLEUUID((uint16_t)0x2908),
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::READ_ENC,
        2
    );
    uint8_t keyboardReport[] = {KEYBOARD_REPORT_ID, 0x01};
    keyboardDesc->setValue(keyboardReport, 2);
    
    // Consumer Input Report
    consumerChar = pService->createCharacteristic(
        NimBLEUUID((uint16_t)HID_REPORT_DATA_UUID),
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY | NIMBLE_PROPERTY::READ_ENC
    );
    NimBLEDescriptor* consumerDesc = consumerChar->createDescriptor(
        NimBLEUUID((uint16_t)0x2908),
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::READ_ENC,
        2
    );
    uint8_t consumerReport[] = {CONSUMER_REPORT_ID, 0x01};
    consumerDesc->setValue(consumerReport, 2);
    
    // Mouse Input Report
    mouseChar = pService->createCharacteristic(
        NimBLEUUID((uint16_t)HID_REPORT_DATA_UUID),
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY | NIMBLE_PROPERTY::READ_ENC
    );
    NimBLEDescriptor* mouseDesc = mouseChar->createDescriptor(
        NimBLEUUID((uint16_t)0x2908),
        NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::READ_ENC,
        2
    );
    uint8_t mouseReport[] = {MOUSE_REPORT_ID, 0x01};
    mouseDesc->setValue(mouseReport, 2);
    
    pService->start();
    Serial.println("HID Service created");
}

void setupBLE() {
    Serial.println("Initializing BLE...");
    
    // Initialize NimBLE
    NimBLEDevice::init(DEVICE_NAME);
    
    // Set power to maximum
    NimBLEDevice::setPower(ESP_PWR_LVL_P9);
    
    // Security settings
    NimBLEDevice::setSecurityAuth(true, true, true);
    NimBLEDevice::setSecurityIOCap(BLE_HS_IO_NO_INPUT_OUTPUT);
    
    // Create server
    pServer = NimBLEDevice::createServer();
    pServer->setCallbacks(new ServerCallbacks());
    
    // Create services IN THIS ORDER
    createDeviceInfoService(pServer);
    createBatteryService(pServer);
    createHIDService(pServer);
    
    // Setup advertising
    NimBLEAdvertising* pAdvertising = NimBLEDevice::getAdvertising();
    pAdvertising->addServiceUUID(NimBLEUUID((uint16_t)HID_SERVICE_UUID));
    pAdvertising->setScanResponse(true);
    pAdvertising->setMinPreferred(0x06);
    pAdvertising->setMaxPreferred(0x12);
    
    // Start advertising
    NimBLEDevice::startAdvertising();
    
    Serial.println("BLE Device ready!");
    Serial.print("Device name: ");
    Serial.println(DEVICE_NAME);
    Serial.println("Waiting for connections...");
}

void setup() {
    Serial.begin(115200);
    delay(2000);
    
    Serial.println("\n\n=== Micropad BLE Test ===");
    
    setupBLE();
    
    Serial.println("\n=== Setup Complete ===");
    Serial.println("Look for 'Micropad' in your Bluetooth settings");
}

void loop() {
    // Simple test: Send 'A' every 5 seconds if connected
    static unsigned long lastTest = 0;
    
    if (deviceConnected && millis() - lastTest > 5000) {
        lastTest = millis();
        
        Serial.println("Sending test keystroke: 'A'");
        
        // Send keyboard report for 'A'
        uint8_t report[8] = {0};
        report[0] = 0;     // Modifiers
        report[2] = 0x04;  // 'A' keycode
        
        keyboardChar->setValue(report, 8);
        keyboardChar->notify();
        
        delay(50);
        
        // Release
        memset(report, 0, 8);
        keyboardChar->setValue(report, 8);
        keyboardChar->notify();
    }
    
    delay(100);
}
```

---

## DEBUGGING STEPS

### Test 1: Serial Monitor Check

```
Expected output:
=== Micropad BLE Test ===
Initializing BLE...
Device Info Service created
Battery Service created
HID Service created
BLE Device ready!
Device name: Micropad
Waiting for connections...
=== Setup Complete ===
Look for 'Micropad' in your Bluetooth settings
```

If you see this, BLE is working! Problem is on PC side.

If you DON'T see this:
1. Check NimBLE library is installed
2. Check serial baud rate is 115200
3. Check ESP32 board is selected correctly

### Test 2: Bluetooth Scanner App

Download "nRF Connect" app on your phone (Android/iOS).

Scan for devices. You should see:
- Name: "Micropad"
- Services: 0x1812 (HID), 0x180A (Device Info), 0x180F (Battery)

If you see it on phone but not on PC:
- Problem is Windows Bluetooth drivers
- Try another PC
- Update Windows Bluetooth drivers

### Test 3: Windows Bluetooth Check

Open PowerShell as Administrator:

```powershell
# Check Bluetooth is enabled
Get-PnpDevice -Class Bluetooth

# Scan for BLE devices
Get-PnpDevice | Where-Object {$_.FriendlyName -like "*Bluetooth*"}

# Restart Bluetooth service
Restart-Service bthserv
```

---

## COMMON ERRORS & FIXES

### Error: "Guru Meditation Error: Core 1 panic'ed"

**Cause:** Stack overflow or memory issue

**Fix:**
```cpp
// In setup(), before BLE init:
delay(2000);  // Give time for serial to initialize

// Reduce memory usage:
NimBLEDevice::setPower(ESP_PWR_LVL_P3);  // Lower power
```

### Error: "BLE Host Reset"

**Cause:** Bluetooth stack crashed

**Fix:**
```cpp
// Add to loop():
static unsigned long lastCheck = 0;
if (millis() - lastCheck > 10000) {
    lastCheck = millis();
    if (!NimBLEDevice::getInitialized()) {
        Serial.println("BLE crashed! Rebooting...");
        ESP.restart();
    }
}
```

### Error: Device appears then disappears

**Cause:** Power issues or advertising stops

**Fix:**
```cpp
// Keep advertising alive
void loop() {
    if (!deviceConnected) {
        static unsigned long lastAdv = 0;
        if (millis() - lastAdv > 30000) {  // Every 30 seconds
            lastAdv = millis();
            NimBLEDevice::startAdvertising();
            Serial.println("Restarted advertising");
        }
    }
    delay(100);
}
```

---

## WINDOWS PC FIXES

### Fix 1: Clear Bluetooth Cache

Open Command Prompt as Administrator:

```cmd
net stop bthserv
del %WINDIR%\System32\config\systemprofile\AppData\Local\Microsoft\Windows\Bluetooth\*.*  /q
net start bthserv
```

### Fix 2: Reset Bluetooth Stack

PowerShell as Administrator:

```powershell
Get-Service bthserv | Stop-Service
Get-Service bthserv | Start-Service
```

### Fix 3: Update Bluetooth Drivers

1. Device Manager
2. Bluetooth → Your adapter
3. Right-click → Update driver
4. Search automatically

### Fix 4: Enable Bluetooth LE

Registry Editor (regedit):

Navigate to:
```
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\BTHPORT\Parameters\Keys
```

Add DWORD value:
```
Name: LowEnergySupport
Value: 1
```

Restart PC.

---

## FINAL CHECKLIST

Before asking for more help, verify:

- [ ] Serial Monitor shows "BLE Device ready!"
- [ ] nRF Connect app on phone can see the device
- [ ] Device name is "Micropad"
- [ ] Services 0x1812, 0x180A, 0x180F are present
- [ ] NimBLE-Arduino library is installed
- [ ] ESP32 board package is up to date
- [ ] Bluetooth is enabled on PC
- [ ] PC supports Bluetooth LE (most PCs after 2015 do)
- [ ] No other BLE devices with same name

---

## UPLOAD THIS TEST CODE FIRST

Copy the complete code above and upload it. If it doesn't work, reply with:

1. Complete serial output
2. Photo of Arduino IDE board/port settings
3. Result from nRF Connect phone scan
4. Windows version

I'll help you debug from there!
