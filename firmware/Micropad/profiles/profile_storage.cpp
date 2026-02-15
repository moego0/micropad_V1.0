#include "profile_storage.h"

ProfileStorage::ProfileStorage() {
    _initialized = false;
}

bool ProfileStorage::init() {
    if (_initialized) {
        return true;
    }
    
    DEBUG_PRINTLN("Initializing LittleFS...");
    
    if (!LittleFS.begin(true)) {  // true = format if mount fails
        DEBUG_PRINTLN("ERROR: LittleFS mount failed!");
        return false;
    }
    
    _initialized = true;
    
    DEBUG_PRINTF("LittleFS initialized: %d KB total, %d KB used\n", 
                 getTotalSpace() / 1024, getUsedSpace() / 1024);
    
    // Create profiles directory if it doesn't exist
    if (!LittleFS.exists(PROFILES_PATH)) {
        LittleFS.mkdir(PROFILES_PATH);
        DEBUG_PRINTLN("Created profiles directory");
    }
    
    return true;
}

bool ProfileStorage::saveProfile(const Profile& profile) {
    if (!_initialized) {
        DEBUG_PRINTLN("ERROR: Storage not initialized");
        return false;
    }
    
    DEBUG_PRINTF("Saving profile %d: %s\n", profile.id, profile.name);
    
    // Create JSON document (allocate enough space)
    DynamicJsonDocument doc(4096);
    
    if (!_serializeProfile(profile, doc)) {
        DEBUG_PRINTLN("ERROR: Failed to serialize profile");
        return false;
    }
    
    // Write to temporary file first (atomic write)
    String tempPath = _getProfilePath(profile.id) + ".tmp";
    File file = LittleFS.open(tempPath, "w");
    
    if (!file) {
        DEBUG_PRINTLN("ERROR: Failed to open file for writing");
        return false;
    }
    
    size_t bytesWritten = serializeJson(doc, file);
    file.close();
    
    if (bytesWritten == 0) {
        DEBUG_PRINTLN("ERROR: Failed to write JSON");
        LittleFS.remove(tempPath);
        return false;
    }
    
    // Rename temp file to actual file (atomic operation)
    String actualPath = _getProfilePath(profile.id);
    LittleFS.remove(actualPath);  // Remove old file if exists
    LittleFS.rename(tempPath, actualPath);
    
    DEBUG_PRINTF("Profile saved successfully (%d bytes)\n", bytesWritten);
    return true;
}

bool ProfileStorage::loadProfile(uint8_t id, Profile& profile) {
    if (!_initialized) {
        DEBUG_PRINTLN("ERROR: Storage not initialized");
        return false;
    }
    
    String path = _getProfilePath(id);
    
    if (!LittleFS.exists(path)) {
        DEBUG_PRINTF("Profile %d does not exist\n", id);
        return false;
    }
    
    DEBUG_PRINTF("Loading profile %d...\n", id);
    
    File file = LittleFS.open(path, "r");
    if (!file) {
        DEBUG_PRINTLN("ERROR: Failed to open file for reading");
        return false;
    }
    
    DynamicJsonDocument doc(4096);
    DeserializationError error = deserializeJson(doc, file);
    file.close();
    
    if (error) {
        DEBUG_PRINTF("ERROR: JSON deserialization failed: %s\n", error.c_str());
        return false;
    }
    
    if (!_deserializeProfile(doc, profile)) {
        DEBUG_PRINTLN("ERROR: Failed to deserialize profile");
        return false;
    }
    
    DEBUG_PRINTF("Profile loaded: %s\n", profile.name);
    return true;
}

bool ProfileStorage::deleteProfile(uint8_t id) {
    if (!_initialized) return false;
    
    String path = _getProfilePath(id);
    if (LittleFS.exists(path)) {
        return LittleFS.remove(path);
    }
    return false;
}

bool ProfileStorage::profileExists(uint8_t id) {
    if (!_initialized) return false;
    return LittleFS.exists(_getProfilePath(id));
}

uint8_t ProfileStorage::getProfileCount() {
    if (!_initialized) return 0;
    
    uint8_t count = 0;
    for (uint8_t i = 0; i < MAX_PROFILES; i++) {
        if (profileExists(i)) {
            count++;
        }
    }
    return count;
}

bool ProfileStorage::getProfileInfo(uint8_t id, char* name, size_t* size) {
    if (!_initialized) return false;
    
    String path = _getProfilePath(id);
    if (!LittleFS.exists(path)) return false;
    
    File file = LittleFS.open(path, "r");
    if (!file) return false;
    
    if (size) {
        *size = file.size();
    }
    
    if (name) {
        // Quick read just to get the name
        DynamicJsonDocument doc(512);
        deserializeJson(doc, file);
        strcpy(name, doc["name"] | "Unknown");
    }
    
    file.close();
    return true;
}

bool ProfileStorage::format() {
    if (!_initialized) return false;
    
    DEBUG_PRINTLN("Formatting LittleFS...");
    LittleFS.end();
    LittleFS.begin(true);
    _initialized = true;
    
    return true;
}

