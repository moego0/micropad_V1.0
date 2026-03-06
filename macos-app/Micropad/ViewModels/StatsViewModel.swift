//
//  StatsViewModel.swift
//  Micropad
//
//  ViewModel for statistics
//

import Foundation
import Combine

@MainActor
class StatsViewModel: ObservableObject {
    @Published var statusText = "Connect to device and click Refresh."
    @Published var uptimeSeconds: Int64 = 0
    @Published var keyPressCounts: [Int] = Array(repeating: 0, count: 12)
    @Published var encoderTurns: [Int] = [0, 0]
    @Published var autoRefresh = true
    
    var totalKeypresses: Int {
        keyPressCounts.reduce(0, +)
    }
    
    private let protocolHandler = ProtocolHandler.shared
    private var refreshTimer: Timer?
    
    init() {
        if autoRefresh {
            startAutoRefresh()
        }
    }
    
    deinit {
        stopAutoRefresh()
    }
    
    func refresh() async {
        do {
            guard let payload = try await protocolHandler.getStats() else {
                statusText = "No stats (device may not support getStats)."
                return
            }
            
            uptimeSeconds = payload["uptime"] as? Int64 ?? 0
            
            if let keyPresses = payload["keyPresses"] as? [Int] {
                keyPressCounts = keyPresses
                while keyPressCounts.count < 12 {
                    keyPressCounts.append(0)
                }
            }
            
            if let encoderTurnsArray = payload["encoderTurns"] as? [Int] {
                encoderTurns = encoderTurnsArray
                while encoderTurns.count < 2 {
                    encoderTurns.append(0)
                }
            }
            
            statusText = "Total key presses: \(totalKeypresses) | Uptime: \(formatUptime(uptimeSeconds))"
        } catch {
            statusText = "Error: \(error.localizedDescription)"
        }
    }
    
    func startAutoRefresh() {
        stopAutoRefresh()
        refreshTimer = Timer.scheduledTimer(withTimeInterval: 5.0, repeats: true) { [weak self] _ in
            Task { @MainActor in
                await self?.refresh()
            }
        }
    }
    
    func stopAutoRefresh() {
        refreshTimer?.invalidate()
        refreshTimer = nil
    }
    
    private func formatUptime(_ seconds: Int64) -> String {
        let hours = seconds / 3600
        let minutes = (seconds % 3600) / 60
        let secs = seconds % 60
        
        if hours > 0 {
            return "\(hours)h \(minutes)m"
        } else if minutes > 0 {
            return "\(minutes)m \(secs)s"
        } else {
            return "\(secs)s"
        }
    }
}
