#include "protocol_handler.h"
#include "ble_config.h"
#include "../profiles/profile_manager.h"

ProtocolHandler::ProtocolHandler() {
    _profileManager = nullptr;
    _bleService = nullptr;
}

void ProtocolHandler::init(ProfileManager* profileManager) {
    _profileManager = profileManager;
    DEBUG_PRINTLN("Protocol Handler initialized");
}

void ProtocolHandler::setBLEService(BLEConfigService* bleService) {
    _bleService = bleService;
}

void ProtocolHandler::handleMessage(const String& json) {
    DEBUG_PRINTF("Protocol RX: %s\n", json.substring(0, 200).c_str());
    
    // Parse JSON
    DynamicJsonDocument doc(4096);
    DeserializationError error = deserializeJson(doc, json);
    
    if (error) {
        DEBUG_PRINTF("JSON parse error: %s\n", error.c_str());
        return;
    }
    
    // Extract envelope fields
    uint8_t version = doc["v"] | 1;
    String type = doc["type"] | "request";
    uint32_t id = doc["id"] | 0;
    
    if (type != "request") {
        DEBUG_PRINTLN("Ignoring non-request message");
        return;
    }
    
    // Get command from payload
    String cmd = doc["cmd"] | "";
    
    if (cmd == "") {
        // Try old format
        cmd = doc["payload"]["cmd"] | "";
    }
    
    DEBUG_PRINTF("Command: %s (id=%d)\n", cmd.c_str(), id);
    
    // Route to appropriate handler
    if (cmd == "getDeviceInfo") {
        handleGetDeviceInfo(id);
    }
    else if (cmd == "listProfiles") {
        handleListProfiles(id);
    }
    else if (cmd == "getProfile") {
        uint8_t profileId = doc["profileId"] | 0;
        handleGetProfile(id, profileId);
    }
    else if (cmd == "setProfile") {
        handleSetProfile(id, doc);
    }
    else if (cmd == "setActiveProfile") {
        uint8_t profileId = doc["profileId"] | 0;
        handleSetActiveProfile(id, profileId);
    }
    else if (cmd == "getStats") {
        handleGetStats(id);
    }
    else if (cmd == "factoryReset") {
        handleFactoryReset(id);
    }
    else if (cmd == "reboot") {
        handleReboot(id);
    }
    else {
        DEBUG_PRINTF("Unknown command: %s\n", cmd.c_str());
        sendResponse(id, false, "Unknown command");
    }
}

void ProtocolHandler::handleGetDeviceInfo(uint32_t requestId) {
    DynamicJsonDocument payload(512);
    
    payload["deviceId"] = String("ESP32-") + String((uint32_t)ESP.getEfuseMac(), HEX);
    payload["firmwareVersion"] = FIRMWARE_VERSION;
    payload["hardwareVersion"] = HARDWARE_VERSION;
    payload["batteryLevel"] = 100;  // No battery yet
    
    JsonArray caps = payload.createNestedArray("capabilities");
    caps.add("ble");
    caps.add("wifi");
    caps.add("macros");
    caps.add("profiles");
    
    payload["uptime"] = millis() / 1000;
    payload["freeHeap"] = ESP.getFreeHeap();
    
    sendResponse(requestId, payload);
}

void ProtocolHandler::handleListProfiles(uint32_t requestId) {
    DynamicJsonDocument payload(2048);
    JsonArray profiles = payload.createNestedArray("profiles");
    
    for (uint8_t i = 0; i < MAX_PROFILES; i++) {
        if (_profileManager->profileExists(i)) {
            JsonObject profile = profiles.createNestedObject();
            profile["id"] = i;
            
            char name[32];
            size_t size;
            if (_profileManager->_storage.getProfileInfo(i, name, &size)) {
                profile["name"] = name;
                profile["size"] = size;
            }
        }
    }
    
    sendResponse(requestId, payload);
}

