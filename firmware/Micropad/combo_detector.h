#ifndef COMBO_DETECTOR_H
#define COMBO_DETECTOR_H

#include <Arduino.h>
#include "config.h"

struct KeyCombo {
    uint8_t key1;
    uint8_t key2;
    uint32_t holdMs;
    bool detected;
    uint32_t startTime;
};

class ComboDetector {
public:
    ComboDetector();
    
    // Add combo to detect
    void addCombo(uint8_t key1, uint8_t key2, uint32_t holdMs);
    
    // Update with current key states
    void update(bool keyStates[]);
    
    // Check if combo was triggered
    bool comboTriggered(uint8_t key1, uint8_t key2);
    
    // Clear all combos
    void clearCombos();
    
    // Get triggered combo index
    int8_t getTriggeredComboIndex();
    
private:
    static const uint8_t MAX_COMBOS = 8;
    KeyCombo _combos[MAX_COMBOS];
    uint8_t _comboCount;
    int8_t _triggeredIndex;
    
    bool _areBothKeysPressed(bool keyStates[], uint8_t key1, uint8_t key2);
};

#endif // COMBO_DETECTOR_H
