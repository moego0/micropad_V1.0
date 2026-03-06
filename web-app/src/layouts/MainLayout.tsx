import { ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useDeviceStore } from '../stores/deviceStore';

const nav = [
  { path: '/', label: 'Devices' },
  { path: '/profiles', label: 'Profiles' },
  { path: '/presets', label: 'Templates' },
  { path: '/macros', label: 'Macros' },
  { path: '/settings', label: 'Settings' }
];

export default function MainLayout({ children }: { children: ReactNode }) {
  const location = useLocation();
  const connectionState = useDeviceStore((s) => s.connectionState);
  const batteryLevel = useDeviceStore((s) => s.batteryLevel);
  const deviceName = useDeviceStore((s) => s.deviceName);

  const connectionColor =
    connectionState === 'ready' ? 'bg-green-500' : connectionState === 'error' ? 'bg-red-500' : 'bg-surface-input';
  const connectionLabel =
    connectionState === 'ready' ? 'Connected' : connectionState === 'error' ? 'Error' : connectionState === 'connecting' || connectionState === 'pairing' ? 'Connecting…' : 'Disconnected';

  return (
    <div className="flex h-screen flex-col bg-surface-primary relative overflow-hidden">
      {/* ambient gradient backdrop */}
      <div className="pointer-events-none absolute inset-0 opacity-60">
        <div className="absolute -top-40 -left-40 h-80 w-80 rounded-full bg-gradient-to-br from-sky-500/40 via-cyan-400/20 to-transparent blur-3xl" />
        <div className="absolute bottom-[-6rem] right-[-4rem] h-96 w-96 rounded-full bg-gradient-to-tr from-indigo-500/30 via-purple-500/20 to-transparent blur-3xl" />
      </div>

      <div className="flex flex-1 min-h-0 relative z-10">
        <aside className="w-60 flex-shrink-0 border-r border-border/80 bg-surface-secondary/90 backdrop-blur flex flex-col">
          <div className="p-4 border-b border-border">
            <div className="rounded-lg bg-surface-tertiary px-3 py-2">
              <span className="text-sm font-semibold text-brand-blue">SenetLabs</span>
            </div>
            <p className="text-xs text-text-tertiary mt-2 font-medium">MICROPAD</p>
          </div>
          <nav className="p-2 flex-1">
            {nav.map(({ path, label }) => (
              <Link
                key={path}
                to={path}
                className={`block rounded px-3 py-2.5 text-sm font-medium transition ${
                  location.pathname === path
                    ? 'bg-surface-tertiary text-text-primary border-l-2 border-brand-blue'
                    : 'text-text-secondary hover:bg-surface-tertiary hover:text-text-primary'
                }`}
              >
                {label}
              </Link>
            ))}
          </nav>
        </aside>
        <main className="flex-1 overflow-auto">
          <div className="h-full w-full px-0 md:px-2 lg:px-4 py-2 animate-fade-in">
            {children}
          </div>
        </main>
      </div>
      <footer className="flex-shrink-0 border-t border-border/80 bg-surface-secondary/90 backdrop-blur px-4 py-2.5 flex items-center justify-between text-xs text-text-secondary relative z-10">
        <span>
          {deviceName && connectionState === 'ready' ? `${deviceName} • ` : ''}
          Last sync: —
        </span>
        <div className="flex items-center gap-3">
          <span className="inline-flex items-center gap-1.5 rounded-full border border-border bg-surface-tertiary px-2.5 py-1">
            <span className={`h-2 w-2 rounded-full ${connectionColor}`} />
            {connectionLabel}
          </span>
          {batteryLevel != null && (
            <span className="rounded-full border border-border bg-surface-tertiary px-2.5 py-1">
              {batteryLevel}% battery
            </span>
          )}
          <span className="text-text-tertiary">v1.0.0</span>
        </div>
      </footer>
    </div>
  );
}
