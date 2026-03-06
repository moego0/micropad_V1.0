//
//  MainView.swift
//  Micropad
//
//  Main window with sidebar navigation
//

import SwiftUI

struct MainView: View {
    @EnvironmentObject var appState: AppState
    
    var body: some View {
        HSplitView {
            // Sidebar
            SidebarView()
                .frame(minWidth: 200, idealWidth: 240)
            
            // Main content
            Group {
                switch appState.selectedTab {
                case .devices:
                    DevicesView()
                case .profiles:
                    ProfilesView()
                case .macros:
                    MacrosView()
                case .stats:
                    StatsView()
                case .settings:
                    SettingsView()
                }
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity)
        }
    }
}

struct SidebarView: View {
    @EnvironmentObject var appState: AppState
    
    var body: some View {
        VStack(spacing: 0) {
            // Logo
            HStack {
                Image(systemName: "keyboard")
                    .font(.system(size: 24))
                    .foregroundColor(.blue)
                Text("Micropad")
                    .font(.title2)
                    .fontWeight(.bold)
            }
            .padding()
            .frame(maxWidth: .infinity)
            .background(Color(NSColor.controlBackgroundColor))
            
            Divider()
            
            // Navigation
            List(AppState.Tab.allCases, id: \.self, selection: $appState.selectedTab) { tab in
                NavigationLink(value: tab) {
                    Label(tab.rawValue, systemImage: iconForTab(tab))
                }
            }
            .listStyle(.sidebar)
            
            Spacer()
            
            // Status bar
            VStack(alignment: .leading, spacing: 4) {
                Divider()
                HStack {
                    Image(systemName: "circle.fill")
                        .foregroundColor(.gray)
                        .font(.system(size: 8))
                    Text("Not Connected")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
                .padding(.horizontal, 12)
                .padding(.vertical, 8)
            }
            .background(Color(NSColor.controlBackgroundColor))
        }
    }
    
    private func iconForTab(_ tab: AppState.Tab) -> String {
        switch tab {
        case .devices: return "antenna.radiowaves.left.and.right"
        case .profiles: return "square.grid.3x3"
        case .macros: return "keyboard"
        case .stats: return "chart.bar"
        case .settings: return "gearshape"
        }
    }
}

#Preview {
    MainView()
        .environmentObject(AppState())
}
