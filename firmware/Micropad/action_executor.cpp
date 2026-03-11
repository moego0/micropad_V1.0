#include "action_executor.h"

ActionExecutor::ActionExecutor() {
    _bleKeyboard = nullptr;
}

void ActionExecutor::init(BLEKeyboard* bleKeyboard) {
    _bleKeyboard = bleKeyboard;
}

void ActionExecutor::execute(const Action& action) {
    if (!_bleKeyboard || !_bleKeyboard->isHidReady()) {
        return;
    }
    
    switch (action.type) {
        case ACTION_HOTKEY:
            _executeHotkey(action.config.hotkey);
            break;
            
        case ACTION_TEXT:
            _executeText(action.config.text);
            break;
            
        case ACTION_MEDIA:
            _executeMedia(action.config.media);
            break;
            
        case ACTION_MOUSE:
            _executeMouse(action.config.mouse);
            break;
            
        case ACTION_MACRO:
            _executeMacro(action.config.macro);
            break;
            
        case ACTION_NONE:
        default:
            break;
    }
}

void ActionExecutor::_executeHotkey(const HotkeyConfig& config) {
    _bleKeyboard->sendKeyPress(config.key, config.modifiers);
    DEBUG_PRINTF("Executed hotkey: mod=0x%02X key=0x%02X\n", config.modifiers, config.key);
}

void ActionExecutor::_executeText(const TextConfig& config) {
    _bleKeyboard->sendText(config.text);
    DEBUG_PRINTF("Executed text: %s\n", config.text);
}

void ActionExecutor::_executeMedia(const MediaConfig& config) {
    uint16_t mediaKey = 0;
    
    switch (config.function) {
        case MEDIA_FUNC_VOLUME_UP:
            mediaKey = MEDIA_VOLUME_UP;
            break;
        case MEDIA_FUNC_VOLUME_DOWN:
            mediaKey = MEDIA_VOLUME_DOWN;
            break;
        case MEDIA_FUNC_MUTE:
            mediaKey = MEDIA_MUTE;
            break;
        case MEDIA_FUNC_PLAY_PAUSE:
            mediaKey = MEDIA_PLAY_PAUSE;
            break;
        case MEDIA_FUNC_NEXT:
            mediaKey = MEDIA_NEXT_TRACK;
            break;
        case MEDIA_FUNC_PREV:
            mediaKey = MEDIA_PREV_TRACK;
            break;
        case MEDIA_FUNC_STOP:
            mediaKey = MEDIA_STOP;
            break;
    }
    
    if (mediaKey != 0) {
        _bleKeyboard->sendMediaKey(mediaKey);
        DEBUG_PRINTF("Executed media key: 0x%04X\n", mediaKey);
    }
}

void ActionExecutor::_executeMouse(const MouseConfig& config) {
    switch (config.action) {
        case MOUSE_ACTION_CLICK:
            _bleKeyboard->sendMouseClick(MOUSE_LEFT);
            break;
            
        case MOUSE_ACTION_RIGHT_CLICK:
            _bleKeyboard->sendMouseClick(MOUSE_RIGHT);
            break;
            
        case MOUSE_ACTION_MIDDLE_CLICK:
            _bleKeyboard->sendMouseClick(MOUSE_MIDDLE);
            break;
            
        case MOUSE_ACTION_SCROLL_UP:
            _bleKeyboard->sendMouseScroll(config.value);
            break;
            
        case MOUSE_ACTION_SCROLL_DOWN:
            _bleKeyboard->sendMouseScroll(-config.value);
            break;
    }
    
    DEBUG_PRINTF("Executed mouse action: %d\n", config.action);
}

void ActionExecutor::_executeMacro(const MacroConfig& config) {
    if (config.stepCount == 0 || config.stepCount > MAX_MACRO_STEPS) return;
    
    for (uint8_t i = 0; i < config.stepCount; i++) {
        const MacroStepConfig& step = config.steps[i];
        
        switch (step.stepType) {
            case 1: // delay
                if (step.delayMs > 0 && step.delayMs <= 5000) {
                    delay(step.delayMs);
                }
                break;
                
            case 2: // keyPress
                _bleKeyboard->sendKeyPress(step.key, step.modifiers);
                delay(10);
                break;
                
            case 3: // text
                _bleKeyboard->sendText(step.text);
                break;
                
            case 4: { // media
                MediaConfig mc;
                mc.function = static_cast<MediaFunction>(step.mediaFunction);
                _executeMedia(mc);
                break;
            }
                
            default:
                break;
        }
        
        if (!_bleKeyboard->isHidReady()) break;
    }
    
    DEBUG_PRINTF("Executed macro with %d steps\n", config.stepCount);
}
