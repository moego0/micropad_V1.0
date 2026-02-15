#include "ble_hid.h"

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

BLEKeyboard::BLEKeyboard() {
    _connected = false;
    _clearKeyboardReport();
    memset(_mouseReport, 0, sizeof(_mouseReport));
}

void BLEKeyboard::begin(const char* deviceName, const char* manufacturer) {
    DEBUG_PRINTLN("Initializing BLE HID...");
    
    // Initialize NimBLE
    NimBLEDevice::init(deviceName);
    
    // Create HID device
    _hid = new NimBLEHIDDevice(NimBLEDevice::getServer());
    
    // Set device info
    _hid->manufacturer(manufacturer);
    _hid->pnp(0x02, 0x05ac, 0x820a, 0x0210);
    _hid->hidInfo(0x00, 0x01);
    
    // Set report maps
    _hid->reportMap((uint8_t*)_hidReportDescriptorKeyboard, sizeof(_hidReportDescriptorKeyboard));
    _hid->startServices();
    
    // Get characteristics
    _inputKeyboard = _hid->inputReport(1);
    _inputMediaKeys = _hid->inputReport(2);
    _inputMouse = _hid->inputReport(3);
    
    // Start advertising
    NimBLEAdvertising* advertising = NimBLEDevice::getAdvertising();
    advertising->setAppearance(0x03C1);  // Keyboard appearance
    advertising->addServiceUUID(_hid->hidService()->getUUID());
    advertising->start();
    
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
    _connected = NimBLEDevice::getServer()->getConnectedCount() > 0;
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
    if (!_connected) return;
    
    uint8_t report[2] = {0, 0};
    
    // Map media key to bit position
    switch (key) {
        case MEDIA_NEXT_TRACK: report[0] = 0x01; break;
        case MEDIA_PREV_TRACK: report[0] = 0x02; break;
        case MEDIA_STOP: report[0] = 0x04; break;
        case MEDIA_PLAY_PAUSE: report[0] = 0x08; break;
        case MEDIA_MUTE: report[0] = 0x10; break;
        case MEDIA_VOLUME_UP: report[0] = 0x20; break;
        case MEDIA_VOLUME_DOWN: report[0] = 0x40; break;
    }
    
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
    if (!_connected) return;
    
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
    if (!_connected) return;
    
    _mouseReport[0] = 0;  // No buttons
    _mouseReport[1] = x;
    _mouseReport[2] = y;
    _mouseReport[3] = 0;  // No wheel
    
    _inputMouse->setValue(_mouseReport, 4);
    _inputMouse->notify();
}

void BLEKeyboard::sendMouseScroll(int8_t wheel) {
    if (!_connected) return;
    
    _mouseReport[0] = 0;  // No buttons
    _mouseReport[1] = 0;  // No X
    _mouseReport[2] = 0;  // No Y
    _mouseReport[3] = wheel;
    
    _inputMouse->setValue(_mouseReport, 4);
    _inputMouse->notify();
}

void BLEKeyboard::_sendKeyboardReport() {
    _inputKeyboard->setValue(_keyReport, 8);
    _inputKeyboard->notify();
}

void BLEKeyboard::_clearKeyboardReport() {
    memset(_keyReport, 0, sizeof(_keyReport));
}
