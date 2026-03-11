import { ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useDeviceStore } from '../stores/deviceStore';
import { useProfilesStore } from '../stores/profilesStore';

const nav = [
  { path: '/', label: 'Devices', icon: '📡' },
  { path: '/profiles', label: 'Profiles', icon: '⌨️' },
  { path: '/macros', label: 'Macros', icon: '⚡' },
  { path: '/presets', label: 'Templates', icon: '📋' },
  { path: '/settings', label: 'Settings', icon: '⚙️' }
];

export default function MainLayout({ children }: { children: ReactNode }) {
  const location = useLocation();
  const connectionState = useDeviceStore((s) => s.connectionState);
  const batteryLevel = useDeviceStore((s) => s.batteryLevel);
  const deviceInfo = useDeviceStore((s) => s.deviceInfo);
  const lastSyncTime = useProfilesStore((s) => s.lastSyncTime);

  const isConfigConnected = ['configConnected', 'hidConnected', 'hidReady'].includes(connectionState);
  const isFullyReady = connectionState === 'hidReady';
  const isConnecting = ['requestingAccess', 'reconnectingGrantedDevice', 'connectingGatt'].includes(connectionState);

  const connectionColor = isConfigConnected
    ? isFullyReady ? 'bg-emerald-500' : 'bg-amber-500'
    : connectionState === 'error' ? 'bg-red-500'
    : isConnecting ? 'bg-sky-500 animate-pulse'
    : 'bg-surface-input';

  const connectionLabel = connectionState === 'error'
    ? 'Error'
    : isFullyReady ? 'Connected'
    : isConfigConnected ? 'Config mode'
    : isConnecting ? 'Connecting…'
    : connectionState === 'busyWithOtherHost' ? 'Busy'
    : 'Disconnected';

  return (
    <div className="flex h-screen flex-col bg-surface-primary relative overflow-hidden">
      {/* Ambient gradient */}
      <div className="pointer-events-none absolute inset-0 opacity-40">
        <div className="absolute -top-40 -left-40 h-80 w-80 rounded-full bg-gradient-to-br from-sky-500/30 via-cyan-400/15 to-transparent blur-3xl" />
        <div className="absolute bottom-[-6rem] right-[-4rem] h-96 w-96 rounded-full bg-gradient-to-tr from-indigo-500/20 via-purple-500/15 to-transparent blur-3xl" />
      </div>

      <div className="flex flex-1 min-h-0 relative z-10">
        <aside className="w-56 flex-shrink-0 border-r border-border/80 bg-surface-secondary/90 backdrop-blur flex flex-col">
          <div className="p-4 border-b border-border/60">
            <div className="rounded-lg bg-surface-tertiary px-3 py-2.5">
              <span className="text-sm font-bold text-brand-blue tracking-wide">Micropad</span>
              <span className="text-[10px] text-text-tertiary ml-1.5">by SenetLabs</span>
            </div>
          </div>
          <nav className="p-2 flex-1 space-y-0.5">
            {nav.map(({ path, label }) => (
              <Link
                key={path}
                to={path}
                className={`flex items-center gap-2.5 rounded-lg px-3 py-2.5 text-sm font-medium transition ${
                  location.pathname === path
                    ? 'bg-brand-blue/10 text-brand-blue border-l-2 border-brand-blue'
                    : 'text-text-secondary hover:bg-surface-tertiary hover:text-text-primary'
                }`}
              >
                {label}
              </Link>
            ))}
          </nav>
          <div className="p-3 border-t border-border/60">
            <div className="flex items-center gap-2 text-xs text-text-tertiary">
              <span className={`h-2 w-2 rounded-full flex-shrink-0 ${connectionColor}`} />
              <span className="truncate">{connectionLabel}</span>
            </div>
          </div>
        </aside>

        <main className="flex-1 overflow-auto">
          <div className="h-full w-full animate-fade-in">
            {children}
          </div>
        </main>
      </div>

      <footer className="flex-shrink-0 border-t border-border/60 bg-surface-secondary/90 backdrop-blur px-4 py-2 flex items-center justify-between text-xs text-text-tertiary relative z-10">
        <span>
          {deviceInfo && isConfigConnected ? (
            <>
              <span className="text-text-secondary">{deviceInfo.deviceId}</span>
              {lastSyncTime && <span className="ml-2">Last sync: {lastSyncTime}</span>}
            </>
          ) : (
            <span>Connect a device to begin</span>
          )}
        </span>
        <div className="flex items-center gap-3">
          <span className="inline-flex items-center gap-1.5 rounded-full border border-border bg-surface-tertiary px-2.5 py-0.5">
            <span className={`h-1.5 w-1.5 rounded-full ${connectionColor}`} />
            <span className="text-text-secondary">{connectionLabel}</span>
          </span>
          {batteryLevel != null && isConfigConnected && (
            <span className="text-text-secondary">{batteryLevel}%</span>
          )}
          <span>v1.0.0</span>
        </div>
      </footer>
    </div>
  );
}
