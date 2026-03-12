#include "profile_manager.h"
#include "default_profile.h"
#include "profile_templates.h"

ProfileManager::ProfileManager() {
    _activeProfileId = 0;
    _initialized = false;
}

bool ProfileManager::init() {
    if (_initialized) {
        return true;
    }
    
    DEBUG_PRINTLN("Initializing Profile Manager...");
    
    // Initialize storage
    if (!_storage.init()) {
        DEBUG_PRINTLN("ERROR: Failed to initialize storage");
        return false;
    }
    
    // Initialize preferences
    _prefs.begin(PREFS_NAMESPACE, false);
    
    // Load last active profile ID
    _loadActiveProfile();
    
    // Check if any profiles exist
    if (_storage.getProfileCount() == 0 || !_storage.profileExists(0)) {
        DEBUG_PRINTLN("Profiles missing, creating defaults...");
        initializeDefaultProfiles();
    }
    
    // Load the active profile
    if (!loadProfile(_activeProfileId)) {
        DEBUG_PRINTLN("Failed to load active profile, loading default...");
        if (!loadProfile(0)) {
            DEBUG_PRINTLN("Default profile missing or corrupt. Rebuilding defaults...");
            _storage.format();
            initializeDefaultProfiles();
            if (!loadProfile(0)) {
                DEBUG_PRINTLN("ERROR: Failed to rebuild default profile!");
                return false;
            }
        }
    }
    
    _initialized = true;
    DEBUG_PRINTF("Profile Manager initialized (active profile: %d)\n", _activeProfileId);
    Serial.println("PM: init done, returning");
    Serial.flush();
    return true;
}

bool ProfileManager::loadProfile(uint8_t id) {
    if (id >= MAX_PROFILES) {
        DEBUG_PRINTF("ERROR: Invalid profile ID: %d\n", id);
        return false;
    }

    if (!_storage.loadProfile(id, _currentProfile)) {
        return false;
    }

    _activeProfileId = id;
    
    DEBUG_PRINTF("Loaded profile %d: %s\n", id, _currentProfile.name);
    
    return true;
}

bool ProfileManager::saveProfile(uint8_t id, const Profile& profile) {
    if (id >= MAX_PROFILES) {
        DEBUG_PRINTF("ERROR: Invalid profile ID: %d\n", id);
        return false;
    }

    if (profile.id == id) {
        return _storage.saveProfile(profile);
    }

    _workProfile = profile;
    _workProfile.id = id;
    return _storage.saveProfile(_workProfile);
}

bool ProfileManager::saveProfileFromJson(JsonObjectConst obj) {
    if (!_storage.deserializeProfileFromObject(obj, _workProfile)) {
        return false;
    }
    if (_workProfile.id >= MAX_PROFILES) {
        return false;
    }
    return _storage.saveProfile(_workProfile);
}

bool ProfileManager::deleteProfile(uint8_t id) {
    if (id >= MAX_PROFILES) {
        return false;
    }
    
    // Don't delete if it's the only profile
    if (_storage.getProfileCount() <= 1) {
        DEBUG_PRINTLN("Cannot delete last profile");
        return false;
    }
    
    // Don't delete if it's currently active
    if (id == _activeProfileId) {
        DEBUG_PRINTLN("Cannot delete active profile");
        return false;
    }
    
    return _storage.deleteProfile(id);
}

bool ProfileManager::setActiveProfile(uint8_t id) {
    if (id >= MAX_PROFILES) {
        return false;
    }
    
    if (!_storage.profileExists(id)) {
        DEBUG_PRINTF("ERROR: Profile %d does not exist\n", id);
        return false;
    }
    
    if (!loadProfile(id)) {
        return false;
    }
    
    _activeProfileId = id;
    _saveActiveProfile();
    
    DEBUG_PRINTF("Switched to profile %d: %s\n", id, _currentProfile.name);
    
    return true;
}

Profile* ProfileManager::getCurrentProfile() {
    return &_currentProfile;
}

uint8_t ProfileManager::getActiveProfileId() {
    return _activeProfileId;
}

bool ProfileManager::profileExists(uint8_t id) {
    return _storage.profileExists(id);
}

uint8_t ProfileManager::getProfileCount() {
    return _storage.getProfileCount();
}