size_t ProfileStorage::getTotalSpace() {
    if (!_initialized) return 0;
    return LittleFS.totalBytes();
}

size_t ProfileStorage::getUsedSpace() {
    if (!_initialized) return 0;
    return LittleFS.usedBytes();
}

size_t ProfileStorage::getFreeSpace() {
    return getTotalSpace() - getUsedSpace();
}

String ProfileStorage::_getProfilePath(uint8_t id) {
    char filename[64];
    snprintf(filename, sizeof(filename), "%s/profile_%d.json", PROFILES_PATH, id);
    return String(filename);
}

bool ProfileStorage::_serializeProfile(const Profile& profile, JsonDocument& doc) {
    doc["id"] = profile.id;
    doc["name"] = profile.name;
    doc["version"] = profile.version;
    
    // Serialize keys
    JsonArray keysArray = doc.createNestedArray("keys");
    for (uint8_t i = 0; i < MATRIX_KEYS; i++) {
        JsonObject keyObj = keysArray.createNestedObject();
        keyObj["index"] = i;
        _serializeAction(profile.keys[i].action, keyObj);
    }
    
    // Serialize encoders
    JsonArray encodersArray = doc.createNestedArray("encoders");
    for (uint8_t i = 0; i < 2; i++) {
        JsonObject encObj = encodersArray.createNestedObject();
        encObj["index"] = i;
        
        JsonObject cwObj = encObj.createNestedObject("cwAction");
        _serializeAction(profile.encoders[i].cwAction, cwObj);
        
        JsonObject ccwObj = encObj.createNestedObject("ccwAction");
        _serializeAction(profile.encoders[i].ccwAction, ccwObj);
        
        JsonObject pressObj = encObj.createNestedObject("pressAction");
        _serializeAction(profile.encoders[i].pressAction, pressObj);
        
        encObj["acceleration"] = profile.encoders[i].acceleration;
        encObj["stepsPerDetent"] = profile.encoders[i].stepsPerDetent;
    }
    
    return true;
}

bool ProfileStorage::_deserializeProfile(const JsonDocument& doc, Profile& profile) {
    profile.id = doc["id"] | 0;
    strlcpy(profile.name, doc["name"] | "Unnamed", sizeof(profile.name));
    profile.version = doc["version"] | 1;
    
    // Deserialize keys
    JsonArray keysArray = doc["keys"];
    for (uint8_t i = 0; i < MATRIX_KEYS && i < keysArray.size(); i++) {
        JsonObject keyObj = keysArray[i];
        _deserializeAction(keyObj, profile.keys[i].action);
    }
    
    // Deserialize encoders
    JsonArray encodersArray = doc["encoders"];
    for (uint8_t i = 0; i < 2 && i < encodersArray.size(); i++) {
        JsonObject encObj = encodersArray[i];
        
        _deserializeAction(encObj["cwAction"], profile.encoders[i].cwAction);
        _deserializeAction(encObj["ccwAction"], profile.encoders[i].ccwAction);
        _deserializeAction(encObj["pressAction"], profile.encoders[i].pressAction);
        
        profile.encoders[i].acceleration = encObj["acceleration"] | true;
        profile.encoders[i].stepsPerDetent = encObj["stepsPerDetent"] | 4;
    }
    
    return true;
}

void ProfileStorage::_serializeAction(const Action& action, JsonObject obj) {
    obj["type"] = static_cast<int>(action.type);
    
    switch (action.type) {
        case ACTION_HOTKEY:
            obj["modifiers"] = action.config.hotkey.modifiers;
            obj["key"] = action.config.hotkey.key;
            break;
            
        case ACTION_TEXT:
            obj["text"] = action.config.text.text;
            break;
            
        case ACTION_MEDIA:
            obj["function"] = static_cast<int>(action.config.media.function);
            break;
            
        case ACTION_MOUSE:
            obj["action"] = static_cast<int>(action.config.mouse.action);
            obj["value"] = action.config.mouse.value;
            break;
            
        case ACTION_PROFILE:
            obj["profileId"] = action.config.profile.profileId;
            break;
            
        default:
            break;
    }
}

void ProfileStorage::_deserializeAction(JsonObject obj, Action& action) {
    action.type = static_cast<ActionType>(obj["type"] | ACTION_NONE);
    
    switch (action.type) {
        case ACTION_HOTKEY:
            action.config.hotkey.modifiers = obj["modifiers"] | 0;
            action.config.hotkey.key = obj["key"] | 0;
            break;
            
        case ACTION_TEXT:
            strlcpy(action.config.text.text, obj["text"] | "", sizeof(action.config.text.text));
            break;
            
        case ACTION_MEDIA:
            action.config.media.function = static_cast<MediaFunction>(obj["function"] | 0);
            break;
            
        case ACTION_MOUSE:
            action.config.mouse.action = static_cast<MouseAction>(obj["action"] | 0);
            action.config.mouse.value = obj["value"] | 0;
            break;
            
        case ACTION_PROFILE:
            action.config.profile.profileId = obj["profileId"] | 0;
            break;
            
        default:
            break;
    }
}
