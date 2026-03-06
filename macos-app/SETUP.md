# macOS App Setup Instructions

## Initial Setup

1. **Open the project in Xcode:**
   ```bash
   cd macos-app
   open Micropad.xcodeproj
   ```

2. **All Swift files are already in the project.** The `project.pbxproj` includes:
   - `Models/` (BleDiscoveredDevice, MicroSlot, MacroTag, Profile)
   - `ViewModels/` (Devices, Macros, Profiles, Stats, Settings)
   - `Views/` (Devices, Macros, Profiles, Stats, Settings)
   - `Services/` (BluetoothService, ProtocolHandler, StorageService)
   - Plus `MainView.swift`, `MicropadApp.swift`, `ContentView.swift`
   No manual "Add Files" step is needed.

3. **Configure Signing:**
   - Select the "Micropad" target
   - Go to "Signing & Capabilities"
   - Select your development team
   - Xcode will automatically create the necessary provisioning profile

4. **Verify Entitlements:**
   - The `Micropad.entitlements` file should already be configured
   - It includes Bluetooth permissions required for BLE

5. **Build and Run:** (no need to add any files manually)
   - Press ⌘R or click the Run button
   - The app should build and launch

## Project Structure

All of these are already included in the Xcode project:

```
Micropad/
├── MicropadApp.swift          ✅
├── ContentView.swift          ✅
├── MainView.swift             ✅
├── Models/
│   ├── BleDiscoveredDevice.swift  ✅
│   ├── MicroSlot.swift           ✅
│   ├── MacroTag.swift            ✅
│   └── Profile.swift             ✅
├── ViewModels/
│   ├── DevicesViewModel.swift    ✅
│   ├── MacrosViewModel.swift     ✅
│   ├── ProfilesViewModel.swift   ✅
│   ├── StatsViewModel.swift      ✅
│   └── SettingsViewModel.swift   ✅
├── Views/
│   ├── DevicesView.swift         ✅
│   ├── MacrosView.swift          ✅
│   ├── ProfilesView.swift        ✅
│   ├── StatsView.swift           ✅
│   └── SettingsView.swift        ✅
└── Services/
    ├── BluetoothService.swift    ✅
    ├── ProtocolHandler.swift     ✅
    └── StorageService.swift      ✅
```

## Features Implemented

✅ **Main Window** - Sidebar navigation with 5 tabs
✅ **Devices View** - BLE device discovery and connection
✅ **Macros View** - Micropad grid with drag-and-drop tag builder
✅ **URL Launcher** - Assign URLs to buttons with browser selection
✅ **Application Launcher** - Assign macOS apps to buttons
✅ **Bluetooth Service** - Core Bluetooth wrapper for BLE communication

## Features Not Yet Implemented

⚠️ **Profiles View** - Placeholder (needs implementation)
⚠️ **Stats View** - Placeholder (needs implementation)
⚠️ **Settings View** - Placeholder (needs implementation)
⚠️ **Profile Storage** - Needs UserDefaults or Core Data implementation
⚠️ **Protocol Handler** - Needs BLE characteristic read/write implementation
⚠️ **Macro Recording** - Needs keyboard event capture

## Notes

- The app uses the same BLE service UUID (`4fafc201-1fb5-459e-8fcc-c5c9c331914b`) as the Windows version for compatibility
- macOS uses `open` command for launching applications and URLs (different from Windows `{RUN:}` syntax)
- The drag-and-drop implementation uses SwiftUI's native drop delegate

## Troubleshooting

**Build errors about missing files:**
- Make sure all Swift files are added to the target in Xcode

**Bluetooth not working:**
- Check that Bluetooth permissions are granted in System Preferences > Security & Privacy > Privacy > Bluetooth
- Verify the entitlements file includes `com.apple.security.device.bluetooth`

**App won't launch:**
- Check Console.app for error messages
- Verify code signing is configured correctly
