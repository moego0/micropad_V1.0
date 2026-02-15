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
    
    // Profile 0: General
    Profile generalProfile = createDefaultProfile();
    generalProfile.id = 0;
    _storage.saveProfile(generalProfile);
    DEBUG_PRINTLN("  - Profile 0: General");
    
    // Profile 1: Media
    Profile mediaProfile;
    mediaProfile.id = 1;
    strcpy(mediaProfile.name, "Media");
    mediaProfile.version = 1;
    
    // All keys are media controls
    for (uint8_t i = 0; i < MATRIX_KEYS; i++) {
        mediaProfile.keys[i].action.type = ACTION_NONE;
    }
    
    // K1-K4: Media controls
    mediaProfile.keys[0].action.type = ACTION_MEDIA;
    mediaProfile.keys[0].action.config.media.function = MEDIA_FUNC_PREV;
    
    mediaProfile.keys[1].action.type = ACTION_MEDIA;
    mediaProfile.keys[1].action.config.media.function = MEDIA_FUNC_PLAY_PAUSE;
    
    mediaProfile.keys[2].action.type = ACTION_MEDIA;
    mediaProfile.keys[2].action.config.media.function = MEDIA_FUNC_NEXT;
    
    mediaProfile.keys[3].action.type = ACTION_MEDIA;
    mediaProfile.keys[3].action.config.media.function = MEDIA_FUNC_STOP;
    
    // K5-K7: Volume controls
    mediaProfile.keys[4].action.type = ACTION_MEDIA;
    mediaProfile.keys[4].action.config.media.function = MEDIA_FUNC_VOLUME_DOWN;
    
    mediaProfile.keys[5].action.type = ACTION_MEDIA;
    mediaProfile.keys[5].action.config.media.function = MEDIA_FUNC_MUTE;
    
    mediaProfile.keys[6].action.type = ACTION_MEDIA;
    mediaProfile.keys[6].action.config.media.function = MEDIA_FUNC_VOLUME_UP;
    
    // K12: Switch back to General profile
    mediaProfile.keys[11].action.type = ACTION_PROFILE;
    mediaProfile.keys[11].action.config.profile.profileId = 0;
    
    // Encoders same as general
    mediaProfile.encoders[0] = generalProfile.encoders[0];
    mediaProfile.encoders[1] = generalProfile.encoders[1];
    
    _storage.saveProfile(mediaProfile);
    DEBUG_PRINTLN("  - Profile 1: Media");
    
    // Profile 2: VS Code
    Profile vscodeProfile = createVSCodeProfile();
    vscodeProfile.id = 2;
    _storage.saveProfile(vscodeProfile);
    DEBUG_PRINTLN("  - Profile 2: VS Code");
    
    // Profile 3: Creative (Photoshop/etc)
    Profile creativeProfile = createCreativeProfile();
    creativeProfile.id = 3;
    _storage.saveProfile(creativeProfile);
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
