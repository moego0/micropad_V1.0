//
//  DevicesViewModel.swift
//  Micropad
//
//  ViewModel for device discovery and connection
//

import Foundation
import Combine
import CoreBluetooth

@MainActor
class DevicesViewModel: ObservableObject {
    @Published var discoveredDevices: [BleDiscoveredDevice] = []
    @Published var isScanning = false
    @Published var isConnected = false
    @Published var connectionStatus = "Not Connected"
    @Published var selectedDevice: BleDiscoveredDevice?
    
    private let bleService = BluetoothService.shared
    private var cancellables = Set<AnyCancellable>()
    
    init() {
        setupSubscriptions()
    }
    
    private func setupSubscriptions() {
        bleService.$discoveredDevices
            .assign(to: &$discoveredDevices)
        
        bleService.$isScanning
            .assign(to: &$isScanning)
        
        bleService.$isConnected
            .assign(to: &$isConnected)
        
        bleService.$connectionStatus
            .assign(to: &$connectionStatus)
    }
    
    func startScan() {
        bleService.startScanning()
    }
    
    func stopScan() {
        bleService.stopScanning()
    }
    
    func connect(to device: BleDiscoveredDevice) {
        selectedDevice = device
        bleService.connect(to: device)
    }
    
    func disconnect() {
        bleService.disconnect()
    }
}
