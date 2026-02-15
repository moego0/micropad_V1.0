#ifndef BLE_HID_H
#define BLE_HID_H

#include <Arduino.h>
#include <NimBLEDevice.h>
#include <NimBLEHIDDevice.h>
#include "../config.h"

// HID Key codes (standard USB HID)
#define KEY_A 0x04
#define KEY_B 0x05
#define KEY_C 0x06
#define KEY_D 0x07
#define KEY_E 0x08
#define KEY_F 0x09
#define KEY_G 0x0A
#define KEY_H 0x0B
#define KEY_I 0x0C
#define KEY_J 0x0D
#define KEY_K 0x0E
#define KEY_L 0x0F
#define KEY_M 0x10
#define KEY_N 0x11
#define KEY_O 0x12
#define KEY_P 0x13
#define KEY_Q 0x14
#define KEY_R 0x15
#define KEY_S 0x16
#define KEY_T 0x17
#define KEY_U 0x18
#define KEY_V 0x19
#define KEY_W 0x1A
#define KEY_X 0x1B
#define KEY_Y 0x1C
#define KEY_Z 0x1D

#define KEY_1 0x1E
#define KEY_2 0x1F
#define KEY_3 0x20
#define KEY_4 0x21
#define KEY_5 0x22
#define KEY_6 0x23
#define KEY_7 0x24
#define KEY_8 0x25
#define KEY_9 0x26
#define KEY_0 0x27

#define KEY_ENTER 0x28
#define KEY_ESC 0x29
#define KEY_BACKSPACE 0x2A
#define KEY_TAB 0x2B
#define KEY_SPACE 0x2C
#define KEY_MINUS 0x2D
#define KEY_PERIOD 0x37
#define KEY_SLASH 0x38
#define KEY_SEMICOLON 0x33
#define KEY_F1 0x3A
#define KEY_F2 0x3B
#define KEY_F3 0x3C
#define KEY_F4 0x3D
#define KEY_F5 0x3E
#define KEY_F6 0x3F
#define KEY_F7 0x40
#define KEY_F8 0x41
#define KEY_F9 0x42
#define KEY_F10 0x43
#define KEY_F11 0x44
#define KEY_F12 0x45

#define KEY_LEFT_ARROW 0x50
#define KEY_DOWN_ARROW 0x51
#define KEY_UP_ARROW 0x52
#define KEY_RIGHT_ARROW 0x4F

// Modifier keys
#define MODIFIER_LEFT_CTRL 0x01
#define MODIFIER_LEFT_SHIFT 0x02
#define MODIFIER_LEFT_ALT 0x04
#define MODIFIER_LEFT_GUI 0x08
#define MODIFIER_RIGHT_CTRL 0x10
#define MODIFIER_RIGHT_SHIFT 0x20
#define MODIFIER_RIGHT_ALT 0x40
#define MODIFIER_RIGHT_GUI 0x80

// Consumer Control codes (media keys)
#define MEDIA_VOLUME_UP 0xE9
#define MEDIA_VOLUME_DOWN 0xEA
#define MEDIA_MUTE 0xE2
#define MEDIA_PLAY_PAUSE 0xCD
#define MEDIA_NEXT_TRACK 0xB5
#define MEDIA_PREV_TRACK 0xB6
#define MEDIA_STOP 0xB7

// Mouse buttons
#define MOUSE_LEFT 0x01
#define MOUSE_RIGHT 0x02
#define MOUSE_MIDDLE 0x04

class BLEKeyboard {
public:
    BLEKeyboard();
    void begin(const char* deviceName, const char* manufacturer = "Custom");
    void end();
    
    // Connection status
    bool isConnected();
    
    // Keyboard functions
    void sendKeyPress(uint8_t key, uint8_t modifiers = 0);
    void sendKeyDown(uint8_t key, uint8_t modifiers = 0);
    void sendKeyUp();
    void sendText(const char* text);
    
    // Media keys
    void sendMediaKey(uint16_t key);
    
    // Mouse functions
    void sendMouseClick(uint8_t button);
    void sendMouseMove(int8_t x, int8_t y);
    void sendMouseScroll(int8_t wheel);
    
    // Update connection status
    void update();
    
    // Restart advertising if not connected (so Windows can connect again after a failed attempt)
    void restartAdvertisingIfNeeded();
    
private:
    NimBLEHIDDevice* _hid;
    NimBLECharacteristic* _inputKeyboard;
    NimBLECharacteristic* _inputMediaKeys;
    NimBLECharacteristic* _inputMouse;
    bool _connected;
    
    uint8_t _keyReport[8];
    uint8_t _mouseReport[4];
    
    void _sendKeyboardReport();
    void _clearKeyboardReport();
};

#endif // BLE_HID_H
