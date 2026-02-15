#include "ble_hid.h"
#include <string>
// NimBLE2904 for Battery Level descriptor (Windows pairing); may be inside NimBLEDevice.h
#if __has_include(<NimBLE2904.h>)
#include <NimBLE2904.h>
#endif

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
    _clearKeyboardReport();
    memset(_mouseReport, 0, sizeof(_mouseReport));
}

void BLEKeyboard::begin(const char* deviceName, const char* manufacturer) {
    NimBLEDevice::init(deviceName);
    NimBLEServer* pServer = NimBLEDevice::createServer();
    if (!pServer) return;

    NimBLEDevice::setPower(ESP_PWR_LVL_P9);
    NimBLEDevice::setSecurityAuth(false, false, false);
    NimBLEDevice::setSecurityIOCap(BLE_HS_IO_NO_INPUT_OUTPUT);

    // Device Info (Windows needs this)
    NimBLEService* pDeviceInfo = pServer->createService(NimBLEUUID((uint16_t)0x180A));
    NimBLECharacteristic* pChar;
    pChar = pDeviceInfo->createCharacteristic(NimBLEUUID((uint16_t)0x2A29), NIMBLE_PROPERTY::READ);
    pChar->setValue(manufacturer);
    pChar = pDeviceInfo->createCharacteristic(NimBLEUUID((uint16_t)0x2A24), NIMBLE_PROPERTY::READ);
    pChar->setValue("Micropad-v1.0");
    char serial[32];
    uint64_t chipid = ESP.getEfuseMac();
    snprintf(serial, sizeof(serial), "%04X%08X", (uint16_t)(chipid >> 32), (uint32_t)chipid);
    pChar = pDeviceInfo->createCharacteristic(NimBLEUUID((uint16_t)0x2A25), NIMBLE_PROPERTY::READ);
    pChar->setValue(serial);
    pChar = pDeviceInfo->createCharacteristic(NimBLEUUID((uint16_t)0x2A26), NIMBLE_PROPERTY::READ);
    pChar->setValue(FIRMWARE_VERSION);
    pChar = pDeviceInfo->createCharacteristic(NimBLEUUID((uint16_t)0x2A27), NIMBLE_PROPERTY::READ);
    pChar->setValue(HARDWARE_VERSION);
    pChar = pDeviceInfo->createCharacteristic(NimBLEUUID((uint16_t)0x2A28), NIMBLE_PROPERTY::READ);
    pChar->setValue(FIRMWARE_VERSION);
    uint8_t pnp[] = { 0x02, 0x5E, 0x04, 0x01, 0x00, 0x00, 0x01 };
    pChar = pDeviceInfo->createCharacteristic(NimBLEUUID((uint16_t)0x2A50), NIMBLE_PROPERTY::READ);
    pChar->setValue(pnp, sizeof(pnp));
    pDeviceInfo->start();

    // Battery (Windows pairing)
    NimBLEService* pBattery = pServer->createService(NimBLEUUID((uint16_t)0x180F));
    NimBLECharacteristic* pBatteryLevel = pBattery->createCharacteristic(
        NimBLEUUID((uint16_t)0x2A19), NIMBLE_PROPERTY::READ | NIMBLE_PROPERTY::NOTIFY);
#if __has_include(<NimBLE2904.h>)
    NimBLE2904* pDesc = (NimBLE2904*)pBatteryLevel->createDescriptor(NimBLEUUID((uint16_t)0x2904));
    if (pDesc) {
        pDesc->setFormat(NimBLE2904::FORMAT_UINT8);
        pDesc->setNamespace(1);
        pDesc->setUnit(0x27AD);
    }
#endif
    uint8_t batteryLevel = 100;
    pBatteryLevel->setValue(&batteryLevel, 1);
    pBattery->start();

    // HID (keyboard + media + mouse)
    _hid = new NimBLEHIDDevice(pServer);
    if (!_hid) return;
    _hid->setManufacturer(manufacturer);
    _hid->setPnp(0x02, 0x045E, 0x0001, 0x0100);
    _hid->setHidInfo(0x11, 0x01);
    _hid->setReportMap((uint8_t*)_hidReportDescriptor, sizeof(_hidReportDescriptor));
    _hid->startServices();
    _inputKeyboard = _hid->getInputReport(1);
    _inputMediaKeys = _hid->getInputReport(2);
    _inputMouse = _hid->getInputReport(3);
    if (!_inputKeyboard || !_inputMediaKeys || !_inputMouse) return;

    // Advertise and start
    NimBLEAdvertising* pAdvertising = NimBLEDevice::getAdvertising();
    pAdvertising->setName(std::string(deviceName));
    pAdvertising->addServiceUUID(NimBLEUUID((uint16_t)0x1812));
    pAdvertising->enableScanResponse(true);
    NimBLEDevice::startAdvertising();
    delay(100);
}

void BLEKeyboard::end() {
    NimBLEDevice::deinit(true);
    _connected = false;
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
    _connected = (pServer && pServer->getConnectedCount() > 0);
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
    if (!_connected || !_inputMediaKeys) return;

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
    if (!_connected || !_inputMouse) return;

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
    if (!_connected || !_inputMouse) return;

    _mouseReport[0] = 0;  // No buttons
    _mouseReport[1] = x;
    _mouseReport[2] = y;
    _mouseReport[3] = 0;  // No wheel
    
    _inputMouse->setValue(_mouseReport, 4);
    _inputMouse->notify();
}

void BLEKeyboard::sendMouseScroll(int8_t wheel) {
    if (!_connected || !_inputMouse) return;

    _mouseReport[0] = 0;  // No buttons
    _mouseReport[1] = 0;  // No X
    _mouseReport[2] = 0;  // No Y
    _mouseReport[3] = wheel;
    
    _inputMouse->setValue(_mouseReport, 4);
    _inputMouse->notify();
}

void BLEKeyboard::_sendKeyboardReport() {
    if (!_inputKeyboard) return;
    _inputKeyboard->setValue(_keyReport, 8);
    _inputKeyboard->notify();
}

void BLEKeyboard::_clearKeyboardReport() {
    memset(_keyReport, 0, sizeof(_keyReport));
}
