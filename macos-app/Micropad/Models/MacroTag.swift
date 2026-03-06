//
//  MacroTag.swift
//  Micropad
//
//  Macro tag model
//

import Foundation

struct MacroTag: Identifiable, Hashable {
    let id = UUID()
    let tag: String
    let display: String
    let group: String
}

struct MacroTagCatalog {
    static let dataFormat = "Micropad.MacroTag"
    
    static func getAll() -> [MacroTag] {
        var tags: [MacroTag] = []
        
        // Modifiers
        let modifiers = ["{CTRL}", "{RCTRL}", "{ALT}", "{RALT}", "{SHIFT}", "{RSHIFT}", "{LWIN}", "{RWIN}", "{APPS}"]
        tags.append(contentsOf: modifiers.map { MacroTag(tag: $0, display: String($0.dropFirst().dropLast()), group: "Modifiers") })
        
        // Extended
        let extended = ["{DEL}", "{INS}", "{PGUP}", "{PGDN}", "{HOME}", "{END}", "{RETURN}", "{ESCAPE}", "{BACKSPACE}", "{TAB}", "{PRTSCN}", "{PAUSE}", "{SPACE}", "{CAPSLOCK}", "{NUMLOCK}", "{SCROLLLOCK}", "{BREAK}", "{CTRLBREAK}"]
        tags.append(contentsOf: extended.map { MacroTag(tag: $0, display: String($0.dropFirst().dropLast()), group: "Extended") })
        
        // Direction
        let direction = ["{UP}", "{DOWN}", "{LEFT}", "{RIGHT}"]
        tags.append(contentsOf: direction.map { MacroTag(tag: $0, display: String($0.dropFirst().dropLast()), group: "Direction") })
        
        // Function F1-F24
        for i in 1...24 {
            tags.append(MacroTag(tag: "{F\(i)}", display: "F\(i)", group: "Function"))
        }
        
        // Volume / Media
        let media = ["{VOL+}", "{VOL-}", "{MUTE}", "{MEDIAPLAY}", "{MEDIASTOP}", "{MEDIANEXT}", "{MEDIAPREV}"]
        tags.append(contentsOf: media.map { MacroTag(tag: $0, display: String($0.dropFirst().dropLast()), group: "Volume/Media") })
        
        // Mouse
        let mouse = ["{LMB}", "{RMB}", "{MMB}", "{MB4/XMB1}", "{MB5/XMB2}", "{LMBD}", "{LMBU}", "{RMBD}", "{RMBU}", "{MWUP}", "{MWDN}", "{TILTL}", "{TILTR}"]
        tags.append(contentsOf: mouse.map { MacroTag(tag: $0, display: String($0.dropFirst().dropLast()), group: "Mouse") })
        
        // NumPad
        let numpad = ["{NUM0}", "{NUM1}", "{NUM2}", "{NUM3}", "{NUM4}", "{NUM5}", "{NUM6}", "{NUM7}", "{NUM8}", "{NUM9}", "{NUM+}", "{NUM-}", "{NUM.}", "{NUM/}", "{NUM*}", "{NUMENTER}"]
        tags.append(contentsOf: numpad.map { MacroTag(tag: $0, display: $0.replacingOccurrences(of: "NUM", with: "N"), group: "NumPad") })
        
        // Letters A-Z
        for c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ" {
            tags.append(MacroTag(tag: String(c), display: String(c), group: "Letters"))
        }
        for c in "abcdefghijklmnopqrstuvwxyz" {
            tags.append(MacroTag(tag: String(c), display: String(c), group: "Letters"))
        }
        
        // Numbers and symbols
        let keys = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", " ", "-", "=", "[", "]", "\\", ";", "'", "`", ",", ".", "/"]
        tags.append(contentsOf: keys.map { MacroTag(tag: $0, display: $0 == " " ? "Space" : $0, group: "Keys") })
        
        // Special
        let special = ["{WAIT:1}", "{WAITMS:100}", "{HOLD:1}", "{HOLDMS:50}", "{CLEAR}", "{PRESS}", "{RELEASE}", "{OD}", "{OU}", "{OR}"]
        tags.append(contentsOf: special.map { MacroTag(tag: $0, display: String($0.dropFirst().dropLast()), group: "Special") })
        
        // Toggle
        let toggle = ["{NUMLOCKON}", "{NUMLOCKOFF}", "{CAPSLOCKON}", "{CAPSLOCKOFF}", "{SCROLLLOCKON}", "{SCROLLLOCKOFF}"]
        tags.append(contentsOf: toggle.map { MacroTag(tag: $0, display: String($0.dropFirst().dropLast()), group: "Toggle") })
        
        // Web
        let web = ["{BACK}", "{FORWARD}", "{STOP}", "{REFRESH}", "{WEBHOME}", "{SEARCH}", "{FAVORITES}"]
        tags.append(contentsOf: web.map { MacroTag(tag: $0, display: String($0.dropFirst().dropLast()), group: "Web") })
        
        return tags
    }
}
