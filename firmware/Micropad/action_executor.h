#ifndef ACTION_EXECUTOR_H
#define ACTION_EXECUTOR_H

#include <Arduino.h>
#include "config.h"
#include "profile.h"
#include "ble_hid.h"

class ActionExecutor {
public:
    ActionExecutor();
    void init(BLEKeyboard* bleKeyboard);
    
    void execute(const Action& action);
    
private:
    BLEKeyboard* _bleKeyboard;
    
    void _executeHotkey(const HotkeyConfig& config);
    void _executeText(const TextConfig& config);
    void _executeMedia(const MediaConfig& config);
    void _executeMouse(const MouseConfig& config);
};

#endif // ACTION_EXECUTOR_H
