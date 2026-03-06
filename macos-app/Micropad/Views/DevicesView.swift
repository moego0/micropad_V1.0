//
//  DevicesView.swift
//  Micropad
//
//  Device discovery and connection view
//

import SwiftUI

struct DevicesView: View {
    @StateObject private var viewModel = DevicesViewModel()
    
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            // Header
            VStack(alignment: .leading, spacing: 8) {
                Text("Devices")
                    .font(.system(size: 24, weight: .semibold))
                Rectangle()
                    .fill(Color.blue)
                    .frame(width: 48, height: 3)
                    .cornerRadius(2)
                Text("Discover and connect to your Micropad device")
                    .font(.system(size: 12))
                    .foregroundColor(.secondary)
            }
            .padding()
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(Color(NSColor.controlBackgroundColor))
            
            Divider()
            
            // Content
            VStack(spacing: 16) {
                // Connection status
                HStack {
                    Circle()
                        .fill(viewModel.isConnected ? Color.green : Color.gray)
                        .frame(width: 12, height: 12)
                    Text(viewModel.connectionStatus)
                        .font(.headline)
                }
                .padding()
                
                // Scan button
                HStack {
                    Button(action: {
                        if viewModel.isScanning {
                            viewModel.stopScan()
                        } else {
                            viewModel.startScan()
                        }
                    }) {
                        Label(viewModel.isScanning ? "Stop Scan" : "Start Scan", systemImage: viewModel.isScanning ? "stop.circle.fill" : "magnifyingglass")
                    }
                    .buttonStyle(.borderedProminent)
                    
                    if viewModel.isConnected {
                        Button("Disconnect") {
                            viewModel.disconnect()
                        }
                        .buttonStyle(.bordered)
                    }
                }
                .padding()
                
                // Device list
                if viewModel.discoveredDevices.isEmpty {
                    VStack(spacing: 8) {
                        Image(systemName: "antenna.radiowaves.left.and.right")
                            .font(.system(size: 48))
                            .foregroundColor(.secondary)
                        Text("No devices found")
                            .font(.headline)
                            .foregroundColor(.secondary)
                        Text("Click 'Start Scan' to discover Micropad devices")
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
                } else {
                    List(viewModel.discoveredDevices) { device in
                        DeviceRow(device: device, viewModel: viewModel)
                    }
                }
            }
            .padding()
        }
    }
}

struct DeviceRow: View {
    let device: BleDiscoveredDevice
    @ObservedObject var viewModel: DevicesViewModel
    
    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text(device.displayName)
                    .font(.headline)
                Text(device.displayId)
                    .font(.caption)
                    .foregroundColor(.secondary)
                Text("RSSI: \(device.rssi) dBm")
                    .font(.caption2)
                    .foregroundColor(.secondary)
            }
            Spacer()
            Button("Connect") {
                viewModel.connect(to: device)
            }
            .buttonStyle(.borderedProminent)
        }
        .padding(.vertical, 4)
    }
}

#Preview {
    DevicesView()
}
