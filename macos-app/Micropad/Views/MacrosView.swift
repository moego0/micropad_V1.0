//
//  MacrosView.swift
//  Micropad
//
//  Macro/shortcut builder view
//

import SwiftUI
import AppKit

struct MacrosView: View {
    @StateObject private var viewModel = MacrosViewModel()
    @State private var showCreateProfileDialog = false
    @State private var newProfileName = ""
    @State private var newProfileId = 0
    
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            // Profile selector header
            HStack {
                Text("Profile:")
                    .font(.headline)
                Picker("", selection: Binding(
                    get: { viewModel.selectedProfile },
                    set: { if let profile = $0 { viewModel.selectProfile(profile) } }
                )) {
                    ForEach(viewModel.availableProfiles) { profile in
                        Text("\(profile.name) (ID: \(profile.id))").tag(profile as Profile?)
                    }
                }
                .frame(width: 200)
                
                Button("New Profile") {
                    newProfileId = viewModel.availableProfiles.count
                    showCreateProfileDialog = true
                }
                .buttonStyle(.bordered)
                
                if viewModel.selectedProfile != nil {
                    TextField("Profile name", text: Binding(
                        get: { viewModel.selectedProfile?.name ?? "" },
                        set: { viewModel.updateProfileName($0) }
                    ))
                    .frame(width: 150)
                }
            }
            .padding()
            .background(Color(NSColor.controlBackgroundColor))
            
            Divider()
            
