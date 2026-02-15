#include "ble_hid.h"
#include <string>
// NimBLE2904 for Battery Level descriptor (Windows pairing); may be inside NimBLEDevice.h
#if __has_include(<NimBLE2904.h>)
#include <NimBLE2904.h>
#endif

// Server callbacks: set connection state and HID-ready delay; restart advertising on disconnect (BLE only, no SPP)
static BLEKeyboard* g_pKeyboard = nullptr;
static unsigned long s_lastBlockedLogMs = 0;
const unsigned long HID_READY_DELAY_MS = 1500;

class HidServerCallbacks : public NimBLEServerCallbacks {
public:
    void onConnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo) override {
        (void)pServer;
        Serial.printf("[BLE] Client connected, conn_id=%d\n", connInfo.getConnHandle());
        if (g_pKeyboard) g_pKeyboard->onConnect();
    }
    void onDisconnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo, int reason) override {
        (void)connInfo;
        Serial.printf("[BLE] Client disconnected, reason=%d (0x%02x)\n", reason, (unsigned)reason);
        if (g_pKeyboard) g_pKeyboard->onDisconnect(reason);
        NimBLEDevice::startAdvertising();
        Serial.println("[BLE] Advertising restarted");
    }
};

// Combined HID Report Descriptor (keyboard + LED, consumer 16-bit, mouse) - improves Windows driver compatibility
static const uint8_t _hidReportDescriptor[] = {
    // Keyboard Report (8 bytes) with LED output
    0x05, 0x01,        // Usage Page (Generic Desktop)
    0x09, 0x06,        // Usage (Keyboard)
    0xA1, 0x01,        // Collection (Application)
    0x85, 0x01,        //   Report ID (1) - KEYBOARD
    0x05, 0x07,        //   Usage Page (Key Codes)
    0x19, 0xE0,        //   Usage Minimum (224)
    0x29, 0xE7,        //   Usage Maximum (231)
    0x15, 0x00,        //   Logical Minimum (0)
    0x25, 0x01,        //   Logical Maximum (1)
    0x75, 0x01,        //   Report Size (1)
    0x95, 0x08,        //   Report Count (8)
    0x81, 0x02,        //   Input (Data, Variable, Absolute) - Modifier byte
    0x95, 0x01,        //   Report Count (1)
    0x75, 0x08,        //   Report Size (8)
    0x81, 0x01,        //   Input (Constant) - Reserved byte
    0x95, 0x05,        //   Report Count (5)
    0x75, 0x01,        //   Report Size (1)
    0x05, 0x08,        //   Usage Page (LEDs)
    0x19, 0x01,        //   Usage Minimum (1)
    0x29, 0x05,        //   Usage Maximum (5)
    0x91, 0x02,        //   Output (Data, Variable, Absolute) - LED report
    0x95, 0x01,        //   Report Count (1)
    0x75, 0x03,        //   Report Size (3)
    0x91, 0x01,        //   Output (Constant) - LED report padding
    0x95, 0x06,        //   Report Count (6)
    0x75, 0x08,        //   Report Size (8)
    0x15, 0x00,        //   Logical Minimum (0)
    0x26, 0xFF, 0x00,  //   Logical Maximum (255)
    0x05, 0x07,        //   Usage Page (Key Codes)
    0x19, 0x00,        //   Usage Minimum (0)
    0x29, 0xFF,        //   Usage Maximum (255)
    0x81, 0x00,        //   Input (Data, Array) - Key array (6 bytes)
    0xC0,              // End Collection

    // Consumer Control (Media Keys) - 16-bit usage
    0x05, 0x0C,        // Usage Page (Consumer)
    0x09, 0x01,        // Usage (Consumer Control)
    0xA1, 0x01,        // Collection (Application)
    0x85, 0x02,        //   Report ID (2) - CONSUMER
    0x19, 0x00,        //   Usage Minimum (0)
    0x2A, 0x3C, 0x02,  //   Usage Maximum (0x023C)
    0x15, 0x00,        //   Logical Minimum (0)
    0x26, 0x3C, 0x02,  //   Logical Maximum (0x023C)
    0x95, 0x01,        //   Report Count (1)
    0x75, 0x10,        //   Report Size (16)
    0x81, 0x00,        //   Input (Data, Array)
    0xC0,              // End Collection

    // Mouse Report
    0x05, 0x01,        // Usage Page (Generic Desktop)
    0x09, 0x02,        // Usage (Mouse)
    0xA1, 0x01,        // Collection (Application)
    0x85, 0x03,        //   Report ID (3) - MOUSE
    0x09, 0x01,        //   Usage (Pointer)
    0xA1, 0x00,        //   Collection (Physical)
    0x05, 0x09,        //     Usage Page (Buttons)
    0x19, 0x01,        //     Usage Minimum (1)
    0x29, 0x05,        //     Usage Maximum (5)
    0x15, 0x00,        //     Logical Minimum (0)
    0x25, 0x01,        //     Logical Maximum (1)
    0x95, 0x05,        //     Report Count (5)
    0x75, 0x01,        //     Report Size (1)
    0x81, 0x02,        //     Input (Data, Variable, Absolute) - Buttons
    0x95, 0x01,        //     Report Count (1)
    0x75, 0x03,        //     Report Size (3)
    0x81, 0x01,        //     Input (Constant) - Padding
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

BLEKeyboard::BLEKeyboard() {
    _hid = nullptr;
    _inputKeyboard = nullptr;
    _inputMediaKeys = nullptr;
    _inputMouse = nullptr;
    _connected = false;
    _readyAtMs = 0;
    _loggedHidReady = false;
    _clearKeyboardReport();
    memset(_mouseReport, 0, sizeof(_mouseReport));
}

void BLEKeyboard::begin(const char* deviceName, const char* manufacturer) {
    g_pKeyboard = this;
    Serial.println("[BLE] Initializing NimBLE (BLE only, no Classic SPP)...");
    NimBLEDevice::init(deviceName);
    NimBLEServer* pServer = NimBLEDevice::createServer();
    if (!pServer) {
        Serial.println("[BLE] ERROR: createServer failed");
        g_pKeyboard = nullptr;
        return;
    }
    pServer->setCallbacks(new HidServerCallbacks());

    // Windows-friendly security: bonding + encryption (required for HID), no passkey UI
    NimBLEDevice::setSecurityAuth(true, false, true);   // bonding ON, MITM OFF, secure connections ON
    NimBLEDevice::setSecurityIOCap(BLE_HS_IO_NO_INPUT_OUTPUT);
    NimBLEDevice::setPower(ESP_PWR_LVL_P9);
    Serial.println("[BLE] Security: bonding=ON, IOCap=NO_INPUT_OUTPUT, secure_conn=ON");

    // Use NimBLEHIDDevice only (no duplicate Device Info/Battery). It creates 0x180A, 0x1812, 0x180F
    // with required HID chars: 2A4A (HID Info), 2A4B (Report Map), 2A4C (Control), 2A4E (Protocol Mode),
    // 2A4D (Reports) with Report Reference 0x2908 and READ_ENC — required for Windows (avoids Event 411).
    _hid = new NimBLEHIDDevice(pServer);
    if (!_hid) {
        Serial.println("[BLE] ERROR: NimBLEHIDDevice failed");
        g_pKeyboard = nullptr;
        return;
    }
    _hid->setManufacturer(manufacturer);
    _hid->setPnp(0x02, 0x045E, 0x0001, 0x0100);
    _hid->setHidInfo(0x00, 0x01);   // country 0, flags RemoteWake (Windows expects valid HID info)
    _hid->setReportMap((uint8_t*)_hidReportDescriptor, sizeof(_hidReportDescriptor));
    // Create input report characteristics (adds 2A4D + 0x2908 + READ_ENC) before starting services
    _inputKeyboard = _hid->getInputReport(1);
    _inputMediaKeys = _hid->getInputReport(2);
    _inputMouse = _hid->getInputReport(3);
    if (!_inputKeyboard || !_inputMediaKeys || !_inputMouse) {
        Serial.println("[BLE] ERROR: HID input reports missing");
        g_pKeyboard = nullptr;
        return;
    }
    _hid->setBatteryLevel(100);
    // Optional: add model/serial/fw to library's Device Info before start (must be before startServices)
    NimBLEService* pDi = _hid->getDeviceInfoService();
    if (pDi) {
        NimBLECharacteristic* c = pDi->createCharacteristic(NimBLEUUID((uint16_t)0x2A24), NIMBLE_PROPERTY::READ);
        if (c) c->setValue("Micropad-v1.0");
        char serial[32];
        uint64_t chipid = ESP.getEfuseMac();
        snprintf(serial, sizeof(serial), "%04X%08X", (uint16_t)(chipid >> 32), (uint32_t)chipid);
        c = pDi->createCharacteristic(NimBLEUUID((uint16_t)0x2A25), NIMBLE_PROPERTY::READ);
        if (c) c->setValue(serial);
        c = pDi->createCharacteristic(NimBLEUUID((uint16_t)0x2A26), NIMBLE_PROPERTY::READ);
        if (c) c->setValue(FIRMWARE_VERSION);
    }
    _hid->startServices();   // starts Device Info, HID, Battery (single set — no duplicates)
    Serial.println("[BLE] HID service 0x1812: 2A4A,2A4B,2A4C,2A4E,2A4D+2908 READ_ENC; DeviceInfo 0x180A; Battery 0x180F");

    // Advertising: HID first, appearance keyboard (Windows expects for hidbthle)
    NimBLEAdvertising* pAdvertising = NimBLEDevice::getAdvertising();
    pAdvertising->setName(std::string(deviceName));
    pAdvertising->setAppearance(0x03C1);   // HID Keyboard (Windows HID-over-GATT)
    pAdvertising->addServiceUUID(NimBLEUUID((uint16_t)0x1812));
    pAdvertising->addServiceUUID(NimBLEUUID((uint16_t)0x180A));
    pAdvertising->addServiceUUID(NimBLEUUID((uint16_t)0x180F));
    pAdvertising->addServiceUUID(NimBLEUUID("4fafc201-1fb5-459e-8fcc-c5c9c331914b"));
    pAdvertising->enableScanResponse(true);
}

void BLEKeyboard::startAdvertising() {
    Serial.println("[BLE] Advertising started (name + 0x1812, 0x180A, 0x180F, config UUID)");
    NimBLEDevice::startAdvertising();
}

void BLEKeyboard::end() {
    g_pKeyboard = nullptr;
    NimBLEDevice::deinit(true);
    _connected = false;
    _readyAtMs = 0;
    _loggedHidReady = false;
}

void BLEKeyboard::onConnect() {
    _connected = true;
    _readyAtMs = millis() + HID_READY_DELAY_MS;
    _loggedHidReady = false;
    Serial.printf("[BLE] HID ready in %lu ms (no reports until then)\n", (unsigned long)HID_READY_DELAY_MS);
}

void BLEKeyboard::onDisconnect(int reason) {
    (void)reason;
    _connected = false;
    _readyAtMs = 0;
    _loggedHidReady = false;
}

bool BLEKeyboard::isHidReady() const {
    return _connected && (_readyAtMs != 0) && (millis() >= _readyAtMs);
}

void BLEKeyboard::restartAdvertisingIfNeeded() {
    NimBLEServer* pServer = NimBLEDevice::getServer();
    if (!pServer) return;
    if (pServer->getConnectedCount() == 0) {
        static unsigned long lastAdv = 0;
        if (millis() - lastAdv > 30000) {
            lastAdv = millis();
            NimBLEDevice::startAdvertising();
        }
    }
}

bool BLEKeyboard::isConnected() {
    return _connected;
}

void BLEKeyboard::update() {
    NimBLEServer* pServer = NimBLEDevice::getServer();
    if (!pServer || pServer->getConnectedCount() == 0) {
        if (_connected) {
            _connected = false;
            _readyAtMs = 0;
            _loggedHidReady = false;
        }
        return;
    }
    // When HID becomes ready, log once
    if (_connected && _readyAtMs != 0 && millis() >= _readyAtMs && !_loggedHidReady) {
        _loggedHidReady = true;
        Serial.println("[BLE] HID ready, reports enabled");
    }
}

void BLEKeyboard::sendKeyPress(uint8_t key, uint8_t modifiers) {
    if (!isHidReady()) {
        if (millis() - s_lastBlockedLogMs >= 2000) { s_lastBlockedLogMs = millis(); Serial.println("[BLE] report blocked (HID not ready)"); }
        return;
    }
    sendKeyDown(key, modifiers);
    delay(10);
    sendKeyUp();
}

void BLEKeyboard::sendKeyDown(uint8_t key, uint8_t modifiers) {
    if (!isHidReady()) return;
    _keyReport[0] = modifiers;
    _keyReport[2] = key;
    _sendKeyboardReport();
}

void BLEKeyboard::sendKeyUp() {
    if (!isHidReady()) return;
    _clearKeyboardReport();
    _sendKeyboardReport();
}

void BLEKeyboard::sendText(const char* text) {
    if (!isHidReady()) return;
    
    while (*text) {
        uint8_t key = 0;
        uint8_t modifiers = 0;
        
        char c = *text;
        
        // Convert character to HID key code
        if (c >= 'a' && c <= 'z') {
            key = KEY_A + (c - 'a');
        } else if (c >= 'A' && c <= 'Z') {
            key = KEY_A + (c - 'A');
            modifiers = MODIFIER_LEFT_SHIFT;
        } else if (c >= '1' && c <= '9') {
            key = KEY_1 + (c - '1');
        } else if (c == '0') {
            key = KEY_0;
        } else if (c == ' ') {
            key = KEY_SPACE;
        } else if (c == '\n') {
            key = KEY_ENTER;
        } else if (c == '.') {
            key = KEY_PERIOD;
        } else if (c == '/') {
            key = KEY_SLASH;
        } else if (c == ':') {
            key = KEY_SEMICOLON;
            modifiers = MODIFIER_LEFT_SHIFT;
        } else if (c == '-') {
            key = KEY_MINUS;
        }
        
        if (key != 0) {
            sendKeyPress(key, modifiers);
            delay(10);
        }
        
        text++;
    }
}

void BLEKeyboard::sendMediaKey(uint16_t key) {
    if (!isHidReady() || !_inputMediaKeys) return;

    // Consumer control: 16-bit usage (low byte, high byte)
    uint8_t report[2] = { (uint8_t)(key & 0xFF), (uint8_t)((key >> 8) & 0xFF) };
    
    // Send press
    _inputMediaKeys->setValue(report, 2);
    _inputMediaKeys->notify();
    delay(10);
    
    // Send release
    report[0] = 0;
    report[1] = 0;
    _inputMediaKeys->setValue(report, 2);
    _inputMediaKeys->notify();
}

void BLEKeyboard::sendMouseClick(uint8_t button) {
    if (!isHidReady() || !_inputMouse) return;

    // Press
    _mouseReport[0] = button;
    _inputMouse->setValue(_mouseReport, 4);
    _inputMouse->notify();
    delay(10);
    
    // Release
    _mouseReport[0] = 0;
    _inputMouse->setValue(_mouseReport, 4);
    _inputMouse->notify();
}

void BLEKeyboard::sendMouseMove(int8_t x, int8_t y) {
    if (!isHidReady() || !_inputMouse) return;

    _mouseReport[0] = 0;  // No buttons
    _mouseReport[1] = x;
    _mouseReport[2] = y;
    _mouseReport[3] = 0;  // No wheel
    
    _inputMouse->setValue(_mouseReport, 4);
    _inputMouse->notify();
}

void BLEKeyboard::sendMouseScroll(int8_t wheel) {
    if (!isHidReady() || !_inputMouse) return;

    _mouseReport[0] = 0;  // No buttons
    _mouseReport[1] = 0;  // No X
    _mouseReport[2] = 0;  // No Y
    _mouseReport[3] = wheel;
    
    _inputMouse->setValue(_mouseReport, 4);
    _inputMouse->notify();
}

void BLEKeyboard::_sendKeyboardReport() {
    if (!isHidReady() || !_inputKeyboard) return;
    _inputKeyboard->setValue(_keyReport, 8);
    _inputKeyboard->notify();
}

void BLEKeyboard::_clearKeyboardReport() {
    memset(_keyReport, 0, sizeof(_keyReport));
}
