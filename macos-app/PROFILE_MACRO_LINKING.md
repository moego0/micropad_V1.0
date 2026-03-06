# Profile-Macro Linking Implementation

## Overview

The Micro (Macros) tab and Profiles tab are now fully linked. Users can:
1. Create profiles with custom names and IDs
2. Assign macro sequences to profile keys
3. Switch between profiles to customize different macro sets
4. Edit profile names in both tabs

## Implementation Details

### 1. **Profile Model Updates**
- Added `macroSequence: String?` field to `KeyConfig`
- Macro sequences are stored per key in each profile
- When a key has a macro sequence, its `type` is set to `.macro`

### 2. **MacrosViewModel Updates**
- **Profile Management:**
  - `availableProfiles` - List of all profiles
  - `selectedProfile` - Currently active profile for macro editing
  - `selectProfile(_:)` - Switch profiles (saves current, loads new)
  - `createNewProfile(name:id:)` - Create new profile
  - `updateProfileName(_:)` - Rename current profile
  - `refreshProfiles()` - Reload profiles from storage

- **Macro Persistence:**
  - `loadProfileMacros()` - Load macro sequences from selected profile's keys
  - `saveCurrentProfileMacros()` - Save macro sequences to profile (auto-saves on every change)
  - Automatically saves when:
    - Tag is appended
    - Tag is dropped on slot
    - URL/App is inserted
    - Sequence is edited in text box
    - Slot is cleared

### 3. **MacrosView Updates**
- **Profile Selector Header:**
  - Dropdown to select active profile
  - "New Profile" button opens dialog
  - Text field to edit profile name inline
  - Shows profile name and ID

- **Create Profile Dialog:**
  - Enter profile name
  - Enter profile ID (0-7 typically)
  - Validates ID uniqueness

### 4. **ProfilesViewModel Updates**
- **Profile Creation:**
  - `createNewProfile()` - Creates profile with name and ID
  - Validates ID uniqueness
  - Auto-selects newly created profile

- **Profile Editing:**
  - `updateProfileName(_:)` - Updates profile name
  - `deleteProfile(_:)` - Removes profile
  - Profile name editor in ProfilesView

### 5. **Storage Synchronization**
- Both ViewModels use `StorageService.shared`
- Profiles are saved/loaded from UserDefaults
- MacrosView refreshes profiles on appear
- Changes in one tab are reflected in the other

## User Workflow

### Creating a Profile:
1. Go to **Macros** tab
2. Click **"New Profile"** button
3. Enter profile name (e.g., "Gaming")
4. Enter profile ID (e.g., 1)
5. Click **"Create"**

### Assigning Macros to Profile:
1. Select a profile from dropdown
2. Click a key/encoder slot in the grid
3. Either:
   - Click tags from the palette to append
   - Drag tags onto grid slots
   - Type in the sequence text box
   - Use "Add URL" or "Add App" buttons
4. Macros are **automatically saved** to the profile

### Editing Profile Name:
- **In Macros tab:** Edit the text field next to profile selector
- **In Profiles tab:** Edit the text field in profile editor

### Switching Profiles:
- Select different profile from dropdown
- Current macros are saved automatically
- New profile's macros are loaded

## Data Flow

```
MacrosView
  └─> MacrosViewModel
       ├─> selectedProfile (Profile)
       ├─> slots[0-11] (MicroSlot) - K1-K12
       └─> saveCurrentProfileMacros()
            └─> Updates Profile.keys[].macroSequence
                 └─> StorageService.shared.saveProfiles()
                      └─> UserDefaults

ProfilesView
  └─> ProfilesViewModel
       ├─> profiles[] (Profile[])
       └─> StorageService.shared.saveProfiles()
            └─> UserDefaults
```

## Key Features

✅ **Auto-save:** Macros save automatically when changed  
✅ **Profile switching:** Seamlessly switch between profiles  
✅ **Name editing:** Edit profile names in both tabs  
✅ **ID management:** Create profiles with custom IDs  
✅ **Synchronization:** Both tabs stay in sync via shared storage  
✅ **Default profile:** Creates "Profile 1" (ID: 0) on first launch  

## Files Modified

- `Models/Profile.swift` - Added `macroSequence` to `KeyConfig`
- `ViewModels/MacrosViewModel.swift` - Added profile management and auto-save
- `ViewModels/ProfilesViewModel.swift` - Added profile creation and name editing
- `Views/MacrosView.swift` - Added profile selector header and create dialog
- `Views/ProfilesView.swift` - Added profile name editor

All changes are backward compatible and work seamlessly together! 🎉
