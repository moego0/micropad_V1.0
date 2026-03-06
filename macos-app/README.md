# Micropad macOS Application

## Overview

Native macOS application for configuring and managing your Micropad wireless macropad via Bluetooth LE. Built with SwiftUI and Core Bluetooth.

## Requirements

- macOS 13.0 (Ventura) or later
- Xcode 15.0 or later
- Bluetooth LE support

## Building

1. Open `Micropad.xcodeproj` in Xcode
2. Select your development team in Signing & Capabilities
3. Build and run (⌘R)

Or from command line:

```bash
cd macos-app
xcodebuild -project Micropad.xcodeproj -scheme Micropad -configuration Debug
```

## Project Structure

```
Micropad/
├── MicropadApp.swift          # App entry point
├── MainView.swift             # Main window with sidebar
├── Models/                     # Data models
│   ├── BleDiscoveredDevice.swift
│   ├── MicroSlot.swift
│   └── MacroTag.swift
├── ViewModels/                 # MVVM ViewModels
│   ├── DevicesViewModel.swift
│   └── MacrosViewModel.swift
├── Views/                      # SwiftUI Views
│   ├── DevicesView.swift
│   ├── MacrosView.swift
│   ├── ProfilesView.swift
│   ├── StatsView.swift
│   └── SettingsView.swift
└── Services/                   # Business logic
    └── BluetoothService.swift  # Core Bluetooth wrapper
```

## Features

### Device & Connection
- **Device discovery** – Scan for Micropad devices via BLE
- **Connect / disconnect** – Pair and connect to the device
- **Connection status** – Real-time connection state

### Macro Builder
- **Micropad grid** – Visual representation of 12 keys + 2 encoders
- **Tag palette** – Drag-and-drop keyboard tags (modifiers, F-keys, media, mouse, etc.)
- **URL launcher** – Assign URLs to buttons with browser selection
- **Application launcher** – Assign applications to buttons
- **Visual feedback** – Selected slot highlighting

## Notes

- This is a port of the Windows WPF application to macOS
- Uses Core Bluetooth (native macOS BLE framework)
- SwiftUI for modern, native macOS UI
- Same BLE service UUIDs as Windows version for compatibility

## Future Enhancements

- Profile management
- Statistics tracking
- Settings persistence
- Auto-reconnect
- Per-app profile switching
