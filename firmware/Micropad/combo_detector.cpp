#include "input/combo_detector.h"

ComboDetector::ComboDetector() {
    _comboCount = 0;
    _triggeredIndex = -1;
}

void ComboDetector::addCombo(uint8_t key1, uint8_t key2, uint32_t holdMs) {
    if (_comboCount >= MAX_COMBOS) {
        DEBUG_PRINTLN("WARNING: Max combos reached");
        return;
    }
    
    _combos[_comboCount].key1 = key1;
    _combos[_comboCount].key2 = key2;
    _combos[_comboCount].holdMs = holdMs;
    _combos[_comboCount].detected = false;
    _combos[_comboCount].startTime = 0;
    
    _comboCount++;
    
    DEBUG_PRINTF("Added combo: K%d + K%d (hold %dms)\n", key1 + 1, key2 + 1, holdMs);
}

void ComboDetector::update(bool keyStates[]) {
    _triggeredIndex = -1;
    
    for (uint8_t i = 0; i < _comboCount; i++) {
        KeyCombo& combo = _combos[i];
        bool bothPressed = _areBothKeysPressed(keyStates, combo.key1, combo.key2);
        
        if (bothPressed) {
            if (combo.startTime == 0) {
                // Just started pressing both keys
                combo.startTime = millis();
                combo.detected = false;
            } else {
                // Both keys still pressed, check duration
                uint32_t duration = millis() - combo.startTime;
                
                if (!combo.detected && duration >= combo.holdMs) {
                    // Combo triggered!
                    combo.detected = true;
                    _triggeredIndex = i;
                    
                    DEBUG_PRINTF("Combo triggered: K%d + K%d\n", combo.key1 + 1, combo.key2 + 1);
                }
            }
        } else {
            // Keys released, reset
            combo.startTime = 0;
            combo.detected = false;
        }
    }
}

bool ComboDetector::comboTriggered(uint8_t key1, uint8_t key2) {
    for (uint8_t i = 0; i < _comboCount; i++) {
        if ((_combos[i].key1 == key1 && _combos[i].key2 == key2) ||
            (_combos[i].key1 == key2 && _combos[i].key2 == key1)) {
            
            bool triggered = _combos[i].detected;
            if (triggered) {
                _combos[i].detected = false;  // Clear flag after checking
            }
            return triggered;
        }
    }
    return false;
}

void ComboDetector::clearCombos() {
    _comboCount = 0;
    _triggeredIndex = -1;
}

int8_t ComboDetector::getTriggeredComboIndex() {
    return _triggeredIndex;
}

bool ComboDetector::_areBothKeysPressed(bool keyStates[], uint8_t key1, uint8_t key2) {
    return keyStates[key1] && keyStates[key2];
}
