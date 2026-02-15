#ifndef PROFILE_MANAGER_H
#define PROFILE_MANAGER_H

#include <Arduino.h>
#include <Preferences.h>
#include "config.h"
#include "profile.h"
#include "profile_storage.h"

class ProfileManager {
public:
    ProfileManager();
    
    // Initialize
    bool init();
    
    // Profile management
    bool loadProfile(uint8_t id);
    bool saveProfile(uint8_t id, const Profile& profile);
    bool deleteProfile(uint8_t id);
    bool setActiveProfile(uint8_t id);
    
    // Get current profile
    Profile* getCurrentProfile();
    uint8_t getActiveProfileId();
    
    // Profile queries
    bool profileExists(uint8_t id);
    uint8_t getProfileCount();
    bool getProfileInfo(uint8_t id, char* name, size_t* size);
    bool loadProfileById(uint8_t id, Profile& profile);
    
    // Factory reset
    void factoryReset();
    
    // Initialize with default profiles
    void initializeDefaultProfiles();
    
private:
    ProfileStorage _storage;
    Preferences _prefs;
    Profile _currentProfile;
    uint8_t _activeProfileId;
    bool _initialized;
    
    void _saveActiveProfile();
    void _loadActiveProfile();
};

#endif // PROFILE_MANAGER_H
