#include "profile_storage.h"

namespace {
template <size_t N>
void copySafeString(char (&dest)[N], const char* src) {
    memset(dest, 0, N);
    if (src) {
        strlcpy(dest, src, N);
    }
    dest[N - 1] = '\0';
}

template <size_t N>
void copyBoundedBuffer(const char* src, char (&dest)[N]) {
    memcpy(dest, src, N);
    dest[N - 1] = '\0';
}

bool isSupportedActionType(uint8_t rawType) {
    switch (rawType) {
        case ACTION_NONE:
        case ACTION_HOTKEY:
        case ACTION_MACRO:
        case ACTION_TEXT:
        case ACTION_MEDIA:
        case ACTION_MOUSE:
        case ACTION_PROFILE:
            return true;
        default:
            return false;
    }
}

void resetAction(Action& action) {
    memset(&action, 0, sizeof(Action));
    action.type = ACTION_NONE;
}

void resetProfile(Profile& profile) {
    memset(&profile, 0, sizeof(Profile));
    profile.version = 1;
    copySafeString(profile.name, "Unnamed");
    for (uint8_t i = 0; i < 2; i++) {
        profile.encoders[i].acceleration = true;
        profile.encoders[i].stepsPerDetent = 4;
    }
}
}

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
    DynamicJsonDocument doc(8192);
    
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
    
    resetProfile(profile);

    DynamicJsonDocument doc(8192);
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
        DeserializationError error = deserializeJson(doc, file);
        if (error) {
            strlcpy(name, "Unknown", 32);
        } else {
            strlcpy(name, doc["name"] | "Unknown", 32);
        }
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
    char safeName[sizeof(profile.name)];
    copyBoundedBuffer(profile.name, safeName);
    doc["name"] = safeName;
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
    resetProfile(profile);
    profile.id = doc["id"] | 0;
    copySafeString(profile.name, doc["name"] | "Unnamed");
    profile.version = doc["version"] | 1;
    
    // Deserialize keys (ArduinoJson 7: use as<JsonArrayConst> for const doc)
    JsonArrayConst keysArray = doc["keys"].as<JsonArrayConst>();
    for (uint8_t i = 0; i < MATRIX_KEYS && i < keysArray.size(); i++) {
        JsonObjectConst keyObj = keysArray[i].as<JsonObjectConst>();
        _deserializeAction(keyObj, profile.keys[i].action);
    }
    
    for (size_t i = keysArray.size(); i < MATRIX_KEYS; i++) {
        resetAction(profile.keys[i].action);
    }

    // Deserialize encoders
    JsonArrayConst encodersArray = doc["encoders"].as<JsonArrayConst>();
    for (uint8_t i = 0; i < 2 && i < encodersArray.size(); i++) {
        JsonObjectConst encObj = encodersArray[i].as<JsonObjectConst>();
        
        _deserializeAction(encObj["cwAction"].as<JsonObjectConst>(), profile.encoders[i].cwAction);
        _deserializeAction(encObj["ccwAction"].as<JsonObjectConst>(), profile.encoders[i].ccwAction);
        _deserializeAction(encObj["pressAction"].as<JsonObjectConst>(), profile.encoders[i].pressAction);
        
        profile.encoders[i].acceleration = encObj["acceleration"] | true;
        profile.encoders[i].stepsPerDetent = encObj["stepsPerDetent"] | 4;
    }
    
    return true;
}

void ProfileStorage::_serializeAction(const Action& action, JsonObject obj) {
    const ActionType type = isSupportedActionType(static_cast<uint8_t>(action.type)) ? action.type : ACTION_NONE;
    obj["type"] = static_cast<int>(type);

    switch (type) {
        case ACTION_HOTKEY:
            obj["modifiers"] = action.config.hotkey.modifiers;
            obj["key"] = action.config.hotkey.key;
            break;
            
        case ACTION_TEXT:
        {
            char safeText[sizeof(action.config.text.text)];
            copyBoundedBuffer(action.config.text.text, safeText);
            obj["text"] = safeText;
            break;
        }
            
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
            
        case ACTION_MACRO: {
            JsonArray stepsArr = obj.createNestedArray("macroSteps");
            for (uint8_t i = 0; i < action.config.macro.stepCount && i < MAX_MACRO_STEPS; i++) {
                JsonObject step = stepsArr.createNestedObject();
                step["stepType"] = action.config.macro.steps[i].stepType;
                step["delayMs"] = action.config.macro.steps[i].delayMs;
                step["key"] = action.config.macro.steps[i].key;
                step["modifiers"] = action.config.macro.steps[i].modifiers;
                char safeStepText[sizeof(action.config.macro.steps[i].text)];
                copyBoundedBuffer(action.config.macro.steps[i].text, safeStepText);
                if (safeStepText[0] != '\0') {
                    step["text"] = safeStepText;
                }
                step["mediaFunction"] = action.config.macro.steps[i].mediaFunction;
            }
            break;
        }
            
        default:
            break;
    }
}

