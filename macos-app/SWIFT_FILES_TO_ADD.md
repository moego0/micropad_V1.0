# Swift Files to Add to Xcode Project

## Instructions

1. Open `Micropad.xcodeproj` in Xcode
2. Right-click on the "Micropad" folder in Project Navigator
3. Select "Add Files to Micropad..."
4. Add the following files/folders (make sure "Copy items if needed" is **unchecked**):

## Files to Add

### ✅ Already in Project
- `MicropadApp.swift`
- `ContentView.swift`
- `MainView.swift`

### 📁 Models Folder
Add the entire `Models/` folder:
- `Models/BleDiscoveredDevice.swift`
- `Models/MicroSlot.swift`
- `Models/MacroTag.swift`
- `Models/Profile.swift` ⭐ **NEW**

### 📁 ViewModels Folder
Add the entire `ViewModels/` folder:
- `ViewModels/DevicesViewModel.swift`
- `ViewModels/MacrosViewModel.swift`
- `ViewModels/ProfilesViewModel.swift` ⭐ **NEW**
- `ViewModels/StatsViewModel.swift` ⭐ **NEW**
- `ViewModels/SettingsViewModel.swift` ⭐ **NEW**

### 📁 Views Folder
Add the entire `Views/` folder:
- `Views/DevicesView.swift`
- `Views/MacrosView.swift`
- `Views/ProfilesView.swift` ⭐ **UPDATED** (was placeholder)
- `Views/StatsView.swift` ⭐ **UPDATED** (was placeholder)
- `Views/SettingsView.swift` ⭐ **UPDATED** (was placeholder)

### 📁 Services Folder
Add the entire `Services/` folder:
- `Services/BluetoothService.swift`
- `Services/ProtocolHandler.swift` ⭐ **NEW**
- `Services/StorageService.swift` ⭐ **NEW**

## Summary

**Total files to add:**
- 1 new Model file
- 3 new ViewModel files
- 3 updated View files (replacing placeholders)
- 2 new Service files

**Total: 9 new/updated files**

## After Adding Files

1. **Build the project** (⌘B) to check for any compilation errors
2. **Fix any import issues** - make sure all imports are correct
3. **Test each view** - navigate through all tabs to ensure they work

## Notes

- All files use SwiftUI and Combine
- Models use Codable for JSON serialization
- ViewModels use `@Published` for reactive updates
- Services use singletons for shared state
- Settings use UserDefaults for persistence
- Profiles use JSON files for import/export
