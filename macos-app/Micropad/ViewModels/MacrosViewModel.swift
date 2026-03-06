//
//  MacrosViewModel.swift
//  Micropad
//
//  ViewModel for macro/shortcut builder
//

import Foundation
import Combine

@MainActor
class MacrosViewModel: ObservableObject {
    @Published var slots: [MicroSlot] = []
    @Published var selectedSlotIndex: Int? = nil
    @Published var selectedSequence: String = ""
    @Published var allTags: [MacroTag] = []
    @Published var urlInput: String = ""
    @Published var applicationPathInput: String = ""
    @Published var selectedBrowser: String = "Default"
    @Published var selectedProfile: Profile? = nil
    @Published var availableProfiles: [Profile] = []
    
    let browsers = ["Default", "Chrome", "Safari", "Firefox", "Edge", "Opera", "Brave"]
    
    
    var selectedSlot: MicroSlot? {
        guard let index = selectedSlotIndex, index >= 0, index < slots.count else { return nil }
        return slots[index]
    }
    
    init() {
        initSlots()
        allTags = MacroTagCatalog.getAll()
        loadProfiles()
    }
    
    private func loadProfiles() {
        availableProfiles = StorageService.shared.loadProfiles()
        if availableProfiles.isEmpty {
            // Create default profile
            createDefaultProfile()
        } else {
            selectedProfile = availableProfiles.first
            loadProfileMacros()
        }
    }
    
    private func createDefaultProfile() {
        let newProfile = Profile(id: 0, name: "Profile 1", version: 1)
        availableProfiles = [newProfile]
        selectedProfile = newProfile
        StorageService.shared.saveProfiles(availableProfiles)
    }
    
    func selectProfile(_ profile: Profile) {
        saveCurrentProfileMacros()
        selectedProfile = profile
        loadProfileMacros()
    }
    
    func createNewProfile(name: String, id: Int) {
        saveCurrentProfileMacros()
        
        // Check if ID already exists
        if availableProfiles.contains(where: { $0.id == id }) {
            return
        }
        
        let newProfile = Profile(id: id, name: name, version: 1)
        availableProfiles.append(newProfile)
        selectedProfile = newProfile
        StorageService.shared.saveProfiles(availableProfiles)
        loadProfileMacros()
    }
    
    func updateProfileName(_ name: String) {
        guard var profile = selectedProfile else { return }
        let trimmedName = name.trimmingCharacters(in: .whitespaces)
        guard !trimmedName.isEmpty else { return }
        profile.name = trimmedName
        if let index = availableProfiles.firstIndex(where: { $0.id == profile.id }) {
            availableProfiles[index] = profile
            selectedProfile = profile
            StorageService.shared.saveProfiles(availableProfiles)
        }
    }
    
    func refreshProfiles() {
        let currentProfileId = selectedProfile?.id
        availableProfiles = StorageService.shared.loadProfiles()
        if let id = currentProfileId, let profile = availableProfiles.first(where: { $0.id == id }) {
            selectedProfile = profile
            loadProfileMacros()
        } else if let first = availableProfiles.first {
            selectedProfile = first
            loadProfileMacros()
        }
    }
    
    private func loadProfileMacros() {
        guard let profile = selectedProfile else { return }
        
        // Load macro sequences from profile keys
        for i in 0..<12 {
            if i < profile.keys.count {
                let keyConfig = profile.keys[i]
                if let sequence = keyConfig.macroSequence {
                    slots[i].sequence = sequence
                } else {
                    slots[i].sequence = ""
                }
            } else {
                slots[i].sequence = ""
            }
        }
        
        // Load encoder sequences if any
        // Note: Encoders don't have macro sequences in the current model, but we can add them
    }
    
