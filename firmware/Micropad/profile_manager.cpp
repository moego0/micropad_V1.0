#include "profiles/profile_manager.h"
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
    if (_storage.getProfileCount() == 0) {
        DEBUG_PRINTLN("No profiles found, creating defaults...");
        initializeDefaultProfiles();
    }
    
    // Load the active profile
    if (!loadProfile(_activeProfileId)) {
        DEBUG_PRINTLN("Failed to load active profile, loading default...");
        if (!loadProfile(0)) {
            DEBUG_PRINTLN("ERROR: Failed to load default profile!");
            return false;
        }
    }
    
    _initialized = true;
    DEBUG_PRINTF("Profile Manager initialized (active profile: %d)\n", _activeProfileId);
    
    return true;
}

bool ProfileManager::loadProfile(uint8_t id) {
    if (id >= MAX_PROFILES) {
        DEBUG_PRINTF("ERROR: Invalid profile ID: %d\n", id);
        return false;
    }
    
    Profile tempProfile;
    
    if (!_storage.loadProfile(id, tempProfile)) {
        return false;
    }
    
    // Successfully loaded, update current profile
    _currentProfile = tempProfile;
    _activeProfileId = id;
    
    DEBUG_PRINTF("Loaded profile %d: %s\n", id, _currentProfile.name);
    
    return true;
}

bool ProfileManager::saveProfile(uint8_t id, const Profile& profile) {
    if (id >= MAX_PROFILES) {
        DEBUG_PRINTF("ERROR: Invalid profile ID: %d\n", id);
        return false;
    }
    
    Profile profileCopy = profile;
    profileCopy.id = id;  // Ensure ID matches
    
    return _storage.saveProfile(profileCopy);
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
    
    // Use a single Profile on stack to avoid stack overflow (Profile is large)
    Profile p;
    EncoderConfig generalEncoders[2];
    
    // Profile 0: General
    p = createDefaultProfile();
    p.id = 0;
    generalEncoders[0] = p.encoders[0];
    generalEncoders[1] = p.encoders[1];
    _storage.saveProfile(p);
    DEBUG_PRINTLN("  - Profile 0: General");
    
    // Profile 1: Media
    p.id = 1;
    strcpy(p.name, "Media");
    p.version = 1;
    for (uint8_t i = 0; i < MATRIX_KEYS; i++) {
        p.keys[i].action.type = ACTION_NONE;
    }
    p.keys[0].action.type = ACTION_MEDIA;
    p.keys[0].action.config.media.function = MEDIA_FUNC_PREV;
    p.keys[1].action.type = ACTION_MEDIA;
    p.keys[1].action.config.media.function = MEDIA_FUNC_PLAY_PAUSE;
    p.keys[2].action.type = ACTION_MEDIA;
    p.keys[2].action.config.media.function = MEDIA_FUNC_NEXT;
    p.keys[3].action.type = ACTION_MEDIA;
    p.keys[3].action.config.media.function = MEDIA_FUNC_STOP;
    p.keys[4].action.type = ACTION_MEDIA;
    p.keys[4].action.config.media.function = MEDIA_FUNC_VOLUME_DOWN;
    p.keys[5].action.type = ACTION_MEDIA;
    p.keys[5].action.config.media.function = MEDIA_FUNC_MUTE;
    p.keys[6].action.type = ACTION_MEDIA;
    p.keys[6].action.config.media.function = MEDIA_FUNC_VOLUME_UP;
    p.keys[11].action.type = ACTION_PROFILE;
    p.keys[11].action.config.profile.profileId = 0;
    p.encoders[0] = generalEncoders[0];
    p.encoders[1] = generalEncoders[1];
    _storage.saveProfile(p);
    DEBUG_PRINTLN("  - Profile 1: Media");
    
    // Profile 2: VS Code
    p = createVSCodeProfile();
    p.id = 2;
    _storage.saveProfile(p);
    DEBUG_PRINTLN("  - Profile 2: VS Code");
    
    // Profile 3: Creative
    p = createCreativeProfile();
    p.id = 3;
    _storage.saveProfile(p);
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