void ProfileStorage::_deserializeAction(JsonObjectConst obj, Action& action) {
    resetAction(action);
    if (obj.isNull()) {
        return;
    }

    uint8_t rawType = obj["type"] | ACTION_NONE;
    if (!isSupportedActionType(rawType)) {
        return;
    }

    action.type = static_cast<ActionType>(rawType);

    switch (action.type) {
        case ACTION_HOTKEY:
            action.config.hotkey.modifiers = obj["modifiers"] | 0;
            action.config.hotkey.key = obj["key"] | 0;
            break;
            
        case ACTION_TEXT:
            copySafeString(action.config.text.text, obj["text"] | "");
            break;
            
        case ACTION_MEDIA:
        {
            uint8_t mediaFunction = obj["function"] | 0;
            if (mediaFunction > MEDIA_FUNC_STOP) {
                resetAction(action);
                return;
            }
            action.config.media.function = static_cast<MediaFunction>(mediaFunction);
            break;
        }
            
        case ACTION_MOUSE:
        {
            uint8_t mouseAction = obj["action"] | 0;
            if (mouseAction > MOUSE_ACTION_SCROLL_DOWN) {
                resetAction(action);
                return;
            }
            action.config.mouse.action = static_cast<MouseAction>(mouseAction);
            action.config.mouse.value = obj["value"] | 0;
            break;
        }
            
        case ACTION_PROFILE:
        {
            uint8_t profileId = obj["profileId"] | 0;
            if (profileId >= MAX_PROFILES) {
                resetAction(action);
                return;
            }
            action.config.profile.profileId = profileId;
            break;
        }
            
        case ACTION_MACRO: {
            memset(&action.config.macro, 0, sizeof(MacroConfig));
            JsonArrayConst stepsArr = obj["macroSteps"].as<JsonArrayConst>();
            uint8_t count = 0;
            for (JsonObjectConst stepObj : stepsArr) {
                if (count >= MAX_MACRO_STEPS) break;
                action.config.macro.steps[count].stepType = stepObj["stepType"] | 0;
                action.config.macro.steps[count].delayMs = stepObj["delayMs"] | 0;
                action.config.macro.steps[count].key = stepObj["key"] | 0;
                action.config.macro.steps[count].modifiers = stepObj["modifiers"] | 0;
                copySafeString(action.config.macro.steps[count].text, stepObj["text"] | "");
                action.config.macro.steps[count].mediaFunction = stepObj["mediaFunction"] | 0;
                count++;
            }
            action.config.macro.stepCount = count;
            break;
        }
            
        default:
            break;
    }
}

bool ProfileStorage::deserializeProfileFromObject(JsonObjectConst obj, Profile& profile) {
    if (!_initialized) return false;
    resetProfile(profile);

    profile.id = obj["id"] | 0;
    copySafeString(profile.name, obj["name"] | "Unnamed");
    profile.version = obj["version"] | 1;
    
    JsonArrayConst keysArray = obj["keys"].as<JsonArrayConst>();
    for (uint8_t i = 0; i < MATRIX_KEYS && i < keysArray.size(); i++) {
        JsonObjectConst keyObj = keysArray[i].as<JsonObjectConst>();
        _deserializeAction(keyObj, profile.keys[i].action);
    }
    for (size_t i = keysArray.size(); i < MATRIX_KEYS; i++) {
        profile.keys[i].action.type = ACTION_NONE;
    }
    
    for (uint8_t i = 0; i < 2; i++) {
        profile.encoders[i].cwAction.type = ACTION_NONE;
        profile.encoders[i].ccwAction.type = ACTION_NONE;
        profile.encoders[i].pressAction.type = ACTION_NONE;
        profile.encoders[i].acceleration = true;
        profile.encoders[i].stepsPerDetent = 4;
    }
    
    JsonArrayConst encodersArray = obj["encoders"].as<JsonArrayConst>();
    for (uint8_t i = 0; i < 2 && i < encodersArray.size(); i++) {
        JsonObjectConst encObj = encodersArray[i].as<JsonObjectConst>();
        
        if (!encObj["cwAction"].isNull()) {
            _deserializeAction(encObj["cwAction"].as<JsonObjectConst>(), profile.encoders[i].cwAction);
        }
        if (!encObj["ccwAction"].isNull()) {
            _deserializeAction(encObj["ccwAction"].as<JsonObjectConst>(), profile.encoders[i].ccwAction);
        }
        if (!encObj["pressAction"].isNull()) {
            _deserializeAction(encObj["pressAction"].as<JsonObjectConst>(), profile.encoders[i].pressAction);
        }
        
        profile.encoders[i].acceleration = encObj["acceleration"] | true;
        profile.encoders[i].stepsPerDetent = encObj["stepsPerDetent"] | 4;
    }
    
    return true;
}
