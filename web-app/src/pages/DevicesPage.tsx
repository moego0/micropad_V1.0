import { useEffect } from 'react';
import { useDeviceStore } from '../stores/deviceStore';

export default function DevicesPage() {
  const init = useDeviceStore((s) => s.init);
  const connect = useDeviceStore((s) => s.connect);
  const disconnect = useDeviceStore((s) => s.disconnect);
  const connectionState = useDeviceStore((s) => s.connectionState);
  const lastError = useDeviceStore((s) => s.lastError);
  const deviceInfo = useDeviceStore((s) => s.deviceInfo);
  const isSupported = useDeviceStore((s) => s.isWebBluetoothSupported);

  useEffect(() => {
    init();
  }, [init]);

  const stepIndex =
    connectionState === 'scanning' ? 1 : connectionState === 'connecting' || connectionState === 'pairing' ? 2 : connectionState === 'ready' ? 3 : 0;

  const handleConnect = () => {
    connect();
  };

  if (!isSupported()) {
    return (
      <div className="p-6 max-w-xl animate-fade-in">
        <h1 className="text-2xl font-bold text-text-primary mb-2">Bluetooth Devices</h1>
        <div className="h-1 w-12 bg-brand-blue rounded mb-4" />
        <div className="rounded-2xl border border-border/80 bg-surface-secondary/90 backdrop-blur p-6 text-text-secondary shadow-xl shadow-black/40">
          <p className="font-medium text-text-primary mb-2">Web Bluetooth not supported</p>
          <p>
            This browser does not support Web Bluetooth. Use Chrome, Edge, or Opera on desktop (or Chrome on Android)
            with HTTPS. Safari and Firefox do not support device connection.
          </p>
          <p className="mt-3 text-sm text-text-tertiary">
            You can still use the app offline to edit profiles and macros, and use Export/Import to move data.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6 animate-fade-in">
      <h1 className="text-2xl font-bold text-text-primary mb-2">Bluetooth Devices</h1>
      <div className="h-1 w-12 bg-brand-blue rounded mb-4" />
      <p className="text-text-secondary mb-6 max-w-2xl">
        Connect to your Micropad over Bluetooth. Click <span className="font-semibold text-text-primary">Connect</span> to open the
        browser pairing dialog (a user gesture is required).
      </p>

      {/* Connection journey */}
      <div className="rounded-2xl border border-border/80 bg-surface-secondary/90 backdrop-blur p-4 mb-6 shadow-lg shadow-black/40">
        <div className="flex items-center justify-between max-w-md mx-auto gap-2">
          {['Scan', 'Found', 'Pairing', 'Ready'].map((label, i) => (
            <div key={label} className="flex flex-col items-center">
              <div
                className={`h-7 w-7 rounded-full transition-all duration-300 ${
                  stepIndex > i
                    ? 'bg-brand-blue shadow-[0_0_0_4px_rgba(56,189,248,0.25)] scale-100'
                    : stepIndex === i && i === 3
                      ? 'bg-green-500 shadow-[0_0_0_4px_rgba(34,197,94,0.25)] scale-100'
                      : 'bg-surface-tertiary scale-95'
                }`}
              />
              <span className="text-xs text-text-secondary mt-1">{label}</span>
            </div>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="md:col-span-2 rounded-2xl border border-border/80 bg-surface-secondary/90 backdrop-blur p-5 shadow-lg shadow-black/40">
          <h2 className="font-semibold text-text-primary mb-3">Device</h2>
          <p className="text-sm text-text-secondary mb-4">
            Web Bluetooth will show a system dialog to choose the Micropad. Ensure the device is on and in range.
          </p>
          <div className="flex flex-wrap gap-2">
            <button
              onClick={handleConnect}
              disabled={connectionState === 'connecting' || connectionState === 'pairing'}
              className="px-5 py-2.5 bg-emerald-500 hover:bg-emerald-400 disabled:opacity-50 text-white rounded-full font-semibold text-sm shadow-md shadow-emerald-500/40 hover:shadow-lg hover:shadow-emerald-400/60 transition"
            >
              Connect
            </button>
            <button
              onClick={() => disconnect()}
              className="px-4 py-2 bg-red-500 hover:bg-red-400 text-white rounded-full font-medium text-sm shadow-md shadow-red-500/40 hover:shadow-lg hover:shadow-red-400/60 transition"
            >
              Disconnect
            </button>
          </div>
          {lastError && (
            <p className="mt-3 text-sm text-red-400" role="alert">
              {lastError}
            </p>
          )}
        </div>
        <div className="rounded-2xl border border-border/80 bg-surface-secondary/90 backdrop-blur p-5 shadow-lg shadow-black/40">
          <h2 className="font-semibold text-text-primary mb-3">Device Information</h2>
          {deviceInfo ? (
            <pre className="text-xs text-text-secondary whitespace-pre-wrap">
              ID: {deviceInfo.deviceId}
              FW: {deviceInfo.firmwareVersion}
              HW: {deviceInfo.hardwareVersion}
              Battery: {deviceInfo.batteryLevel}%
            </pre>
          ) : (
            <p className="text-sm text-text-tertiary">Select and connect to view info.</p>
          )}
          <p className="text-xs text-text-tertiary mt-2">State: {connectionState}</p>
        </div>
      </div>

      <p className="text-xs text-text-tertiary mt-6">
        If Connect fails: remove Micropad from system Bluetooth settings, power-cycle the device, then try again.
      </p>
    </div>
  );
}
