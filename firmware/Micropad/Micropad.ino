/*
 * Micropad - Professional Wireless Macropad
 * Version: 1.0.0
 * 
 * Hardware:
 * - Wemos D1 Mini ESP32
 * - 12 mechanical switches (3x4 matrix)
 * - 2 rotary encoders with switches
 * 
 * Features:
 * - BLE HID (keyboard, mouse, media)
 * - Profile system (8 slots)
 * - LittleFS storage
 * - BLE config service
 * - WiFi + WebSocket (optional)
 * 
 * Pin Configuration:
 * - Matrix Rows: GPIO 16, 17, 18
 * - Matrix Cols: GPIO 21, 22, 23, 19
 * - Encoder 1: A=32, B=33, SW=27
 * - Encoder 2: A=25, B=26, SW=13
 */

#include <Arduino.h>
#include <Preferences.h>
#include "config.h"
#include "matrix.h"
#include "encoder.h"
#include "combo_detector.h"
#include "ble_hid.h"
#include "ble_config.h"
#include "protocol_handler.h"
#include "wifi_manager.h"
#include "websocket_server.h"
#include "action_executor.h"
#include "profile.h"
#include "profile_manager.h"

// ============================================
// Global Objects
// ============================================
KeyMatrix matrix;
RotaryEncoder encoder1;
RotaryEncoder encoder2;
BLEKeyboard bleKeyboard;
BLEConfigService bleConfig;
ProtocolHandler protocolHandler;
WiFiManager wifiManager;
WebSocketServer wsServer;
ActionExecutor actionExecutor;
ProfileManager profileManager;
ComboDetector comboDetector;
Preferences preferences;

// ============================================
// Setup Function
// ============================================
void setup() {
    Serial.begin(115200);
    delay(500);

    matrix.init();
    encoder1.init(ENC1_PIN_A, ENC1_PIN_B, ENC1_PIN_SW);
    encoder2.init(ENC2_PIN_A, ENC2_PIN_B, ENC2_PIN_SW);

    if (!profileManager.init()) {
        // Continue with default profile
    }

    comboDetector.addCombo(0, 3, 800);   // K1 + K4 = profile 1
    comboDetector.addCombo(0, 11, 800);  // K1 + K12 = profile 0

    setCpuFrequencyMhz(80);
    bleKeyboard.begin(BLE_DEVICE_NAME, BLE_MANUFACTURER);
    setCpuFrequencyMhz(160);

    protocolHandler.init(&profileManager);
    bleConfig.begin(&protocolHandler);
    protocolHandler.setBLEService(&bleConfig);

    // Order required: HID + Config must be registered before advertising (so GATT has config service 4fafc201-...)
    bleKeyboard.startAdvertising();

    preferences.begin(PREFS_NAMESPACE, true);
    bool wifiEnabled = preferences.getBool("wifiEnabled", false);
    if (wifiEnabled) {
        String ssid = preferences.getString("wifiSSID", "");
        String pass = preferences.getString("wifiPass", "");
        if (ssid.length() > 0 && wifiManager.connectSTA(ssid.c_str(), pass.c_str(), 10000)) {
            wifiManager.startMDNS(MDNS_HOSTNAME);
            wsServer.begin(WEBSOCKET_PORT, &protocolHandler);
        }
    }
    preferences.end();

    actionExecutor.init(&bleKeyboard);

    Serial.println("Micropad ready");
}

// ============================================
// Main Loop
// ============================================
void loop() {
    // Update BLE connection status
    bleKeyboard.update();
    // Restart BLE advertising if not connected (throttled to every 30s in ble_hid)
    bleKeyboard.restartAdvertisingIfNeeded();
    
    // Scan matrix for key presses
    matrix.scan();
    
    // Update encoders
    encoder1.update();
    encoder2.update();
    
    // Update communication
    bleConfig.update();
    wifiManager.update();
    
    // Check for key combos
    processCombos();
    
    // Process key events
    processKeys();
    
    // Process encoder events
    processEncoders();
    
    // Small delay to prevent overwhelming the system
    delay(1);
}

// ============================================
// Combo Processing
// ============================================
void processCombos() {
    // Get current key states
    bool keyStates[MATRIX_KEYS];
    for (uint8_t i = 0; i < MATRIX_KEYS; i++) {
        keyStates[i] = matrix.isPressed(i);
    }
    
    // Update combo detector
    comboDetector.update(keyStates);
    
    // Check for triggered combos
    int8_t comboIndex = comboDetector.getTriggeredComboIndex();
    
    if (comboIndex >= 0) {
        // Combo triggered! Switch profile based on combo
        switch (comboIndex) {
            case 0:  // K1 + K4 = Profile 1
                if (profileManager.profileExists(1)) {
                    profileManager.setActiveProfile(1);
                    DEBUG_PRINTLN("Switched to Profile 1");
                }
                break;
                
            case 1:  // K1 + K12 = Profile 0
                if (profileManager.profileExists(0)) {
                    profileManager.setActiveProfile(0);
                    DEBUG_PRINTLN("Switched to Profile 0");
                }
                break;
        }
    }
}

// ============================================
// Key Processing
// ============================================
void processKeys() {
    Profile* currentProfile = profileManager.getCurrentProfile();
    
    // Check each key for press events
    for (uint8_t i = 0; i < MATRIX_KEYS; i++) {
        if (matrix.justPressed(i)) {
            DEBUG_PRINTF("Key %d pressed\n", i);
            
            // Get action for this key
            const Action& action = currentProfile->keys[i].action;
            
            // Check if this is a profile switch action
            if (action.type == ACTION_PROFILE) {
                uint8_t targetProfile = action.config.profile.profileId;
                if (profileManager.profileExists(targetProfile)) {
                    profileManager.setActiveProfile(targetProfile);
                    DEBUG_PRINTF("Switched to profile %d\n", targetProfile);
                }
            } else if (action.type != ACTION_NONE) {
                // Execute normal action
                actionExecutor.execute(action);
            }
        }
        
        if (matrix.justReleased(i)) {
            DEBUG_PRINTF("Key %d released\n", i);
        }
    }
}

// ============================================
// Encoder Processing
// ============================================
void processEncoders() {
    Profile* currentProfile = profileManager.getCurrentProfile();
    
    // Encoder 1
    int8_t delta1 = encoder1.getDelta();
    if (delta1 != 0) {
        DEBUG_PRINTF("Encoder 1 turned: %d\n", delta1);
        
        if (delta1 > 0) {
            // Clockwise
            actionExecutor.execute(currentProfile->encoders[0].cwAction);
        } else {
            // Counter-clockwise
            actionExecutor.execute(currentProfile->encoders[0].ccwAction);
        }
    }
    
    if (encoder1.isSWJustPressed()) {
        DEBUG_PRINTLN("Encoder 1 pressed");
        actionExecutor.execute(currentProfile->encoders[0].pressAction);
    }
    
    // Encoder 2
    int8_t delta2 = encoder2.getDelta();
    if (delta2 != 0) {
        DEBUG_PRINTF("Encoder 2 turned: %d\n", delta2);
        
        if (delta2 > 0) {
            // Clockwise
            actionExecutor.execute(currentProfile->encoders[1].cwAction);
        } else {
            // Counter-clockwise
            actionExecutor.execute(currentProfile->encoders[1].ccwAction);
        }
    }
    
    if (encoder2.isSWJustPressed()) {
        DEBUG_PRINTLN("Encoder 2 pressed");
        actionExecutor.execute(currentProfile->encoders[1].pressAction);
    }
}
