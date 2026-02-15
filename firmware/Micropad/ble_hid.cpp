#include "comms/ble_hid.h"

// HID Report Descriptor for Keyboard
static const uint8_t _hidReportDescriptorKeyboard[] = {
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
    0x25, 0x65,        //   Logical Maximum (101)
    0x05, 0x07,        //   Usage Page (Key Codes)
    0x19, 0x00,        //   Usage Minimum (0)
    0x29, 0x65,        //   Usage Maximum (101)
    0x81, 0x00,        //   Input (Data, Array)
    0xC0               // End Collection
};

// HID Report Descriptor for Consumer Control (Media Keys)
static const uint8_t _hidReportDescriptorConsumer[] = {
    0x05, 0x0C,        // Usage Page (Consumer)
    0x09, 0x01,        // Usage (Consumer Control)
    0xA1, 0x01,        // Collection (Application)
    0x85, 0x02,        //   Report ID (2)
    0x15, 0x00,        //   Logical Minimum (0)
    0x25, 0x01,        //   Logical Maximum (1)
    0x75, 0x01,        //   Report Size (1)
    0x95, 0x10,        //   Report Count (16)
    0x09, 0xB5,        //   Usage (Scan Next Track)
    0x09, 0xB6,        //   Usage (Scan Previous Track)
    0x09, 0xB7,        //   Usage (Stop)
    0x09, 0xCD,        //   Usage (Play/Pause)
    0x09, 0xE2,        //   Usage (Mute)
    0x09, 0xE9,        //   Usage (Volume Up)
    0x09, 0xEA,        //   Usage (Volume Down)
    0x0A, 0x83, 0x01,  //   Usage (Media Select)
    0x81, 0x02,        //   Input (Data, Variable, Absolute)
    0xC0               // End Collection
};

