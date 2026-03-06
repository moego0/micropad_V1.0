//
//  StorageService.swift
//  Micropad
//
//  Local storage using UserDefaults
//

import Foundation

class StorageService {
    static let shared = StorageService()
    
    private let defaults = UserDefaults.standard
    private let profilesKey = "Micropad.Profiles"
    private let settingsKey = "Micropad.Settings"
    
    // MARK: - Profiles
    
    func saveProfiles(_ profiles: [Profile]) {
        if let data = try? JSONEncoder().encode(profiles) {
            defaults.set(data, forKey: profilesKey)
        }
    }
    
    func loadProfiles() -> [Profile] {
        guard let data = defaults.data(forKey: profilesKey),
              let profiles = try? JSONDecoder().decode([Profile].self, from: data) else {
            return []
        }
        return profiles
    }
    
    // MARK: - Settings
    
    struct AppSettings: Codable {
        var autoConnect: Bool = true
        var startWithLogin: Bool = false
        var autoReconnect: Bool = false
        var foregroundMonitorMappings: [String: Int] = [:]
    }
    
    func saveSettings(_ settings: AppSettings) {
        if let data = try? JSONEncoder().encode(settings) {
            defaults.set(data, forKey: settingsKey)
        }
    }
    
    func loadSettings() -> AppSettings {
        guard let data = defaults.data(forKey: settingsKey),
              let settings = try? JSONDecoder().decode(AppSettings.self, from: data) else {
            return AppSettings()
        }
        return settings
    }
}
