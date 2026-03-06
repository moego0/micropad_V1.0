//
//  StatsView.swift
//  Micropad
//
//  Statistics view
//

import SwiftUI

struct StatsView: View {
    @StateObject private var viewModel = StatsViewModel()
    
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            // Header
            VStack(alignment: .leading, spacing: 8) {
                Text("Statistics & Analytics")
                    .font(.system(size: 24, weight: .semibold))
                Rectangle()
                    .fill(Color.blue)
                    .frame(width: 48, height: 3)
                    .cornerRadius(2)
                Text("Key press and encoder usage from the device")
                    .font(.system(size: 13))
                    .foregroundColor(.secondary)
            }
            .padding()
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(Color(NSColor.controlBackgroundColor))
            
            Divider()
            
            ScrollView {
                VStack(alignment: .leading, spacing: 20) {
                    // Quick stats
                    HStack(spacing: 12) {
                        StatCard(title: "Total key presses", value: "\(viewModel.totalKeypresses)", color: .blue)
                        StatCard(title: "Uptime", value: formatUptime(viewModel.uptimeSeconds), subtitle: "seconds", color: .green)
                        StatCard(title: "Encoders", value: "ENC1: \(viewModel.encoderTurns[0])  ENC2: \(viewModel.encoderTurns[1])", color: .blue)
                    }
                    
                    // Toolbar
                    HStack {
                        Button("Refresh") {
                            Task {
                                await viewModel.refresh()
                            }
                        }
                        .buttonStyle(.borderedProminent)
                        
                        Toggle("Auto-refresh every 5s", isOn: $viewModel.autoRefresh)
                            .onChange(of: viewModel.autoRefresh) { _, enabled in
                                if enabled {
                                    viewModel.startAutoRefresh()
                                } else {
                                    viewModel.stopAutoRefresh()
                                }
                            }
                    }
                    
                    // Key usage
                    VStack(alignment: .leading, spacing: 12) {
                        Text("Key usage (K1–K12)")
                            .font(.headline)
                        
                        Text(viewModel.statusText)
                            .font(.caption)
                            .foregroundColor(.secondary)
                        
                        LazyVGrid(columns: [GridItem(.adaptive(minimum: 80))], spacing: 8) {
                            ForEach(0..<viewModel.keyPressCounts.count, id: \.self) { index in
                                VStack {
                                    Text("K\(index + 1)")
                                        .font(.caption)
                                        .foregroundColor(.secondary)
                                    Text("\(viewModel.keyPressCounts[index])")
                                        .font(.title3)
                                        .fontWeight(.semibold)
                                        .foregroundColor(.blue)
                                }
                                .frame(maxWidth: .infinity)
                                .padding()
                                .background(Color(NSColor.controlBackgroundColor))
                                .cornerRadius(8)
                            }
                        }
                    }
                    .padding()
                    .background(Color(NSColor.controlBackgroundColor))
                    .cornerRadius(12)
                }
                .padding()
            }
        }
        .onAppear {
            Task {
                await viewModel.refresh()
            }
        }
    }
    
    private func formatUptime(_ seconds: Int64) -> String {
        let hours = seconds / 3600
        let minutes = (seconds % 3600) / 60
        if hours > 0 {
            return "\(hours)h \(minutes)m"
        } else if minutes > 0 {
            return "\(minutes)m"
        } else {
            return "\(seconds)s"
        }
    }
}

struct StatCard: View {
    let title: String
    let value: String
    var subtitle: String? = nil
    let color: Color
    
    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(title)
                .font(.caption)
                .foregroundColor(.secondary)
            Text(value)
                .font(.system(size: 28, weight: .semibold))
                .foregroundColor(color)
            if let subtitle = subtitle {
                Text(subtitle)
                    .font(.caption2)
                    .foregroundColor(.secondary)
            }
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding()
        .background(Color(NSColor.controlBackgroundColor))
        .cornerRadius(12)
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(Color(NSColor.separatorColor), lineWidth: 1)
        )
    }
}

#Preview {
    StatsView()
}
