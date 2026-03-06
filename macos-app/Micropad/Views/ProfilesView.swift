//
//  ProfilesView.swift
//  Micropad
//
//  Profiles management view
//

import SwiftUI
import AppKit

struct ProfilesView: View {
    @StateObject private var viewModel = ProfilesViewModel()
    
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            // Header
            VStack(alignment: .leading, spacing: 8) {
                Text("Profiles")
                    .font(.system(size: 24, weight: .bold))
                Rectangle()
                    .fill(Color.blue)
                    .frame(width: 48, height: 3)
                    .cornerRadius(2)
                Text("Manage your device profiles and key assignments")
                    .font(.system(size: 12))
                    .foregroundColor(.secondary)
            }
            .padding()
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(Color(NSColor.controlBackgroundColor))
            
            Divider()
            
            // Content
            HSplitView {
                // Profile list
                VStack(alignment: .leading, spacing: 12) {
                    Button("Refresh") {
                        Task {
                            await viewModel.refreshProfiles()
                        }
                    }
                    .buttonStyle(.borderedProminent)
                    .frame(maxWidth: .infinity)
                    
                    // Create new profile
                    VStack(alignment: .leading, spacing: 8) {
                        Text("Create Profile")
                            .font(.caption)
                            .foregroundColor(.secondary)
                        TextField("Profile name", text: $viewModel.newProfileName)
                            .textFieldStyle(.roundedBorder)
                        HStack {
                            Text("ID:")
                                .font(.caption)
                            TextField("ID", value: $viewModel.newProfileId, format: .number)
                                .textFieldStyle(.roundedBorder)
                                .frame(width: 60)
                            Button("Create") {
                                viewModel.createNewProfile()
                            }
                            .buttonStyle(.bordered)
                        }
                    }
                    .padding(8)
                    .background(Color(NSColor.controlBackgroundColor).opacity(0.5))
                    .cornerRadius(6)
                    
                    HStack {
                        Button("Import") {
                            viewModel.importProfile()
                        }
                        .buttonStyle(.bordered)
                        
                        Button("Export") {
                            if let profile = viewModel.selectedProfile {
                                viewModel.exportProfile(profile)
                            }
                        }
                        .buttonStyle(.bordered)
                        .disabled(viewModel.selectedProfile == nil)
                    }
                    
                    List(viewModel.profiles, selection: $viewModel.selectedProfile) { profile in
                        HStack {
                            VStack(alignment: .leading, spacing: 2) {
                                Text(profile.name)
                                    .font(.headline)
                                Text("ID: \(profile.id)")
                                    .font(.caption)
                                    .foregroundColor(.secondary)
                            }
                            Spacer()
                            Button(action: {
                                viewModel.deleteProfile(profile)
                            }) {
                                Image(systemName: "trash")
                                    .foregroundColor(.red)
                            }
                            .buttonStyle(.borderless)
                        }
                        .tag(profile)
                    }
                    .listStyle(.sidebar)
                }
                .frame(width: 260)
                .padding()
                .background(Color(NSColor.controlBackgroundColor))
                
                // Profile editor
                ScrollView {
                    VStack(alignment: .leading, spacing: 16) {
                        if let profile = viewModel.selectedProfile {
                            // Profile name editor
                            HStack {
                                Text("Profile Name:")
                                TextField("Profile name", text: Binding(
                                    get: { viewModel.selectedProfile?.name ?? "" },
                                    set: { viewModel.updateProfileName($0) }
                                ))
                                .textFieldStyle(.roundedBorder)
                            }
                            .padding()
                            .background(Color(NSColor.controlBackgroundColor))
                            .cornerRadius(8)
                            
                            // Key grid (3x4)
                            VStack(alignment: .leading, spacing: 12) {
                                Text("Keys (K1-K12)")
                                    .font(.headline)
                                
                                Grid(alignment: .center, horizontalSpacing: 8, verticalSpacing: 8) {
                                    ForEach(0..<3) { row in
                                        GridRow {
                                            ForEach(0..<4) { col in
                                                let index = row * 4 + col
                                                if index < viewModel.keySlots.count {
                                                    KeySlotView(keyConfig: viewModel.keySlots[index])
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            .padding()
                            .background(Color(NSColor.controlBackgroundColor))
                            .cornerRadius(8)
                            
                            // Encoders
                            VStack(alignment: .leading, spacing: 12) {
                                Text("Encoders")
                                    .font(.headline)
                                
                                HStack(spacing: 16) {
                                    ForEach(viewModel.encoderSlots) { encoder in
                                        EncoderSlotView(encoder: encoder)
                                    }
                                }
                            }
                            .padding()
                            .background(Color(NSColor.controlBackgroundColor))
                            .cornerRadius(8)
                            
                            // Status
                            Text(viewModel.statusText)
                                .font(.caption)
                                .foregroundColor(.secondary)
                            
                            HStack {
                                Button("Save Profile") {
                                    Task {
                                        await viewModel.saveProfile()
                                    }
                                }
                                .buttonStyle(.borderedProminent)
                                
                                Text("Macros are saved automatically in the Macros tab")
                                    .font(.caption)
                                    .foregroundColor(.secondary)
                            }
                        } else {
                            Text("Select a profile to edit")
                                .foregroundColor(.secondary)
                                .frame(maxWidth: .infinity, maxHeight: .infinity)
                        }
                    }
                    .padding()
                }
            }
        }
        .onChange(of: viewModel.selectedProfile) { _, newProfile in
            if let profile = newProfile {
                viewModel.selectProfile(profile)
            }
        }
    }
}

struct KeySlotView: View {
    let keyConfig: KeyConfig
    
    var body: some View {
        Button(action: {
            // TODO: Open key configuration dialog
        }) {
            VStack(spacing: 4) {
                Text("K\(keyConfig.index + 1)")
                    .font(.system(size: 10))
                    .foregroundColor(.secondary)
                Text(keyConfig.displayName)
                    .font(.system(size: 9))
                    .lineLimit(2)
                    .multilineTextAlignment(.center)
            }
            .frame(width: 100, height: 80)
            .padding(8)
            .background(Color(NSColor.controlBackgroundColor))
            .cornerRadius(12)
            .overlay(
                RoundedRectangle(cornerRadius: 12)
                    .stroke(Color(NSColor.separatorColor), lineWidth: 1)
            )
        }
        .buttonStyle(.plain)
    }
}

struct EncoderSlotView: View {
    let encoder: EncoderConfig
    
    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("E\(encoder.index + 1)")
                .font(.headline)
            Text("Acceleration: \(encoder.acceleration ? "On" : "Off")")
                .font(.caption)
            Text("Steps/Detent: \(encoder.stepsPerDetent)")
                .font(.caption)
        }
        .padding()
        .frame(width: 150)
        .background(Color(NSColor.controlBackgroundColor))
        .cornerRadius(8)
        .overlay(
            RoundedRectangle(cornerRadius: 8)
                .stroke(Color(NSColor.separatorColor), lineWidth: 1)
        )
    }
}

#Preview {
    ProfilesView()
}