            HSplitView {
                // Left: Micropad grid
                VStack(alignment: .leading, spacing: 12) {
                    Text("Micropad grid")
                        .font(.headline)
                    
                    // Grid (4x4 layout: E1,K1-K3 | K4-K7 | K8-K11 | K12,E2)
                    Grid(alignment: .center, horizontalSpacing: 6, verticalSpacing: 6) {
                        GridRow {
                            SlotView(slot: viewModel.slots[12], index: 12, viewModel: viewModel) // E1
                            SlotView(slot: viewModel.slots[0], index: 0, viewModel: viewModel)  // K1
                            SlotView(slot: viewModel.slots[1], index: 1, viewModel: viewModel)  // K2
                            SlotView(slot: viewModel.slots[2], index: 2, viewModel: viewModel)  // K3
                        }
                        GridRow {
                            SlotView(slot: viewModel.slots[3], index: 3, viewModel: viewModel)  // K4
                            SlotView(slot: viewModel.slots[4], index: 4, viewModel: viewModel)  // K5
                            SlotView(slot: viewModel.slots[5], index: 5, viewModel: viewModel)  // K6
                            SlotView(slot: viewModel.slots[6], index: 6, viewModel: viewModel)  // K7
                        }
                        GridRow {
                            SlotView(slot: viewModel.slots[7], index: 7, viewModel: viewModel)  // K8
                            SlotView(slot: viewModel.slots[8], index: 8, viewModel: viewModel)  // K9
                            SlotView(slot: viewModel.slots[9], index: 9, viewModel: viewModel)  // K10
                            SlotView(slot: viewModel.slots[10], index: 10, viewModel: viewModel) // K11
                        }
                        GridRow {
                            SlotView(slot: viewModel.slots[11], index: 11, viewModel: viewModel) // K12
                            SlotView(slot: viewModel.slots[13], index: 13, viewModel: viewModel) // E2
                            Color.clear.gridCellUnsizedAxes([.horizontal, .vertical])
                            Color.clear.gridCellUnsizedAxes([.horizontal, .vertical])
                        }
                    }
                    .padding(8)
                    
                    // Selected slot sequence editor
                    VStack(alignment: .leading, spacing: 4) {
                        Text("Selected slot sequence")
                            .font(.caption)
                            .foregroundColor(.secondary)
                        TextEditor(text: Binding(
                            get: { viewModel.selectedSequence },
                            set: { viewModel.updateSelectedSequence($0) }
                        ))
                        .font(.system(.body, design: .monospaced))
                        .frame(height: 56)
                        .padding(4)
                        .background(Color(NSColor.controlBackgroundColor))
                        .cornerRadius(4)
                        .overlay(
                            RoundedRectangle(cornerRadius: 4)
                                .stroke(Color(NSColor.separatorColor), lineWidth: 1)
                        )
                    }
                }
                .padding()
                .frame(width: 300)
                .background(Color(NSColor.controlBackgroundColor))
                
                // Right: Quick actions + Tag palette
                ScrollView {
                    VStack(alignment: .leading, spacing: 16) {
                        // Quick Actions
                        VStack(alignment: .leading, spacing: 12) {
                            Text("Quick Actions")
                                .font(.headline)
                            
                            // URL Input
                            VStack(alignment: .leading, spacing: 4) {
                                Text("Open URL")
                                    .font(.caption)
                                    .foregroundColor(.secondary)
                                HStack {
                                    TextField("https://example.com", text: $viewModel.urlInput)
                                    Picker("", selection: $viewModel.selectedBrowser) {
                                        ForEach(viewModel.browsers, id: \.self) { browser in
                                            Text(browser).tag(browser)
                                        }
                                    }
                                    .frame(width: 100)
                                    Button("Add URL") {
                                        viewModel.insertUrl()
                                    }
                                    .buttonStyle(.borderedProminent)
                                }
                            }
                            
                            // Application Input
                            VStack(alignment: .leading, spacing: 4) {
                                Text("Launch Application")
                                    .font(.caption)
                                    .foregroundColor(.secondary)
                                HStack {
                                    TextField("/Applications/App.app", text: $viewModel.applicationPathInput)
                                    Button("Browse...") {
                                        selectApplication()
                                    }
                                    .buttonStyle(.bordered)
                                    Button("Add App") {
                                        viewModel.insertApplication()
                                    }
                                    .buttonStyle(.borderedProminent)
                                }
                            }
                        }
                        .padding()
                        .background(Color(NSColor.controlBackgroundColor))
                        .cornerRadius(8)
                        
                        // Tag palette
                        VStack(alignment: .leading, spacing: 8) {
                            Text("Click a tag to append to selected slot, or drag onto a grid cell")
                                .font(.caption)
                                .foregroundColor(.secondary)
                            
                            LazyVGrid(columns: [GridItem(.adaptive(minimum: 80))], spacing: 8) {
                                ForEach(viewModel.allTags) { tag in
                                    TagChip(tag: tag, viewModel: viewModel)
                                }
                            }
                        }
                    }
                    .padding()
                }
            }
        }
        .sheet(isPresented: $showCreateProfileDialog) {
            CreateProfileDialog(
                profileName: $newProfileName,
                profileId: $newProfileId,
                onCreate: {
                    viewModel.createNewProfile(name: newProfileName.isEmpty ? "Profile \(newProfileId)" : newProfileName, id: newProfileId)
                    showCreateProfileDialog = false
                    newProfileName = ""
                    newProfileId = viewModel.availableProfiles.count
                },
                onCancel: {
                    showCreateProfileDialog = false
                }
            )
        }
        .onAppear {
            viewModel.refreshProfiles()
        }
    }
    
    private func selectApplication() {
        let panel = NSOpenPanel()
        panel.allowsMultipleSelection = false
        panel.canChooseDirectories = false
        panel.canChooseFiles = true
        if #available(macOS 11.0, *) {
            panel.allowedContentTypes = [.application]
        }
        panel.directoryURL = URL(fileURLWithPath: "/Applications")
        
        if panel.runModal() == .OK {
            if let url = panel.url {
                viewModel.applicationPathInput = url.path
            }
        }
    }
}

struct SlotView: View {
    @ObservedObject var slot: MicroSlot
    let index: Int
    @ObservedObject var viewModel: MacrosViewModel
    
