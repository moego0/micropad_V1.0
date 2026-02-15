#ifndef PROFILE_TEMPLATES_H
#define PROFILE_TEMPLATES_H

#include "profile.h"
#include "ble_hid.h"

// VS Code Profile
inline Profile createVSCodeProfile() {
    Profile profile;
    profile.id = 2;
    strcpy(profile.name, "VS Code");
    profile.version = 1;
    
    // K1: Save (Ctrl+S)
    profile.keys[0].action.type = ACTION_HOTKEY;
    profile.keys[0].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[0].action.config.hotkey.key = KEY_S;
    
    // K2: Find (Ctrl+Shift+F)
    profile.keys[1].action.type = ACTION_HOTKEY;
    profile.keys[1].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL | MODIFIER_LEFT_SHIFT;
    profile.keys[1].action.config.hotkey.key = KEY_F;
    
    // K3: Quick Open (Ctrl+P)
    profile.keys[2].action.type = ACTION_HOTKEY;
    profile.keys[2].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[2].action.config.hotkey.key = KEY_P;
    
    // K4: Command Palette (Ctrl+Shift+P)
    profile.keys[3].action.type = ACTION_HOTKEY;
    profile.keys[3].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL | MODIFIER_LEFT_SHIFT;
    profile.keys[3].action.config.hotkey.key = KEY_P;
    
    // K5: Debug (F5)
    profile.keys[4].action.type = ACTION_HOTKEY;
    profile.keys[4].action.config.hotkey.modifiers = 0;
    profile.keys[4].action.config.hotkey.key = KEY_F5;
    
    // K6: Terminal (Ctrl+`)
    profile.keys[5].action.type = ACTION_HOTKEY;
    profile.keys[5].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[5].action.config.hotkey.key = 0x35;  // Backtick/Tilde key
    
    // K7: Comment (Ctrl+/)
    profile.keys[6].action.type = ACTION_HOTKEY;
    profile.keys[6].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[6].action.config.hotkey.key = 0x38;  // Slash key
    
    // K8: Format (Alt+Shift+F)
    profile.keys[7].action.type = ACTION_HOTKEY;
    profile.keys[7].action.config.hotkey.modifiers = MODIFIER_LEFT_ALT | MODIFIER_LEFT_SHIFT;
    profile.keys[7].action.config.hotkey.key = KEY_F;
    
    // K9: console.log();
    profile.keys[8].action.type = ACTION_TEXT;
    strcpy(profile.keys[8].action.config.text.text, "console.log();");
    
    // K10: Toggle Sidebar (Ctrl+B)
    profile.keys[9].action.type = ACTION_HOTKEY;
    profile.keys[9].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[9].action.config.hotkey.key = KEY_B;
    
    // K11: Split Editor (Ctrl+\)
    profile.keys[10].action.type = ACTION_HOTKEY;
    profile.keys[10].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[10].action.config.hotkey.key = 0x31;  // Backslash
    
    // K12: Back to General
    profile.keys[11].action.type = ACTION_PROFILE;
    profile.keys[11].action.config.profile.profileId = 0;
    
    // Encoder 1: Zoom
    profile.encoders[0].cwAction.type = ACTION_HOTKEY;
    profile.encoders[0].cwAction.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.encoders[0].cwAction.config.hotkey.key = 0x2E;  // = key
    
    profile.encoders[0].ccwAction.type = ACTION_HOTKEY;
    profile.encoders[0].ccwAction.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.encoders[0].ccwAction.config.hotkey.key = 0x2D;  // - key
    
    profile.encoders[0].pressAction.type = ACTION_HOTKEY;
    profile.encoders[0].pressAction.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.encoders[0].pressAction.config.hotkey.key = KEY_0;  // Reset zoom
    
    profile.encoders[0].acceleration = true;
    profile.encoders[0].stepsPerDetent = 4;
    
    // Encoder 2: Navigate
    profile.encoders[1].cwAction.type = ACTION_HOTKEY;
    profile.encoders[1].cwAction.config.hotkey.modifiers = MODIFIER_LEFT_ALT;
    profile.encoders[1].cwAction.config.hotkey.key = KEY_RIGHT_ARROW;
    
    profile.encoders[1].ccwAction.type = ACTION_HOTKEY;
    profile.encoders[1].ccwAction.config.hotkey.modifiers = MODIFIER_LEFT_ALT;
    profile.encoders[1].ccwAction.config.hotkey.key = KEY_LEFT_ARROW;
    
    profile.encoders[1].pressAction.type = ACTION_HOTKEY;
    profile.encoders[1].pressAction.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.encoders[1].pressAction.config.hotkey.key = KEY_P;
    
    profile.encoders[1].acceleration = false;
    profile.encoders[1].stepsPerDetent = 4;
    
    return profile;
}

