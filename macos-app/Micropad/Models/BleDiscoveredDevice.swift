//
//  BleDiscoveredDevice.swift
//  Micropad
//
//  BLE device model
//

import Foundation
import CoreBluetooth

struct BleDiscoveredDevice: Identifiable, Hashable {
    let id: UUID
    let name: String
    let rssi: Int
    let peripheral: CBPeripheral?
    
    var displayName: String {
        name.isEmpty ? "Unknown Device" : name
    }
    
    var displayId: String {
        peripheral?.identifier.uuidString ?? id.uuidString
    }
}
