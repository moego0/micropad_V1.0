//
//  SettingsView.swift
//  Micropad
//
//  Settings view
//

import SwiftUI

struct SettingsView: View {
    @StateObject private var viewModel = SettingsViewModel()
    
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            // Header
            VStack(alignment: .leading, spacing: 8) {
                Text("Settings")
                    .font(.system(size: 24, weight: .bold))
                Rectangle()
                    .fill(Color.blue)
                    .frame(width: 48, height: 3)
                    .cornerRadius(2)
            }
            .padding()
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(Color(NSColor.controlBackgroundColor))
            
            Divider()
            
            ScrollView {
                VStack(alignment: .leading, spacing: 20) {
                    // Auto Connect
                    SettingRow(
                        title: "Auto Connect",
                        description: "Automatically connect to the last paired device",
                        isOn: $viewModel.autoConnect
                    ) {
                        viewModel.saveSettings()
                    }
                    
                    // Start with Login
                    SettingRow(
                        title: "Start with Login",
                        description: "Launch Micropad when macOS starts",
                        isOn: $viewModel.startWithLogin
                    ) {
                        viewModel.saveSettings()
                    }
                    
                    // Auto Reconnect
                    SettingRow(
                        title: "Auto Reconnect",
                        description: "Reconnect with exponential backoff when connection is lost",
                        isOn: $viewModel.autoReconnect
                    ) {
                        viewModel.saveSettings()
                    }
                    
                    Divider()
                    
                    // Per-app profile switching
                    VStack(alignment: .leading, spacing: 12) {
                        Text("Per-app profile switching")
                            .font(.headline)
                        
                        Text("When the foreground app changes, switch to the mapped profile (e.g. Code → Profile 1).")
                            .font(.caption)
                            .foregroundColor(.secondary)
                        
                        HStack {
                            TextField("Process name (e.g. Code)", text: $viewModel.newProcessName)
                            TextField("Profile ID", value: $viewModel.newProfileId, format: .number)
                                .frame(width: 80)
                            Button("Add") {
                                viewModel.addProcessMapping()
                            }
                            .buttonStyle(.borderedProminent)
                        }
                        
                        if !viewModel.processProfileMappings.isEmpty {
                            List {
                                ForEach(viewModel.processProfileMappings, id: \.0) { mapping in
                                    HStack {
                                        Text("\(mapping.0) → Profile \(mapping.1)")
                                        Spacer()
                                        Button("Remove") {
                                            viewModel.removeProcessMapping(mapping.0)
                                        }
                                        .buttonStyle(.borderless)
                                        .foregroundColor(.red)
                                    }
                                }
                            }
                            .frame(height: 200)
                        }
                    }
                    .padding()
                    .background(Color(NSColor.controlBackgroundColor))
                    .cornerRadius(8)
                }
                .padding()
            }
        }
    }
}

struct SettingRow: View {
    let title: String
    let description: String
    @Binding var isOn: Bool
    let onChanged: () -> Void
    
    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text(title)
                    .font(.headline)
                Text(description)
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
            Spacer()
            Toggle("", isOn: $isOn)
                .onChange(of: isOn) { _, _ in
                    onChanged()
                }
        }
        .padding()
        .background(Color(NSColor.controlBackgroundColor))
        .cornerRadius(8)
    }
}

#Preview {
    SettingsView()
}