    func saveCurrentProfileMacros() {
        guard var profile = selectedProfile else { return }
        
        // Save macro sequences to profile keys
        for i in 0..<12 {
            if i < slots.count {
                let sequence = slots[i].sequence
                if i < profile.keys.count {
                    profile.keys[i].macroSequence = sequence.isEmpty ? nil : sequence
                    if !sequence.isEmpty {
                        profile.keys[i].type = .macro
                    }
                } else {
                    // Create new KeyConfig with macro sequence
                    var newKey = KeyConfig(
                        index: i,
                        type: sequence.isEmpty ? .none : .macro,
                        modifiers: 0,
                        key: 0,
                        function: 0,
                        action: 0,
                        value: 0,
                        profileId: profile.id
                    )
                    newKey.macroSequence = sequence.isEmpty ? nil : sequence
                    profile.keys.append(newKey)
                }
            }
        }
        
        // Update profile in list
        if let index = availableProfiles.firstIndex(where: { $0.id == profile.id }) {
            availableProfiles[index] = profile
            selectedProfile = profile
            StorageService.shared.saveProfiles(availableProfiles)
        }
    }
    
    private func initSlots() {
        slots = []
        for i in 0..<12 {
            slots.append(MicroSlot(index: i, label: "K\(i + 1)", isEncoder: false))
        }
        slots.append(MicroSlot(index: 12, label: "E1", isEncoder: true))
        slots.append(MicroSlot(index: 13, label: "E2", isEncoder: true))
    }
    
    func selectSlot(_ index: Int) {
        selectedSlotIndex = index
        updateSelectedSequence()
    }
    
    private func updateSelectedSequence() {
        selectedSequence = selectedSlot?.sequence ?? ""
    }
    
    func updateSelectedSequence(_ newSequence: String) {
        guard let slot = selectedSlot else { return }
        slot.sequence = newSequence
        selectedSequence = newSequence
        saveCurrentProfileMacros()
    }
    
    func appendTag(_ tag: String) {
        guard let slot = selectedSlot else { return }
        slot.sequence += tag
        selectedSequence = slot.sequence
        saveCurrentProfileMacros()
    }
    
    func dropTagOnSlot(_ slotIndex: Int, tag: String) {
        guard slotIndex >= 0, slotIndex < slots.count else { return }
        slots[slotIndex].sequence += tag
        selectedSlotIndex = slotIndex
        selectedSequence = slots[slotIndex].sequence
        saveCurrentProfileMacros()
    }
    
    func insertUrl() {
        guard let slot = selectedSlot else { return }
        var url = urlInput.trimmingCharacters(in: .whitespaces)
        if !url.hasPrefix("http://") && !url.hasPrefix("https://") {
            url = "https://" + url
        }
        
        let browserTag: String
        switch selectedBrowser {
        case "Chrome":
            browserTag = "{RUN:open -a \"Google Chrome\" \(url)}"
        case "Safari":
            browserTag = "{RUN:open -a Safari \(url)}"
        case "Firefox":
            browserTag = "{RUN:open -a Firefox \(url)}"
        case "Edge":
            browserTag = "{RUN:open -a \"Microsoft Edge\" \(url)}"
        case "Opera":
            browserTag = "{RUN:open -a Opera \(url)}"
        case "Brave":
            browserTag = "{RUN:open -a Brave \(url)}"
        default:
            browserTag = "{RUN:open \(url)}"
        }
        
        slot.sequence += browserTag
        selectedSequence = slot.sequence
        urlInput = ""
        saveCurrentProfileMacros()
    }
    
    func insertApplication() {
        guard let slot = selectedSlot else { return }
        let path = applicationPathInput.trimmingCharacters(in: .whitespaces)
        let appTag = "{RUN:open -a \"\(path)\"}"
        slot.sequence += appTag
        selectedSequence = slot.sequence
        applicationPathInput = ""
        saveCurrentProfileMacros()
    }
    
    func clearSlot(_ index: Int) {
        guard index >= 0, index < slots.count else { return }
        slots[index].sequence = ""
        if selectedSlotIndex == index {
            selectedSequence = ""
        }
        saveCurrentProfileMacros()
    }
    
    deinit {
        saveCurrentProfileMacros()
    }
}
