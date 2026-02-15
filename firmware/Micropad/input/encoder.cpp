#include "encoder.h"

RotaryEncoder::RotaryEncoder() {
    _position = 0;
    _lastPosition = 0;
    _lastEncoded = 0;
    _lastTurnTime = 0;
    _acceleration = 1.0;
    _accelerationEnabled = true;
    _stepsPerDetent = ENCODER_STEPS_PER_DETENT;
    
    _swCurrentState = false;
    _swPreviousState = false;
    _swDebouncedState = false;
    _swLastDebounceTime = 0;
}

void RotaryEncoder::init(uint8_t pinA, uint8_t pinB, uint8_t pinSW) {
    _pinA = pinA;
    _pinB = pinB;
    _pinSW = pinSW;
    
    // Set up encoder pins with pullups
    pinMode(_pinA, INPUT_PULLUP);
    pinMode(_pinB, INPUT_PULLUP);
    pinMode(_pinSW, INPUT_PULLUP);
    
    // Read initial state
    uint8_t a = digitalRead(_pinA);
    uint8_t b = digitalRead(_pinB);
    _lastEncoded = (a << 1) | b;
    
    DEBUG_PRINTF("Encoder initialized on pins A=%d, B=%d, SW=%d\n", pinA, pinB, pinSW);
}

void RotaryEncoder::update() {
    _updateRotation();
    _updateSwitch();
}

int8_t RotaryEncoder::getDelta() {
    int8_t delta = _position - _lastPosition;
    _lastPosition = _position;
    return delta;
}

float RotaryEncoder::getAcceleration() {
    return _acceleration;
}

bool RotaryEncoder::isSWPressed() {
    return _swCurrentState;
}

bool RotaryEncoder::isSWJustPressed() {
    return _swCurrentState && !_swPreviousState;
}

bool RotaryEncoder::isSWJustReleased() {
    return !_swCurrentState && _swPreviousState;
}

void RotaryEncoder::setStepsPerDetent(uint8_t steps) {
    _stepsPerDetent = steps;
}

void RotaryEncoder::setAccelerationEnabled(bool enabled) {
    _accelerationEnabled = enabled;
}

void RotaryEncoder::_updateRotation() {
    // Read current state
    uint8_t a = digitalRead(_pinA);
    uint8_t b = digitalRead(_pinB);
    uint8_t encoded = (a << 1) | b;
    
    // Check if state changed
    if (encoded != _lastEncoded) {
        // Gray code state machine for direction detection
        uint8_t sum = (_lastEncoded << 2) | encoded;
        
        // Clockwise: 0b0010, 0b1011, 0b1101, 0b0100
        // Counter-clockwise: 0b0001, 0b0111, 0b1110, 0b1000
        if (sum == 0b0010 || sum == 0b1011 || sum == 0b1101 || sum == 0b0100) {
            _position++;
        } else if (sum == 0b0001 || sum == 0b0111 || sum == 0b1110 || sum == 0b1000) {
            _position--;
        }
        
        _lastEncoded = encoded;
        
        // Calculate acceleration
        if (_accelerationEnabled) {
            _calculateAcceleration();
        }
    }
}

void RotaryEncoder::_updateSwitch() {
    uint32_t currentTime = millis();
    bool rawState = !digitalRead(_pinSW);  // Inverted due to pullup
    
    // Debounce logic
    if (rawState != _swDebouncedState) {
        _swLastDebounceTime = currentTime;
        _swDebouncedState = rawState;
    }
    
    if ((currentTime - _swLastDebounceTime) >= DEBOUNCE_MS) {
        _swPreviousState = _swCurrentState;
        _swCurrentState = _swDebouncedState;
    }
}

void RotaryEncoder::_calculateAcceleration() {
    uint32_t currentTime = millis();
    uint32_t timeSinceLastTurn = currentTime - _lastTurnTime;
    
    if (timeSinceLastTurn < ENCODER_ACCEL_THRESHOLD_MS) {
        // Faster turns = higher acceleration (up to 5x)
        _acceleration = min(5.0f, _acceleration + 0.5f);
    } else {
        // Slow down acceleration back to 1.0
        _acceleration = max(1.0f, _acceleration - 0.2f);
    }
    
    _lastTurnTime = currentTime;
}
