#ifndef PROFILE_STORAGE_H
#define PROFILE_STORAGE_H

#include <Arduino.h>
#include <LittleFS.h>
#include <ArduinoJson.h>
#include "config.h"
#include "profile.h"

class ProfileStorage {
public:
    ProfileStorage();
    
    // Initialize storage
    bool init();
    
    // Profile operations
    bool saveProfile(const Profile& profile);
    bool loadProfile(uint8_t id, Profile& profile);
    bool deleteProfile(uint8_t id);
    bool profileExists(uint8_t id);
    
    // List profiles
    uint8_t getProfileCount();
    bool getProfileInfo(uint8_t id, char* name, size_t* size);
    
    // Format storage
    bool format();
    
    // Storage info
    size_t getTotalSpace();
    size_t getUsedSpace();
    size_t getFreeSpace();
    
private:
    bool _initialized;
    
    String _getProfilePath(uint8_t id);
    bool _serializeProfile(const Profile& profile, JsonDocument& doc);
    bool _deserializeProfile(const JsonDocument& doc, Profile& profile);
    
    void _serializeAction(const Action& action, JsonObject obj);
    void _deserializeAction(JsonObjectConst obj, Action& action);
};

#endif // PROFILE_STORAGE_H
