#ifndef DEFAULT_PROFILE_H
#define DEFAULT_PROFILE_H

#include "profile.h"
#include "ble_hid.h"

// Create default profile (General use)
inline Profile createDefaultProfile() {
    Profile profile;
    
    profile.id = 0;
    strcpy(profile.name, "General");
    profile.version = 1;
    
    // Key 0: Ctrl+C (Copy)
    profile.keys[0].action.type = ACTION_HOTKEY;
    profile.keys[0].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[0].action.config.hotkey.key = KEY_C;
    
    // Key 1: Ctrl+V (Paste)
    profile.keys[1].action.type = ACTION_HOTKEY;
    profile.keys[1].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[1].action.config.hotkey.key = KEY_V;
    
    // Key 2: Ctrl+Z (Undo)
    profile.keys[2].action.type = ACTION_HOTKEY;
    profile.keys[2].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[2].action.config.hotkey.key = KEY_Z;
    
    // Key 3: Ctrl+Y (Redo)
    profile.keys[3].action.type = ACTION_HOTKEY;
    profile.keys[3].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[3].action.config.hotkey.key = KEY_Y;
    
    // Key 4: Alt+Tab (Switch Window)
    profile.keys[4].action.type = ACTION_HOTKEY;
    profile.keys[4].action.config.hotkey.modifiers = MODIFIER_LEFT_ALT;
    profile.keys[4].action.config.hotkey.key = KEY_TAB;
    
    // Key 5: Win+D (Show Desktop)
    profile.keys[5].action.type = ACTION_HOTKEY;
    profile.keys[5].action.config.hotkey.modifiers = MODIFIER_LEFT_GUI;
    profile.keys[5].action.config.hotkey.key = KEY_D;
    
    // Key 6: Win+Shift+S (Screenshot)
    profile.keys[6].action.type = ACTION_HOTKEY;
    profile.keys[6].action.config.hotkey.modifiers = MODIFIER_LEFT_GUI | MODIFIER_LEFT_SHIFT;
    profile.keys[6].action.config.hotkey.key = KEY_S;
    
    // Key 7: Win+E (Explorer) - Using just Win+E
    profile.keys[7].action.type = ACTION_HOTKEY;
    profile.keys[7].action.config.hotkey.modifiers = MODIFIER_LEFT_GUI;
    profile.keys[7].action.config.hotkey.key = KEY_E;
    
    // Key 8: Previous Track
    profile.keys[8].action.type = ACTION_MEDIA;
    profile.keys[8].action.config.media.function = MEDIA_FUNC_PREV;
    
    // Key 9: Play/Pause
    profile.keys[9].action.type = ACTION_MEDIA;
    profile.keys[9].action.config.media.function = MEDIA_FUNC_PLAY_PAUSE;
    
    // Key 10: Next Track
    profile.keys[10].action.type = ACTION_MEDIA;
    profile.keys[10].action.config.media.function = MEDIA_FUNC_NEXT;
    
    // Key 11: Open YouTube (types URL + Enter in address bar)
    profile.keys[11].action.type = ACTION_TEXT;
    strncpy(profile.keys[11].action.config.text.text, "https://www.youtube.com\n", sizeof(profile.keys[11].action.config.text.text) - 1);
    profile.keys[11].action.config.text.text[sizeof(profile.keys[11].action.config.text.text) - 1] = '\0';
    
    // Encoder 1 (Top-left): Volume control – turn for volume, press for mute
    profile.encoders[0].cwAction.type = ACTION_MEDIA;
    profile.encoders[0].cwAction.config.media.function = MEDIA_FUNC_VOLUME_UP;
    
    profile.encoders[0].ccwAction.type = ACTION_MEDIA;
    profile.encoders[0].ccwAction.config.media.function = MEDIA_FUNC_VOLUME_DOWN;
    
    profile.encoders[0].pressAction.type = ACTION_MEDIA;
    profile.encoders[0].pressAction.config.media.function = MEDIA_FUNC_MUTE;
    
    profile.encoders[0].acceleration = true;
    profile.encoders[0].stepsPerDetent = 4;
    
    // Encoder 2 (Top-right): Scroll Control
    profile.encoders[1].cwAction.type = ACTION_MOUSE;
    profile.encoders[1].cwAction.config.mouse.action = MOUSE_ACTION_SCROLL_DOWN;
    profile.encoders[1].cwAction.config.mouse.value = 3;
    
    profile.encoders[1].ccwAction.type = ACTION_MOUSE;
    profile.encoders[1].ccwAction.config.mouse.action = MOUSE_ACTION_SCROLL_UP;
    profile.encoders[1].ccwAction.config.mouse.value = 3;
    
    profile.encoders[1].pressAction.type = ACTION_MEDIA;
    profile.encoders[1].pressAction.config.media.function = MEDIA_FUNC_PLAY_PAUSE;
    
    profile.encoders[1].acceleration = true;
    profile.encoders[1].stepsPerDetent = 4;
    
    return profile;
}

#endif // DEFAULT_PROFILE_H