    var isSelected: Bool {
        viewModel.selectedSlotIndex == index
    }
    
    var body: some View {
        Button(action: {
            viewModel.selectSlot(index)
        }) {
            VStack(spacing: 4) {
                Text(slot.label)
                    .font(.system(size: 9))
                    .foregroundColor(.secondary)
                Text(slot.displaySequence)
                    .font(.system(size: 8))
                    .lineLimit(2)
                    .multilineTextAlignment(.center)
                    .foregroundColor(.primary)
            }
            .frame(maxWidth: .infinity)
            .frame(height: 64)
            .padding(4)
            .background(isSelected ? Color.blue.opacity(0.3) : Color(NSColor.controlBackgroundColor))
            .overlay(
                RoundedRectangle(cornerRadius: 6)
                    .stroke(isSelected ? Color.blue : Color(NSColor.separatorColor), lineWidth: isSelected ? 2 : 1)
            )
            .cornerRadius(6)
        }
        .buttonStyle(.plain)
        .onDrop(of: [.text], delegate: SlotDropDelegate(slotIndex: index, viewModel: viewModel))
    }
}

struct TagChip: View {
    let tag: MacroTag
    @ObservedObject var viewModel: MacrosViewModel
    
    var body: some View {
        Button(action: {
            viewModel.appendTag(tag.tag)
        }) {
            Text(tag.display)
                .font(.system(size: 11))
                .padding(.horizontal, 6)
                .padding(.vertical, 4)
                .background(Color(NSColor.controlBackgroundColor))
                .cornerRadius(4)
                .overlay(
                    RoundedRectangle(cornerRadius: 4)
                        .stroke(Color(NSColor.separatorColor), lineWidth: 1)
                )
        }
        .buttonStyle(.plain)
        .help(tag.tag)
        .onDrag {
            let provider = NSItemProvider()
            provider.registerDataRepresentation(forTypeIdentifier: "public.text", visibility: .all) { completion in
                completion(tag.tag.data(using: .utf8), nil)
                return nil
            }
            return provider
        }
    }
}

struct SlotDropDelegate: DropDelegate {
    let slotIndex: Int
    @ObservedObject var viewModel: MacrosViewModel
    
    func validateDrop(info: DropInfo) -> Bool {
        return info.hasItemsConforming(to: [.text])
    }
    
    func performDrop(info: DropInfo) -> Bool {
        guard let itemProvider = info.itemProviders(for: [.text]).first else { return false }
        itemProvider.loadItem(forTypeIdentifier: "public.text", options: nil) { data, error in
            if let data = data as? Data, let tag = String(data: data, encoding: .utf8) {
                Task { @MainActor in
                    viewModel.dropTagOnSlot(slotIndex, tag: tag)
                }
            } else if let string = data as? String {
                Task { @MainActor in
                    viewModel.dropTagOnSlot(slotIndex, tag: string)
                }
            }
        }
        return true
    }
}

struct CreateProfileDialog: View {
    @Binding var profileName: String
    @Binding var profileId: Int
    let onCreate: () -> Void
    let onCancel: () -> Void
    
    var body: some View {
        VStack(spacing: 20) {
            Text("Create New Profile")
                .font(.headline)
            
            VStack(alignment: .leading, spacing: 8) {
                Text("Profile Name:")
                TextField("Enter profile name", text: $profileName)
                    .textFieldStyle(.roundedBorder)
            }
            
            VStack(alignment: .leading, spacing: 8) {
                Text("Profile ID:")
                TextField("Profile ID", value: $profileId, format: .number)
                    .textFieldStyle(.roundedBorder)
            }
            
            HStack {
                Button("Cancel", action: onCancel)
                    .buttonStyle(.bordered)
                Button("Create", action: onCreate)
                    .buttonStyle(.borderedProminent)
            }
        }
        .padding()
        .frame(width: 300)
    }
}

#Preview {
    MacrosView()
}