// HID Report Descriptor for Mouse
static const uint8_t _hidReportDescriptorMouse[] = {
    0x05, 0x01,        // Usage Page (Generic Desktop)
    0x09, 0x02,        // Usage (Mouse)
    0xA1, 0x01,        // Collection (Application)
    0x85, 0x03,        //   Report ID (3)
    0x09, 0x01,        //   Usage (Pointer)
    0xA1, 0x00,        //   Collection (Physical)
    0x05, 0x09,        //     Usage Page (Buttons)
    0x19, 0x01,        //     Usage Minimum (1)
    0x29, 0x03,        //     Usage Maximum (3)
    0x15, 0x00,        //     Logical Minimum (0)
    0x25, 0x01,        //     Logical Maximum (1)
    0x95, 0x03,        //     Report Count (3)
    0x75, 0x01,        //     Report Size (1)
    0x81, 0x02,        //     Input (Data, Variable, Absolute)
    0x95, 0x01,        //     Report Count (1)
    0x75, 0x05,        //     Report Size (5)
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

// Combined HID report map (keyboard + consumer + mouse) so all report IDs exist
static const uint8_t _hidReportDescriptorCombined[] = {
    // Keyboard (Report ID 1)
    0x05, 0x01, 0x09, 0x06, 0xA1, 0x01, 0x85, 0x01,
    0x05, 0x07, 0x19, 0xE0, 0x29, 0xE7, 0x15, 0x00, 0x25, 0x01, 0x75, 0x01, 0x95, 0x08, 0x81, 0x02,
    0x95, 0x01, 0x75, 0x08, 0x81, 0x01,
    0x95, 0x06, 0x75, 0x08, 0x15, 0x00, 0x25, 0x65, 0x05, 0x07, 0x19, 0x00, 0x29, 0x65, 0x81, 0x00,
    0xC0,
    // Consumer (Report ID 2)
    0x05, 0x0C, 0x09, 0x01, 0xA1, 0x01, 0x85, 0x02,
    0x15, 0x00, 0x25, 0x01, 0x75, 0x01, 0x95, 0x10,
    0x09, 0xB5, 0x09, 0xB6, 0x09, 0xB7, 0x09, 0xCD, 0x09, 0xE2, 0x09, 0xE9, 0x09, 0xEA, 0x0A, 0x83, 0x01,
    0x81, 0x02,
    0xC0,
    // Mouse (Report ID 3)
    0x05, 0x01, 0x09, 0x02, 0xA1, 0x01, 0x85, 0x03,
    0x09, 0x01, 0xA1, 0x00,
    0x05, 0x09, 0x19, 0x01, 0x29, 0x03, 0x15, 0x00, 0x25, 0x01, 0x95, 0x03, 0x75, 0x01, 0x81, 0x02,
    0x95, 0x01, 0x75, 0x05, 0x81, 0x01,
    0x05, 0x01, 0x09, 0x30, 0x09, 0x31, 0x09, 0x38, 0x15, 0x81, 0x25, 0x7F, 0x75, 0x08, 0x95, 0x03, 0x81, 0x06,
    0xC0, 0xC0
};
static const size_t _hidReportDescriptorCombinedLen = sizeof(_hidReportDescriptorCombined);

BLEKeyboard::BLEKeyboard() {
    _hid = nullptr;
    _inputKeyboard = nullptr;
    _inputMediaKeys = nullptr;
    _inputMouse = nullptr;
    _connected = false;
    _clearKeyboardReport();
    memset(_mouseReport, 0, sizeof(_mouseReport));
}

void BLEKeyboard::begin(const char* deviceName, const char* manufacturer) {
    DEBUG_PRINTLN("Initializing BLE HID...");
    
    NimBLEDevice::init(deviceName);
    
    // No bonding/MITM - lets Windows connect without pairing prompts (HID "just works")
    NimBLEDevice::setSecurityAuth(false, false, false);
    NimBLEDevice::setSecurityIOCap(0x03);  // NoInputNoOutput = "just works" pairing
    
    // Must create server first - getServer() returns null until createServer() is called
    NimBLEServer* server = NimBLEDevice::createServer();
    if (!server) {
        DEBUG_PRINTLN("ERROR: BLE createServer failed");
        return;
    }
    
    _hid = new NimBLEHIDDevice(server);
    if (!_hid) {
        DEBUG_PRINTLN("ERROR: Failed to create HID device");
        return;
    }
    
    _hid->setManufacturer(manufacturer);
    _hid->setPnp(0x02, 0x05ac, 0x820a, 0x0210);
    _hid->setHidInfo(0x00, 0x01);
    
    // Combined report map so report IDs 1, 2, 3 all exist
    _hid->setReportMap((uint8_t*)_hidReportDescriptorCombined, _hidReportDescriptorCombinedLen);
    _hid->startServices();
    
    // Ensure server is started before advertising (required for connections)
    server->start();
    
    _inputKeyboard = _hid->getInputReport(1);
    _inputMediaKeys = _hid->getInputReport(2);
    _inputMouse = _hid->getInputReport(3);
    
    if (!_inputKeyboard) DEBUG_PRINTLN("WARN: Keyboard report null");
    if (!_inputMediaKeys) DEBUG_PRINTLN("WARN: Media report null");
    if (!_inputMouse) DEBUG_PRINTLN("WARN: Mouse report null");
    
    NimBLEAdvertising* advertising = NimBLEDevice::getAdvertising();
    if (advertising) {
        advertising->setAppearance(0x03C1);
        // Preferred connection params (1.25ms units): 20=25ms, 80=100ms - wide range for different PCs
        advertising->setPreferredParams(20, 80);
        NimBLEService* hidSvc = _hid->getHidService();
        if (hidSvc) {
            advertising->addServiceUUID(hidSvc->getUUID());
        }
        advertising->start();
    }
    
    DEBUG_PRINTLN("BLE HID started, waiting for connection...");
}

void BLEKeyboard::end() {
    NimBLEDevice::deinit(true);
    _connected = false;
}

bool BLEKeyboard::isConnected() {
    return _connected;
}

void BLEKeyboard::update() {
    NimBLEServer* server = NimBLEDevice::getServer();
    _connected = (server && server->getConnectedCount() > 0);
}

void BLEKeyboard::restartAdvertisingIfNeeded() {
    if (_connected) return;
    NimBLEAdvertising* adv = NimBLEDevice::getAdvertising();
    if (!adv || adv->isAdvertising()) return;
    static uint32_t lastAttempt = 0;
    if (millis() - lastAttempt < 2000) return;  // Throttle: try at most every 2s
    lastAttempt = millis();
    NimBLEDevice::startAdvertising();
}

void BLEKeyboard::sendKeyPress(uint8_t key, uint8_t modifiers) {
    if (!_connected) return;
    
    // Press
    sendKeyDown(key, modifiers);
    delay(10);
    
    // Release
    sendKeyUp();
}

void BLEKeyboard::sendKeyDown(uint8_t key, uint8_t modifiers) {
    if (!_connected) return;
    
    _keyReport[0] = modifiers;
    _keyReport[2] = key;
    _sendKeyboardReport();
}

void BLEKeyboard::sendKeyUp() {
    if (!_connected) return;
    
    _clearKeyboardReport();
    _sendKeyboardReport();
}

void BLEKeyboard::sendText(const char* text) {
    if (!_connected) return;
    
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
        }
        
        if (key != 0) {
            sendKeyPress(key, modifiers);
            delay(10);
        }
        
        text++;
    }
}

