//
//  SettingsViewModel.swift
//  Micropad
//
//  ViewModel for settings
//

import Foundation
import Combine
import AppKit

@MainActor
class SettingsViewModel: ObservableObject {
    @Published var autoConnect = true
    @Published var startWithLogin = false
    @Published var autoReconnect = false
    @Published var newProcessName = ""
    @Published var newProfileId = 0
    @Published var processProfileMappings: [(String, Int)] = []
    
    private let storage = StorageService.shared
    
    init() {
        loadSettings()
    }
    
    private func loadSettings() {
        let settings = storage.loadSettings()
        autoConnect = settings.autoConnect
        startWithLogin = settings.startWithLogin
        autoReconnect = settings.autoReconnect
        processProfileMappings = settings.foregroundMonitorMappings.map { ($0.key, $0.value) }
        applyStartWithLogin(startWithLogin)
    }
    
    func saveSettings() {
        var settings = StorageService.AppSettings()
        settings.autoConnect = autoConnect
        settings.startWithLogin = startWithLogin
        settings.autoReconnect = autoReconnect
        settings.foregroundMonitorMappings = Dictionary(uniqueKeysWithValues: processProfileMappings)
        storage.saveSettings(settings)
        applyStartWithLogin(startWithLogin)
    }
    
    func addProcessMapping() {
        let trimmed = newProcessName.trimmingCharacters(in: .whitespaces)
        guard !trimmed.isEmpty else { return }
        
        if !processProfileMappings.contains(where: { $0.0 == trimmed }) {
            processProfileMappings.append((trimmed, newProfileId))
            newProcessName = ""
            saveSettings()
        }
    }
    
    func removeProcessMapping(_ processName: String) {
        processProfileMappings.removeAll { $0.0 == processName }
        saveSettings()
    }
    
    private func applyStartWithLogin(_ enable: Bool) {
        let bundleId = Bundle.main.bundleIdentifier ?? "com.senetlabs.Micropad"
        
        if enable {
            // Add to login items
            if let script = NSAppleScript(source: """
                tell application "System Events"
                    make login item at end with properties {path:"\(Bundle.main.bundlePath)", hidden:false}
                end tell
            """) {
                script.executeAndReturnError(nil)
            }
        } else {
            // Remove from login items
            if let script = NSAppleScript(source: """
                tell application "System Events"
                    delete login item "\(Bundle.main.bundlePath)"
                end tell
            """) {
                script.executeAndReturnError(nil)
            }
        }
    }
}
