//
//  MicroSlot.swift
//  Micropad
//
//  Slot model for Micropad grid
//

import Foundation

class MicroSlot: ObservableObject, Identifiable {
    let id = UUID()
    let index: Int
    let label: String
    let isEncoder: Bool
    
    @Published var sequence: String = ""
    
    var displaySequence: String {
        sequence.isEmpty ? "Drop or click" : (sequence.count > 24 ? String(sequence.prefix(24)) + "…" : sequence)
    }
    
    init(index: Int, label: String, isEncoder: Bool) {
        self.index = index
        self.label = label
        self.isEncoder = isEncoder
    }
}
