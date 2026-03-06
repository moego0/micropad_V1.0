//
//  Profile.swift
//  Micropad
//
//  Profile model for device configuration
//

import Foundation

struct Profile: Codable, Identifiable, Hashable {
    let id: Int
    var name: String
    var version: Int
    var keys: [KeyConfig]
    var encoders: [EncoderConfig]
    
    init(id: Int = 0, name: String = "Unnamed", version: Int = 1, keys: [KeyConfig] = [], encoders: [EncoderConfig] = []) {
        self.id = id
        self.name = name
        self.version = version
        self.keys = keys
        self.encoders = encoders
    }
}

struct KeyConfig: Codable, Identifiable, Hashable {
    var index: Int
    var type: ActionType
    var modifiers: Int
    var key: Int
    var text: String?
    var function: Int
    var action: Int
    var value: Int
    var profileId: Int
    var appPath: String?
    var url: String?
    var macroId: String?
    var macroSequence: String? // Macro sequence string (e.g. "{CTRL}A{F5}")
    
    var id: Int { index }
    
    var displayName: String {
        switch type {
        case .none:
            return "Not Assigned"
        case .hotkey:
            return "Hotkey: \(hotkeyString)"
        case .text:
            let preview = text?.prefix(20) ?? ""
            return "Text: \(preview)\(text?.count ?? 0 > 20 ? "..." : "")"
        case .media:
            return "Media: \(mediaFunctionName)"
        case .mouse:
            return "Mouse: \(mouseActionName)"
        case .layer:
            return "Layer: \(value)"
        case .profile:
            return "Switch to Profile \(profileId)"
        case .app:
            if let path = appPath {
                return "App: \(URL(fileURLWithPath: path).lastPathComponent)"
            }
            return "Launch App"
        case .url:
            if let url = url {
                let preview = url.count > 20 ? String(url.prefix(20)) + "..." : url
                return "URL: \(preview)"
            }
            return "Open URL"
        case .macro:
            if let sequence = macroSequence, !sequence.isEmpty {
                let preview = sequence.count > 20 ? String(sequence.prefix(20)) + "..." : sequence
                return "Macro: \(preview)"
            }
            if let macroId = macroId {
                return "Macro: \(macroId)"
            }
            return "Macro"
        }
    }
    
    private var hotkeyString: String {
        var parts: [String] = []
        if (modifiers & 0x01) != 0 { parts.append("Ctrl") }
        if (modifiers & 0x02) != 0 { parts.append("Shift") }
        if (modifiers & 0x04) != 0 { parts.append("Alt") }
        if (modifiers & 0x08) != 0 { parts.append("Cmd") }
        parts.append(keyName)
        return parts.joined(separator: "+")
    }
    
    private var keyName: String {
        if key >= 0x04 && key <= 0x1D {
            return String(Character(UnicodeScalar(0x41 + key - 0x04)!))
        }
        if key >= 0x1E && key <= 0x26 {
            return String(key - 0x1E + 1)
        }
        if key == 0x27 { return "0" }
        
        switch key {
        case 0x28: return "Enter"
        case 0x29: return "Esc"
        case 0x2A: return "Backspace"
        case 0x2B: return "Tab"
        case 0x2C: return "Space"
        default: return String(format: "Key%02X", key)
        }
    }
    
    private var mediaFunctionName: String {
        switch function {
        case 0: return "VolumeUp"
        case 1: return "VolumeDown"
        case 2: return "Mute"
        case 3: return "PlayPause"
        case 4: return "Next"
        case 5: return "Prev"
        case 6: return "Stop"
        default: return "Unknown"
        }
    }
    
    private var mouseActionName: String {
        switch action {
        case 0: return "Click"
        case 1: return "RightClick"
        case 2: return "MiddleClick"
        case 3: return "ScrollUp"
        case 4: return "ScrollDown"
        default: return "Unknown"
        }
    }
}

struct EncoderConfig: Codable, Identifiable, Hashable {
    var index: Int
    var acceleration: Bool
    var stepsPerDetent: Int
    
    var id: Int { index }
}

enum ActionType: Int, Codable, CaseIterable {
    case none = 0
    case hotkey = 1
    case macro = 2
    case text = 3
    case media = 4
    case mouse = 5
    case layer = 6
    case profile = 7
    case app = 8
    case url = 9
    
    var displayName: String {
        switch self {
        case .none: return "None"
        case .hotkey: return "Hotkey"
        case .macro: return "Macro"
        case .text: return "Text"
        case .media: return "Media"
        case .mouse: return "Mouse"
        case .layer: return "Layer"
        case .profile: return "Profile"
        case .app: return "Application"
        case .url: return "URL"
        }
    }
}