void BLEKeyboard::sendMediaKey(uint16_t key) {
    if (!_connected || !_inputMediaKeys) return;
    
    uint8_t report[2] = {0, 0};
    
    switch (key) {
        case MEDIA_NEXT_TRACK: report[0] = 0x01; break;
        case MEDIA_PREV_TRACK: report[0] = 0x02; break;
        case MEDIA_STOP: report[0] = 0x04; break;
        case MEDIA_PLAY_PAUSE: report[0] = 0x08; break;
        case MEDIA_MUTE: report[0] = 0x10; break;
        case MEDIA_VOLUME_UP: report[0] = 0x20; break;
        case MEDIA_VOLUME_DOWN: report[0] = 0x40; break;
    }
    
    _inputMediaKeys->setValue(report, 2);
    _inputMediaKeys->notify();
    delay(10);
    report[0] = 0;
    report[1] = 0;
    _inputMediaKeys->setValue(report, 2);
    _inputMediaKeys->notify();
}

void BLEKeyboard::sendMouseClick(uint8_t button) {
    if (!_connected || !_inputMouse) return;
    
    _mouseReport[0] = button;
    _inputMouse->setValue(_mouseReport, 4);
    _inputMouse->notify();
    delay(10);
    _mouseReport[0] = 0;
    _inputMouse->setValue(_mouseReport, 4);
    _inputMouse->notify();
}

void BLEKeyboard::sendMouseMove(int8_t x, int8_t y) {
    if (!_connected || !_inputMouse) return;
    
    _mouseReport[0] = 0;
    _mouseReport[1] = x;
    _mouseReport[2] = y;
    _mouseReport[3] = 0;  // No wheel
    
    _inputMouse->setValue(_mouseReport, 4);
    _inputMouse->notify();
}

void BLEKeyboard::sendMouseScroll(int8_t wheel) {
    if (!_connected || !_inputMouse) return;
    
    _mouseReport[0] = 0;
    _mouseReport[1] = 0;  // No X
    _mouseReport[2] = 0;  // No Y
    _mouseReport[3] = wheel;
    
    _inputMouse->setValue(_mouseReport, 4);
    _inputMouse->notify();
}

void BLEKeyboard::_sendKeyboardReport() {
    if (_inputKeyboard) {
        _inputKeyboard->setValue(_keyReport, 8);
        _inputKeyboard->notify();
    }
}

void BLEKeyboard::_clearKeyboardReport() {
    memset(_keyReport, 0, sizeof(_keyReport));
}
