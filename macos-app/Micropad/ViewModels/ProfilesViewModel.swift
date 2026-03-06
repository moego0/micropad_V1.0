//
//  ProfilesViewModel.swift
//  Micropad
//
//  ViewModel for profile management
//

import Foundation
import Combine
import AppKit
import UniformTypeIdentifiers

@MainActor
class ProfilesViewModel: ObservableObject {
    @Published var profiles: [Profile] = []
    @Published var selectedProfile: Profile?
    @Published var keySlots: [KeyConfig] = []
    @Published var encoderSlots: [EncoderConfig] = []
    @Published var statusText = "Click Refresh to load profiles"
    @Published var newProfileName: String = ""
    @Published var newProfileId: Int = 0
    
    private let protocolHandler = ProtocolHandler.shared
    private let storage = StorageService.shared
    
    init() {
        loadLocalProfiles()
        if profiles.isEmpty {
            createDefaultProfile()
        }
    }
    
    private func loadLocalProfiles() {
        profiles = storage.loadProfiles()
    }
    
    private func createDefaultProfile() {
        let defaultProfile = Profile(id: 0, name: "Profile 1", version: 1)
        profiles = [defaultProfile]
        storage.saveProfiles(profiles)
    }
    
    func createNewProfile() {
        let name = newProfileName.isEmpty ? "Profile \(newProfileId + 1)" : newProfileName
        let id = newProfileId
        
        // Check if ID already exists
        if profiles.contains(where: { $0.id == id }) {
            statusText = "Profile ID \(id) already exists"
            return
        }
        
        let newProfile = Profile(id: id, name: name, version: 1)
        profiles.append(newProfile)
        storage.saveProfiles(profiles)
        statusText = "Created profile: \(name) (ID: \(id))"
        newProfileName = ""
        newProfileId = profiles.count
        
        // Select the new profile
        selectedProfile = newProfile
        loadProfileDetails(newProfile)
    }
    
    func deleteProfile(_ profile: Profile) {
        profiles.removeAll { $0.id == profile.id }
        storage.saveProfiles(profiles)
        if selectedProfile?.id == profile.id {
            selectedProfile = profiles.first
            if let profile = selectedProfile {
                loadProfileDetails(profile)
            }
        }
        statusText = "Deleted profile: \(profile.name)"
    }
    
    func refreshProfiles() async {
        statusText = "Loading profiles..."
        do {
            let deviceProfiles = try await protocolHandler.listProfiles()
            profiles = deviceProfiles
            if profiles.isEmpty {
                // Fallback to local storage
                profiles = storage.loadProfiles()
            }
            statusText = "Loaded \(profiles.count) profile(s)"
        } catch {
            statusText = "Failed to load profiles: \(error.localizedDescription)"
            // Fallback to local storage
            profiles = storage.loadProfiles()
        }
    }
    
    func selectProfile(_ profile: Profile) {
        selectedProfile = profile
        loadProfileDetails(profile)
    }
    
    private func loadProfileDetails(_ profile: Profile) {
        // Ensure 12 keys
        keySlots = profile.keys
        while keySlots.count < 12 {
            keySlots.append(KeyConfig(
                index: keySlots.count,
                type: .none,
                modifiers: 0,
                key: 0,
                function: 0,
                action: 0,
                value: 0,
                profileId: 0
            ))
        }
        
        // Ensure 2 encoders
        encoderSlots = profile.encoders
        while encoderSlots.count < 2 {
            encoderSlots.append(EncoderConfig(
                index: encoderSlots.count,
                acceleration: false,
                stepsPerDetent: 4
            ))
        }
    }
    
    func saveProfile() async {
        guard var profile = selectedProfile else { return }
        profile.keys = keySlots
        profile.encoders = encoderSlots
        
        // Update profile in list
        if let index = profiles.firstIndex(where: { $0.id == profile.id }) {
            profiles[index] = profile
            selectedProfile = profile
        }
        
        storage.saveProfiles(profiles)
        
        do {
            try await protocolHandler.saveProfile(profile)
            statusText = "Profile saved to device"
        } catch {
            statusText = "Profile saved locally. Device sync failed: \(error.localizedDescription)"
        }
    }
    
    func updateProfileName(_ name: String) {
        guard var profile = selectedProfile else { return }
        let trimmedName = name.trimmingCharacters(in: .whitespaces)
        guard !trimmedName.isEmpty else { return }
        profile.name = trimmedName
        if let index = profiles.firstIndex(where: { $0.id == profile.id }) {
            profiles[index] = profile
            selectedProfile = profile
            storage.saveProfiles(profiles)
        }
    }
    
    func importProfile() {
        let panel = NSOpenPanel()
        panel.allowsMultipleSelection = false
        panel.canChooseDirectories = false
        panel.canChooseFiles = true
        if #available(macOS 11.0, *) {
            panel.allowedContentTypes = [.json]
        }
        
        if panel.runModal() == .OK, let url = panel.url {
            do {
                let data = try Data(contentsOf: url)
                let profile = try JSONDecoder().decode(Profile.self, from: data)
                profiles.append(profile)
                storage.saveProfiles(profiles)
                statusText = "Profile imported"
            } catch {
                statusText = "Failed to import: \(error.localizedDescription)"
            }
        }
    }
    
    func exportProfile(_ profile: Profile) {
        let panel = NSSavePanel()
        if #available(macOS 11.0, *) {
            panel.allowedContentTypes = [.json]
        }
        panel.nameFieldStringValue = "\(profile.name).json"
        
        if panel.runModal() == .OK, let url = panel.url {
            do {
                let data = try JSONEncoder().encode(profile)
                try data.write(to: url)
                statusText = "Profile exported"
            } catch {
                statusText = "Failed to export: \(error.localizedDescription)"
            }
        }
    }
}