void ProtocolHandler::handleGetProfile(uint32_t requestId, uint8_t profileId) {
    Profile profile;
    
    if (!_profileManager->_storage.loadProfile(profileId, profile)) {
        sendResponse(requestId, false, "Profile not found");
        return;
    }
    
    // Serialize profile to JSON
    DynamicJsonDocument payload(4096);
    
    payload["id"] = profile.id;
    payload["name"] = profile.name;
    payload["version"] = profile.version;
    
    // Keys
    JsonArray keys = payload.createNestedArray("keys");
    for (uint8_t i = 0; i < MATRIX_KEYS; i++) {
        JsonObject key = keys.createNestedObject();
        key["index"] = i;
        key["type"] = static_cast<int>(profile.keys[i].action.type);
        
        // Add config based on type
        switch (profile.keys[i].action.type) {
            case ACTION_HOTKEY:
                key["modifiers"] = profile.keys[i].action.config.hotkey.modifiers;
                key["key"] = profile.keys[i].action.config.hotkey.key;
                break;
            case ACTION_TEXT:
                key["text"] = profile.keys[i].action.config.text.text;
                break;
            case ACTION_MEDIA:
                key["function"] = static_cast<int>(profile.keys[i].action.config.media.function);
                break;
            case ACTION_MOUSE:
                key["action"] = static_cast<int>(profile.keys[i].action.config.mouse.action);
                key["value"] = profile.keys[i].action.config.mouse.value;
                break;
            case ACTION_PROFILE:
                key["profileId"] = profile.keys[i].action.config.profile.profileId;
                break;
            default:
                break;
        }
    }
    
    // Encoders (simplified)
    JsonArray encoders = payload.createNestedArray("encoders");
    for (uint8_t i = 0; i < 2; i++) {
        JsonObject enc = encoders.createNestedObject();
        enc["index"] = i;
        enc["acceleration"] = profile.encoders[i].acceleration;
        enc["stepsPerDetent"] = profile.encoders[i].stepsPerDetent;
    }
    
    sendResponse(requestId, payload);
}

void ProtocolHandler::handleSetProfile(uint32_t requestId, const JsonDocument& doc) {
    // This would deserialize and save a profile
    // For now, just acknowledge
    sendResponse(requestId, false, "Not implemented yet");
}

void ProtocolHandler::handleSetActiveProfile(uint32_t requestId, uint8_t profileId) {
    if (_profileManager->setActiveProfile(profileId)) {
        DynamicJsonDocument payload(128);
        payload["profileId"] = profileId;
        sendResponse(requestId, payload);
        
        // Send event
        DynamicJsonDocument eventPayload(128);
        eventPayload["profileId"] = profileId;
        sendEvent("profileChanged", eventPayload);
    } else {
        sendResponse(requestId, false, "Failed to switch profile");
    }
}

void ProtocolHandler::handleGetStats(uint32_t requestId) {
    DynamicJsonDocument payload(512);
    
    JsonArray keyPresses = payload.createNestedArray("keyPresses");
    for (uint8_t i = 0; i < MATRIX_KEYS; i++) {
        keyPresses.add(0);  // TODO: Track actual stats
    }
    
    JsonArray encoderTurns = payload.createNestedArray("encoderTurns");
    encoderTurns.add(0);
    encoderTurns.add(0);
    
    payload["uptime"] = millis() / 1000;
    
    sendResponse(requestId, payload);
}

void ProtocolHandler::handleFactoryReset(uint32_t requestId) {
    _profileManager->factoryReset();
    
    DynamicJsonDocument payload(64);
    payload["success"] = true;
    sendResponse(requestId, payload);
}

void ProtocolHandler::handleReboot(uint32_t requestId) {
    DynamicJsonDocument payload(64);
    payload["success"] = true;
    sendResponse(requestId, payload);
    
    delay(100);
    ESP.restart();
}

void ProtocolHandler::sendResponse(uint32_t requestId, bool success, const String& error) {
    DynamicJsonDocument payload(128);
    payload["success"] = success;
    if (error.length() > 0) {
        payload["error"] = error;
    }
    sendResponse(requestId, payload);
}

void ProtocolHandler::sendResponse(uint32_t requestId, const JsonDocument& payload) {
    String message = createMessage("response", requestId, payload);
    
    if (_bleService) {
        _bleService->sendEvent(message);
    }
}

void ProtocolHandler::sendEvent(const String& eventName, const JsonDocument& payload) {
    DynamicJsonDocument doc(2048);
    doc["v"] = 1;
    doc["type"] = "event";
    doc["event"] = eventName;
    doc["ts"] = millis() / 1000;
    doc["payload"] = payload;
    
    String message;
    serializeJson(doc, message);
    
    if (_bleService) {
        _bleService->sendEvent(message);
    }
}

String ProtocolHandler::createMessage(const String& type, uint32_t id, const JsonDocument& payload) {
    DynamicJsonDocument doc(4096);
    doc["v"] = 1;
    doc["type"] = type;
    doc["id"] = id;
    doc["ts"] = millis() / 1000;
    doc["payload"] = payload;
    
    String message;
    serializeJson(doc, message);
    
    return message;
}
