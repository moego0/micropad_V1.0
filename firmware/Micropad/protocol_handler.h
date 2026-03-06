#ifndef PROTOCOL_HANDLER_H
#define PROTOCOL_HANDLER_H

#include <Arduino.h>
#include <ArduinoJson.h>
#include "config.h"

// Forward declarations
class ProfileManager;
class BLEConfigService;

class ProtocolHandler {
public:
    ProtocolHandler();
    
    void init(ProfileManager* profileManager);
    void setBLEService(BLEConfigService* bleService);
    void setBLEKeyboard(class BLEKeyboard* bleKeyboard);
    
    // Handle incoming messages
    void handleMessage(const String& json);
    
    // Process deferred (heavy) messages from main loop to avoid blocking BLE callback
    void processDeferred();
    
    // Send responses
    void sendResponse(uint32_t requestId, bool success, const String& error = "");
    void sendResponse(uint32_t requestId, const JsonDocument& payload);
    
    // Send events
    void sendEvent(const String& eventName, const JsonDocument& payload);
    
private:
    ProfileManager* _profileManager;
    BLEConfigService* _bleService;
    class BLEKeyboard* _bleKeyboard;
    
    // Command handlers
    void handleGetDeviceInfo(uint32_t requestId);
    void handleGetCaps(uint32_t requestId);
    void handleListProfiles(uint32_t requestId);
    void handleGetProfile(uint32_t requestId, uint8_t profileId);
    void handleSetProfile(uint32_t requestId, const JsonDocument& doc);
    void handleSetActiveProfile(uint32_t requestId, uint8_t profileId);
    void handleGetActiveProfile(uint32_t requestId);
    void handleDeleteProfile(uint32_t requestId, uint8_t profileId);
    void handleGetStats(uint32_t requestId);
    void handleFactoryReset(uint32_t requestId);
    void handleReboot(uint32_t requestId);
    void handleGetConnectionStatus(uint32_t requestId);
    
    // Defer setProfile to main loop so BLE callback returns quickly (avoids disconnect)
    String _deferredMessage;
    bool _processingDeferred;
    
    // Helper
    String createMessage(const String& type, uint32_t id, const JsonDocument& payload);
};

#endif // PROTOCOL_HANDLER_H
