#ifndef ENCODER_H
#define ENCODER_H

#include <Arduino.h>
#include "config.h"

class RotaryEncoder {
public:
    RotaryEncoder();
    void init(uint8_t pinA, uint8_t pinB, uint8_t pinSW);
    void update();
    
    // Rotation
    int8_t getDelta();  // Returns rotation steps since last call
    float getAcceleration();  // Returns acceleration multiplier (1.0 - 5.0)
    
    // Switch
    bool isSWPressed();
    bool isSWJustPressed();
    bool isSWJustReleased();
    
    // Configuration
    void setStepsPerDetent(uint8_t steps);
    void setAccelerationEnabled(bool enabled);
    
private:
    // Pins
    uint8_t _pinA;
    uint8_t _pinB;
    uint8_t _pinSW;
    
    // Rotation state
    int8_t _position;
    int8_t _lastPosition;
    uint8_t _lastEncoded;
    uint32_t _lastTurnTime;
    float _acceleration;
    bool _accelerationEnabled;
    uint8_t _stepsPerDetent;
    
    // Switch state
    bool _swCurrentState;
    bool _swPreviousState;
    bool _swDebouncedState;
    uint32_t _swLastDebounceTime;
    
    // Helper methods
    void _updateRotation();
    void _updateSwitch();
    void _calculateAcceleration();
};

#endif // ENCODER_H
