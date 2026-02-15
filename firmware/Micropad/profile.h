#ifndef PROFILE_H
#define PROFILE_H

#include <Arduino.h>
#include "config.h"

// Action types
enum ActionType {
    ACTION_NONE = 0,
    ACTION_HOTKEY,
    ACTION_MACRO,
    ACTION_TEXT,
    ACTION_MEDIA,
    ACTION_MOUSE,
    ACTION_LAYER,
    ACTION_PROFILE,
    ACTION_APP,
    ACTION_URL
};

// Media functions
enum MediaFunction {
    MEDIA_FUNC_VOLUME_UP = 0,
    MEDIA_FUNC_VOLUME_DOWN,
    MEDIA_FUNC_MUTE,
    MEDIA_FUNC_PLAY_PAUSE,
    MEDIA_FUNC_NEXT,
    MEDIA_FUNC_PREV,
    MEDIA_FUNC_STOP
};

// Mouse actions
enum MouseAction {
    MOUSE_ACTION_CLICK = 0,
    MOUSE_ACTION_RIGHT_CLICK,
    MOUSE_ACTION_MIDDLE_CLICK,
    MOUSE_ACTION_SCROLL_UP,
    MOUSE_ACTION_SCROLL_DOWN
};

// Action configuration structures
struct HotkeyConfig {
    uint8_t modifiers;  // Bitmask of modifier keys
    uint8_t key;        // HID key code
};

struct TextConfig {
    char text[128];
};

struct MediaConfig {
    MediaFunction function;
};

struct MouseConfig {
    MouseAction action;
    int8_t value;  // For scroll amount or move distance
};

struct ProfileSwitchConfig {
    uint8_t profileId;
};

// Generic action structure
struct Action {
    ActionType type;
    union {
        HotkeyConfig hotkey;
        TextConfig text;
        MediaConfig media;
        MouseConfig mouse;
        ProfileSwitchConfig profile;
    } config;
};

// Key configuration
struct KeyConfig {
    Action action;
};

// Encoder configuration
struct EncoderConfig {
    Action cwAction;      // Clockwise
    Action ccwAction;     // Counter-clockwise
    Action pressAction;   // Button press
    bool acceleration;
    uint8_t stepsPerDetent;
};

// Profile structure
struct Profile {
    uint8_t id;
    char name[32];
    uint8_t version;
    KeyConfig keys[MATRIX_KEYS];
    EncoderConfig encoders[2];
};

#endif // PROFILE_H
