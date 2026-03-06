//
//  MicropadApp.swift
//  Micropad
//
//  macOS application for Micropad wireless macropad
//

import SwiftUI

@main
struct MicropadApp: App {
    @StateObject private var appState = AppState()
    
    var body: some Scene {
        WindowGroup {
            MainView()
                .environmentObject(appState)
                .frame(minWidth: 800, minHeight: 500)
        }
        .windowStyle(.automatic)
        .commands {
            CommandGroup(replacing: .newItem) {}
        }
    }
}

// Global app state
class AppState: ObservableObject {
    @Published var selectedTab: Tab = .devices
    
    enum Tab: String, CaseIterable {
        case devices = "Devices"
        case profiles = "Profiles"
        case macros = "Macros"
        case stats = "Stats"
        case settings = "Settings"
    }
}
