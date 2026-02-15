#include "input/matrix.h"

KeyMatrix::KeyMatrix() {
    // Initialize all states to false
    for (uint8_t i = 0; i < MATRIX_KEYS; i++) {
        _currentState[i] = false;
        _previousState[i] = false;
        _debouncedState[i] = false;
        _lastDebounceTime[i] = 0;
        _pressStartTime[i] = 0;
    }
}

void KeyMatrix::init() {
    // Set up column pins as INPUT_PULLUP
    for (uint8_t col = 0; col < MATRIX_COLS; col++) {
        pinMode(COL_PINS[col], INPUT_PULLUP);
    }
    
    // Set up row pins as OUTPUT (initially HIGH)
    for (uint8_t row = 0; row < MATRIX_ROWS; row++) {
        pinMode(ROW_PINS[row], OUTPUT);
        digitalWrite(ROW_PINS[row], HIGH);
    }
    
    DEBUG_PRINTLN("Matrix initialized");
}

void KeyMatrix::scan() {
    // Scan each row
    for (uint8_t row = 0; row < MATRIX_ROWS; row++) {
        // Drive current row LOW
        digitalWrite(ROW_PINS[row], LOW);
        
        // Small delay for signal stabilization
        delayMicroseconds(5);
        
        // Read all columns
        for (uint8_t col = 0; col < MATRIX_COLS; col++) {
            uint8_t keyIndex = _keyToIndex(row, col);
            
            // Read column pin (LOW = pressed due to pullup)
            bool rawState = !digitalRead(COL_PINS[col]);
            
            // Update debounce logic
            _updateDebounce(keyIndex, rawState);
        }
        
        // Set row back to HIGH
        digitalWrite(ROW_PINS[row], HIGH);
    }
}

bool KeyMatrix::isPressed(uint8_t key) {
    if (key >= MATRIX_KEYS) return false;
    return _currentState[key];
}

bool KeyMatrix::justPressed(uint8_t key) {
    if (key >= MATRIX_KEYS) return false;
    return _currentState[key] && !_previousState[key];
}

bool KeyMatrix::justReleased(uint8_t key) {
    if (key >= MATRIX_KEYS) return false;
    return !_currentState[key] && _previousState[key];
}

uint32_t KeyMatrix::getPressedDuration(uint8_t key) {
    if (key >= MATRIX_KEYS) return 0;
    if (!_currentState[key]) return 0;
    return millis() - _pressStartTime[key];
}

uint8_t KeyMatrix::_keyToIndex(uint8_t row, uint8_t col) {
    return row * MATRIX_COLS + col;
}

void KeyMatrix::_updateDebounce(uint8_t key, bool rawState) {
    uint32_t currentTime = millis();
    
    // If state changed, reset debounce timer
    if (rawState != _debouncedState[key]) {
        _lastDebounceTime[key] = currentTime;
        _debouncedState[key] = rawState;
    }
    
    // If state has been stable for debounce period
    if ((currentTime - _lastDebounceTime[key]) >= DEBOUNCE_MS) {
        // Update previous state before changing current
        _previousState[key] = _currentState[key];
        
        // Update current state
        if (_debouncedState[key] != _currentState[key]) {
            _currentState[key] = _debouncedState[key];
            
            // Track press start time for duration calculation
            if (_currentState[key]) {
                _pressStartTime[key] = currentTime;
            }
        }
    }
}