// Photoshop/Creative Profile
inline Profile createCreativeProfile() {
    Profile profile;
    profile.id = 3;
    strcpy(profile.name, "Creative");
    profile.version = 1;
    
    // K1: Undo (Ctrl+Z)
    profile.keys[0].action.type = ACTION_HOTKEY;
    profile.keys[0].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[0].action.config.hotkey.key = KEY_Z;
    
    // K2: Redo (Ctrl+Shift+Z)
    profile.keys[1].action.type = ACTION_HOTKEY;
    profile.keys[1].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL | MODIFIER_LEFT_SHIFT;
    profile.keys[1].action.config.hotkey.key = KEY_Z;
    
    // K3: Save (Ctrl+S)
    profile.keys[2].action.type = ACTION_HOTKEY;
    profile.keys[2].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[2].action.config.hotkey.key = KEY_S;
    
    // K4: Save As (Ctrl+Shift+S)
    profile.keys[3].action.type = ACTION_HOTKEY;
    profile.keys[3].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL | MODIFIER_LEFT_SHIFT;
    profile.keys[3].action.config.hotkey.key = KEY_S;
    
    // K5: Brush Tool (B)
    profile.keys[4].action.type = ACTION_HOTKEY;
    profile.keys[4].action.config.hotkey.modifiers = 0;
    profile.keys[4].action.config.hotkey.key = KEY_B;
    
    // K6: Eraser Tool (E)
    profile.keys[5].action.type = ACTION_HOTKEY;
    profile.keys[5].action.config.hotkey.modifiers = 0;
    profile.keys[5].action.config.hotkey.key = KEY_E;
    
    // K7: New Layer (Ctrl+Shift+N)
    profile.keys[6].action.type = ACTION_HOTKEY;
    profile.keys[6].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL | MODIFIER_LEFT_SHIFT;
    profile.keys[6].action.config.hotkey.key = KEY_N;
    
    // K8: Merge Layers (Ctrl+E)
    profile.keys[7].action.type = ACTION_HOTKEY;
    profile.keys[7].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[7].action.config.hotkey.key = KEY_E;
    
    // K9: Free Transform (Ctrl+T)
    profile.keys[8].action.type = ACTION_HOTKEY;
    profile.keys[8].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[8].action.config.hotkey.key = KEY_T;
    
    // K10: Deselect (Ctrl+D)
    profile.keys[9].action.type = ACTION_HOTKEY;
    profile.keys[9].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.keys[9].action.config.hotkey.key = KEY_D;
    
    // K11: Invert Selection (Ctrl+Shift+I)
    profile.keys[10].action.type = ACTION_HOTKEY;
    profile.keys[10].action.config.hotkey.modifiers = MODIFIER_LEFT_CTRL | MODIFIER_LEFT_SHIFT;
    profile.keys[10].action.config.hotkey.key = KEY_I;
    
    // K12: Back to General
    profile.keys[11].action.type = ACTION_PROFILE;
    profile.keys[11].action.config.profile.profileId = 0;
    
    // Encoder 1: Brush Size
    profile.encoders[0].cwAction.type = ACTION_HOTKEY;
    profile.encoders[0].cwAction.config.hotkey.modifiers = 0;
    profile.encoders[0].cwAction.config.hotkey.key = 0x2F;  // ] key
    
    profile.encoders[0].ccwAction.type = ACTION_HOTKEY;
    profile.encoders[0].ccwAction.config.hotkey.modifiers = 0;
    profile.encoders[0].ccwAction.config.hotkey.key = 0x2E;  // [ key
    
    profile.encoders[0].pressAction.type = ACTION_HOTKEY;
    profile.encoders[0].pressAction.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.encoders[0].pressAction.config.hotkey.key = KEY_Z;
    
    profile.encoders[0].acceleration = true;
    profile.encoders[0].stepsPerDetent = 4;
    
    // Encoder 2: Zoom
    profile.encoders[1].cwAction.type = ACTION_HOTKEY;
    profile.encoders[1].cwAction.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.encoders[1].cwAction.config.hotkey.key = 0x57;  // + key
    
    profile.encoders[1].ccwAction.type = ACTION_HOTKEY;
    profile.encoders[1].ccwAction.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.encoders[1].ccwAction.config.hotkey.key = 0x56;  // - key
    
    profile.encoders[1].pressAction.type = ACTION_HOTKEY;
    profile.encoders[1].pressAction.config.hotkey.modifiers = MODIFIER_LEFT_CTRL;
    profile.encoders[1].pressAction.config.hotkey.key = KEY_0;
    
    profile.encoders[1].acceleration = true;
    profile.encoders[1].stepsPerDetent = 4;
    
    return profile;
}

#endif // PROFILE_TEMPLATES_H
