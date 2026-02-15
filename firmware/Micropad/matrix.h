#ifndef MATRIX_H
#define MATRIX_H

#include <Arduino.h>
#include "config.h"

class KeyMatrix {
public:
    KeyMatrix();
    void init();
    void scan();
    
    // State query methods
    bool isPressed(uint8_t key);
    bool justPressed(uint8_t key);
    bool justReleased(uint8_t key);
    
    // Get key press duration
    uint32_t getPressedDuration(uint8_t key);
    
private:
    bool _currentState[MATRIX_KEYS];
    bool _previousState[MATRIX_KEYS];
    uint32_t _lastDebounceTime[MATRIX_KEYS];
    uint32_t _pressStartTime[MATRIX_KEYS];
    bool _debouncedState[MATRIX_KEYS];
    
    uint8_t _keyToIndex(uint8_t row, uint8_t col);
    void _updateDebounce(uint8_t key, bool rawState);
};

#endif // MATRIX_H
