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
#include "input/matrix.h"
#include "input/encoder.h"
#include "input/combo_detector.h"
#include "comms/ble_hid.h"
#include "comms/ble_config.h"
#include "comms/protocol_handler.h"
#include "comms/wifi_manager.h"
#include "comms/websocket_server.h"
#include "actions/action_executor.h"
#include "profiles/profile.h"
#include "profiles/profile_manager.h"

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
    // Initialize serial for debugging
    Serial.begin(115200);
    delay(1000);  // Wait for serial to initialize
    
    DEBUG_PRINTLN("========================================");
    DEBUG_PRINTLN("Micropad Firmware " FIRMWARE_VERSION);
    DEBUG_PRINTLN("========================================");
    
    // 1. Initialize input hardware
    DEBUG_PRINTLN("Initializing input hardware...");
    matrix.init();
    encoder1.init(ENC1_PIN_A, ENC1_PIN_B, ENC1_PIN_SW);
    encoder2.init(ENC2_PIN_A, ENC2_PIN_B, ENC2_PIN_SW);
    
    // 2. Initialize profile manager (loads from storage)
    DEBUG_PRINTLN("Initializing profile manager...");
    if (!profileManager.init()) {
        DEBUG_PRINTLN("ERROR: Failed to initialize profile manager!");
        // Continue anyway with default profile
    }
    
    // 3. Setup key combos for profile switching
    DEBUG_PRINTLN("Setting up key combos...");
    comboDetector.addCombo(0, 3, 800);   // K1 + K4 = Switch to profile 1
    comboDetector.addCombo(0, 11, 800);  // K1 + K12 = Switch to profile 0
    
    // 4. Start BLE HID
    DEBUG_PRINTLN("Starting BLE HID...");
    bleKeyboard.begin(BLE_DEVICE_NAME, BLE_MANUFACTURER);
    
    // 5. Initialize protocol handler
    protocolHandler.init(&profileManager);
    
    // 6. Start BLE Config Service
    bleConfig.begin(&protocolHandler);
    protocolHandler.setBLEService(&bleConfig);
    
    // 7. Start WiFi (optional, based on preferences)
    preferences.begin(PREFS_NAMESPACE, true);  // Read-only
    bool wifiEnabled = preferences.getBool("wifiEnabled", false);
    
    if (wifiEnabled) {
        String ssid = preferences.getString("wifiSSID", "");
        String pass = preferences.getString("wifiPass", "");
        
        if (ssid.length() > 0) {
            DEBUG_PRINTLN("Starting WiFi...");
            if (wifiManager.connectSTA(ssid.c_str(), pass.c_str(), 10000)) {
                // Start mDNS
                wifiManager.startMDNS(MDNS_HOSTNAME);
                
                // Start WebSocket server
                wsServer.begin(WEBSOCKET_PORT, &protocolHandler);
            }
        }
    } else {
        DEBUG_PRINTLN("WiFi disabled (enable via preferences)");
    }
    preferences.end();
    
    // 8. Initialize action executor
    actionExecutor.init(&bleKeyboard);
    
    DEBUG_PRINTLN("========================================");
    DEBUG_PRINTF("Active Profile: %d - %s\n", 
                 profileManager.getActiveProfileId(), 
                 profileManager.getCurrentProfile()->name);
    DEBUG_PRINTLN("Micropad ready! Waiting for BLE connection...");
    DEBUG_PRINTLN("========================================");
}

// ============================================
// Main Loop
// ============================================
void loop() {
    // Update BLE connection status
    bleKeyboard.update();
    // Restart BLE advertising if not connected (helps Windows reconnect)
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
