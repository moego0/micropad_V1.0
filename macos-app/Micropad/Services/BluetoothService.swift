//
//  BluetoothService.swift
//  Micropad
//
//  Core Bluetooth service for BLE communication
//

import Foundation
import Combine
import CoreBluetooth

class BluetoothService: NSObject, ObservableObject {
    static let shared = BluetoothService()
    
    // Config Service UUID (same as Windows version)
    private let configServiceUUID = CBUUID(string: "4fafc201-1fb5-459e-8fcc-c5c9c331914b")
    
    @Published var discoveredDevices: [BleDiscoveredDevice] = []
    @Published var isScanning = false
    @Published var isConnected = false
    @Published var connectionStatus = "Not Connected"
    
    private var centralManager: CBCentralManager!
    private var connectedPeripheral: CBPeripheral?
    
    override init() {
        super.init()
        centralManager = CBCentralManager(delegate: self, queue: nil)
    }
    
    func startScanning() {
        guard centralManager.state == .poweredOn else {
            connectionStatus = "Bluetooth is not available"
            return
        }
        
        isScanning = true
        discoveredDevices = []
        connectionStatus = "Scanning..."
        
        // Scan for devices with the config service UUID
        centralManager.scanForPeripherals(withServices: [configServiceUUID], options: [CBCentralManagerScanOptionAllowDuplicatesKey: false])
    }
    
    func stopScanning() {
        centralManager.stopScan()
        isScanning = false
        connectionStatus = "Scan stopped"
    }
    
    func connect(to device: BleDiscoveredDevice) {
        guard let peripheral = device.peripheral else { return }
        stopScanning()
        connectedPeripheral = peripheral
        centralManager.connect(peripheral, options: nil)
        connectionStatus = "Connecting..."
    }
    
    func disconnect() {
        if let peripheral = connectedPeripheral {
            centralManager.cancelPeripheralConnection(peripheral)
        }
        connectedPeripheral = nil
        isConnected = false
        connectionStatus = "Disconnected"
    }
}

extension BluetoothService: CBCentralManagerDelegate {
    func centralManagerDidUpdateState(_ central: CBCentralManager) {
        switch central.state {
        case .poweredOn:
            connectionStatus = "Ready"
        case .poweredOff:
            connectionStatus = "Bluetooth is off"
            isScanning = false
        case .unauthorized:
            connectionStatus = "Bluetooth unauthorized"
        case .unsupported:
            connectionStatus = "Bluetooth unsupported"
        case .resetting:
            connectionStatus = "Bluetooth resetting"
        case .unknown:
            connectionStatus = "Bluetooth state unknown"
        @unknown default:
            connectionStatus = "Bluetooth state unknown"
        }
    }
    
    func centralManager(_ central: CBCentralManager, didDiscover peripheral: CBPeripheral, advertisementData: [String : Any], rssi RSSI: NSNumber) {
        let rssiValue = RSSI.intValue
        
        // Check if device already exists
        if let existingIndex = discoveredDevices.firstIndex(where: { $0.peripheral?.identifier == peripheral.identifier }) {
            // Update RSSI
            discoveredDevices[existingIndex] = BleDiscoveredDevice(
                id: peripheral.identifier,
                name: peripheral.name ?? "",
                rssi: rssiValue,
                peripheral: peripheral
            )
        } else {
            // Add new device
            let device = BleDiscoveredDevice(
                id: peripheral.identifier,
                name: peripheral.name ?? "",
                rssi: rssiValue,
                peripheral: peripheral
            )
            discoveredDevices.append(device)
        }
    }
    
    func centralManager(_ central: CBCentralManager, didConnect peripheral: CBPeripheral) {
        isConnected = true
        connectionStatus = "Connected to \(peripheral.name ?? "Device")"
        peripheral.delegate = self
        peripheral.discoverServices([configServiceUUID])
    }
    
    func centralManager(_ central: CBCentralManager, didFailToConnect peripheral: CBPeripheral, error: Error?) {
        isConnected = false
        connectionStatus = "Failed to connect: \(error?.localizedDescription ?? "Unknown error")"
        connectedPeripheral = nil
    }
    
    func centralManager(_ central: CBCentralManager, didDisconnectPeripheral peripheral: CBPeripheral, error: Error?) {
        isConnected = false
        connectionStatus = "Disconnected"
        connectedPeripheral = nil
    }
}

extension BluetoothService: CBPeripheralDelegate {
    func peripheral(_ peripheral: CBPeripheral, didDiscoverServices error: Error?) {
        guard let services = peripheral.services else { return }
        for service in services {
            if service.uuid == configServiceUUID {
                peripheral.discoverCharacteristics(nil, for: service)
            }
        }
    }
    
    func peripheral(_ peripheral: CBPeripheral, didDiscoverCharacteristicsFor service: CBService, error: Error?) {
        // Characteristics discovered - ready for communication
        connectionStatus = "Ready for communication"
    }
}