bool ProfileManager::getProfileInfo(uint8_t id, char* name, size_t* size) {
    return _storage.getProfileInfo(id, name, size);
}

bool ProfileManager::loadProfileById(uint8_t id, Profile& profile) {
    return _storage.loadProfile(id, profile);
}

bool ProfileManager::loadProfileIntoWorkBuffer(uint8_t id) {
    return _storage.loadProfile(id, _workProfile);
}

const Profile* ProfileManager::getWorkProfile() const {
    return &_workProfile;
}

size_t ProfileManager::getFreeSpace() {
    return _storage.getFreeSpace();
}

size_t ProfileManager::getTotalSpace() {
    return _storage.getTotalSpace();
}

void ProfileManager::factoryReset() {
    DEBUG_PRINTLN("Factory reset initiated...");
    
    // Clear all profiles
    _storage.format();
    
    // Clear preferences
    _prefs.clear();
    
    // Recreate default profiles
    initializeDefaultProfiles();
    
    // Load default profile
    loadProfile(0);
    _activeProfileId = 0;
    _saveActiveProfile();
    
    DEBUG_PRINTLN("Factory reset complete");
}

void ProfileManager::initializeDefaultProfiles() {
    DEBUG_PRINTLN("Creating default profiles...");

    // --- Profile 0: General ---
    populateDefaultProfile(_workProfile);
    _storage.saveProfile(_workProfile);
    DEBUG_PRINTLN("  - Profile 0: General");

    _encoderScratch[0] = _workProfile.encoders[0];
    _encoderScratch[1] = _workProfile.encoders[1];

    // --- Profile 1: Media ---
    memset(&_workProfile, 0, sizeof(Profile));
    _workProfile.id = 1;
    strcpy(_workProfile.name, "Media");
    _workProfile.version = 1;
    _workProfile.keys[0].action.type = ACTION_MEDIA;
    _workProfile.keys[0].action.config.media.function = MEDIA_FUNC_PREV;
    _workProfile.keys[1].action.type = ACTION_MEDIA;
    _workProfile.keys[1].action.config.media.function = MEDIA_FUNC_PLAY_PAUSE;
    _workProfile.keys[2].action.type = ACTION_MEDIA;
    _workProfile.keys[2].action.config.media.function = MEDIA_FUNC_NEXT;
    _workProfile.keys[3].action.type = ACTION_MEDIA;
    _workProfile.keys[3].action.config.media.function = MEDIA_FUNC_STOP;
    _workProfile.keys[4].action.type = ACTION_MEDIA;
    _workProfile.keys[4].action.config.media.function = MEDIA_FUNC_VOLUME_DOWN;
    _workProfile.keys[5].action.type = ACTION_MEDIA;
    _workProfile.keys[5].action.config.media.function = MEDIA_FUNC_MUTE;
    _workProfile.keys[6].action.type = ACTION_MEDIA;
    _workProfile.keys[6].action.config.media.function = MEDIA_FUNC_VOLUME_UP;
    _workProfile.keys[11].action.type = ACTION_PROFILE;
    _workProfile.keys[11].action.config.profile.profileId = 0;
    _workProfile.encoders[0] = _encoderScratch[0];
    _workProfile.encoders[1] = _encoderScratch[1];
    _storage.saveProfile(_workProfile);
    DEBUG_PRINTLN("  - Profile 1: Media");

    // --- Profile 2: VS Code ---
    populateVSCodeProfile(_workProfile);
    _storage.saveProfile(_workProfile);
    DEBUG_PRINTLN("  - Profile 2: VS Code");

    // --- Profile 3: Creative ---
    populateCreativeProfile(_workProfile);
    _storage.saveProfile(_workProfile);
    DEBUG_PRINTLN("  - Profile 3: Creative");

    DEBUG_PRINTF("Created %d default profiles\n", _storage.getProfileCount());
}

void ProfileManager::_saveActiveProfile() {
    _prefs.putUChar("activeProfile", _activeProfileId);
}

void ProfileManager::_loadActiveProfile() {
    _activeProfileId = _prefs.getUChar("activeProfile", DEFAULT_PROFILE);
    
    // Validate
    if (_activeProfileId >= MAX_PROFILES) {
        _activeProfileId = DEFAULT_PROFILE;
    }
}
