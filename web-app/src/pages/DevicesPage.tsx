import { useEffect } from 'react';
import { useDeviceStore } from '../stores/deviceStore';

function connectionLabel(state: string, _status: ReturnType<typeof useDeviceStore.getState>['connectionStatus']): { text: string; color: string; detail: string } {
  switch (state) {
    case 'hidReady':
      return { text: 'Connected', color: 'text-emerald-400', detail: 'Your Micropad is connected as a keyboard and ready for configuration.' };
    case 'hidConnected':
      return { text: 'Connected to PC', color: 'text-emerald-400', detail: 'Micropad is connected to your PC as a keyboard. Configuration channel is also available.' };
    case 'configConnected':
      return { text: 'Configuration mode', color: 'text-amber-400', detail: 'Configuration channel connected. The Micropad is not paired to a PC as a keyboard yet — keys and encoders will work once paired.' };
    case 'busyWithOtherHost':
      return { text: 'Busy with PC', color: 'text-amber-400', detail: 'The Micropad is connected to a PC but the configuration channel is not available. This usually resolves when reconnecting.' };
    case 'requestingAccess':
    case 'reconnectingGrantedDevice':
    case 'connectingGatt':
      return { text: 'Connecting…', color: 'text-sky-400', detail: 'Establishing connection to your Micropad…' };
    case 'error':
      return { text: 'Connection error', color: 'text-red-400', detail: '' };
    default:
      return { text: 'Not connected', color: 'text-text-tertiary', detail: 'Connect your Micropad to configure keys, encoders, and profiles.' };
  }
}

export default function DevicesPage() {
  const init = useDeviceStore((s) => s.init);
  const disconnect = useDeviceStore((s) => s.disconnect);
  const requestAccess = useDeviceStore((s) => s.requestAccess);
  const reconnectToGranted = useDeviceStore((s) => s.reconnectToGranted);
  const refreshGrantedDevices = useDeviceStore((s) => s.refreshGrantedDevices);
  const connectionState = useDeviceStore((s) => s.connectionState);
  const connectionStatus = useDeviceStore((s) => s.connectionStatus);
  const lastError = useDeviceStore((s) => s.lastError);
  const deviceInfo = useDeviceStore((s) => s.deviceInfo);
  const deviceCaps = useDeviceStore((s) => s.deviceCaps);
  const grantedDevices = useDeviceStore((s) => s.grantedDevices);
  const isSupported = useDeviceStore((s) => s.isWebBluetoothSupported);

  useEffect(() => { init(); }, [init]);

  const isConnecting = ['requestingAccess', 'reconnectingGrantedDevice', 'connectingGatt'].includes(connectionState);
  const isConfigConnected = ['configConnected', 'hidConnected', 'hidReady'].includes(connectionState);
  const { text: statusText, color: statusColor, detail: statusDetail } = connectionLabel(connectionState, connectionStatus);

  if (!isSupported()) {
    return (
      <div className="p-6 max-w-2xl animate-fade-in">
        <h1 className="text-2xl font-bold text-text-primary mb-2">Connect Your Micropad</h1>
        <div className="h-1 w-12 bg-brand-blue rounded mb-6" />
        <div className="rounded-2xl border border-red-500/30 bg-red-500/5 p-6">
          <p className="font-medium text-text-primary mb-2">Browser not supported</p>
          <p className="text-text-secondary text-sm">
            This browser doesn't support Bluetooth connections. Please use <strong>Chrome</strong>, <strong>Edge</strong>, or <strong>Opera</strong> on desktop.
            Safari and Firefox are not supported.
          </p>
          <p className="mt-3 text-sm text-text-tertiary">
            You can still edit profiles and macros offline, and use Export/Import in Settings to move data.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6 max-w-3xl animate-fade-in">
      <div>
        <h1 className="text-2xl font-bold text-text-primary mb-2">Connect Your Micropad</h1>
        <div className="h-1 w-12 bg-brand-blue rounded mb-4" />
      </div>

      {/* Connection status */}
      <div className="rounded-2xl border border-border/80 bg-surface-secondary/90 backdrop-blur p-6 shadow-lg shadow-black/30">
        <div className="flex items-center gap-3 mb-3">
          <div className={`h-3 w-3 rounded-full ${isConfigConnected ? 'bg-emerald-500 animate-pulse' : isConnecting ? 'bg-sky-500 animate-pulse' : 'bg-surface-input'}`} />
          <h2 className={`text-lg font-semibold ${statusColor}`}>{statusText}</h2>
        </div>
        {statusDetail && <p className="text-sm text-text-secondary mb-4">{statusDetail}</p>}

        {lastError && (
          <div className="rounded-lg border border-red-500/30 bg-red-500/5 px-4 py-3 mb-4">
            <p className="text-sm text-red-400">{lastError}</p>
            {(lastError.includes('busy') || lastError.includes('PC') || lastError.includes('GATT')) && (
              <p className="text-xs text-text-tertiary mt-2">
                If the device is paired to your PC: open Windows Bluetooth settings, find Micropad, and disconnect it. Then try connecting again here.
              </p>
            )}
          </div>
        )}

        <div className="flex flex-wrap gap-3">
          {!isConfigConnected && (
            <>
              <button
                onClick={() => requestAccess()}
                disabled={isConnecting}
                className="px-6 py-2.5 bg-emerald-500 hover:bg-emerald-400 disabled:opacity-50 text-white rounded-full font-semibold text-sm shadow-md shadow-emerald-500/30 transition"
              >
                {isConnecting ? 'Connecting…' : 'Connect Micropad'}
              </button>
              {grantedDevices.length > 0 && (
                <button
                  onClick={() => {
                    const first = grantedDevices[0];
                    if (first?.device) reconnectToGranted(first.device);
                  }}
                  disabled={isConnecting}
                  className="px-5 py-2.5 bg-brand-blue hover:bg-brand-blue/90 disabled:opacity-50 text-white rounded-full font-semibold text-sm shadow-md shadow-brand-blue/30 transition"
                >
                  Reconnect
                </button>
              )}
            </>
          )}
          {isConfigConnected && (
            <button
              onClick={() => disconnect()}
              className="px-5 py-2.5 bg-surface-tertiary hover:bg-red-500/20 text-text-primary hover:text-red-400 rounded-full font-medium text-sm border border-border transition"
            >
              Disconnect
            </button>
          )}
          <button
            onClick={() => refreshGrantedDevices()}
            className="px-4 py-2.5 text-text-secondary hover:text-text-primary text-sm transition"
          >
            Refresh
          </button>
        </div>

        {!isConfigConnected && grantedDevices.length === 0 && (
          <div className="mt-4 rounded-lg bg-surface-tertiary/50 p-4">
            <p className="text-sm text-text-secondary">
              <strong>First time?</strong> Make sure your Micropad is powered on and nearby, then click <strong>Connect Micropad</strong>.
              Your browser will show a device picker — select "Micropad" to pair.
            </p>
            <p className="text-xs text-text-tertiary mt-2">
              After connecting once, the <strong>Reconnect</strong> button will appear for quick access next time.
            </p>
          </div>
        )}
      </div>

      {/* Previously paired devices */}
      {grantedDevices.length > 0 && !isConfigConnected && (
        <div className="rounded-2xl border border-border/80 bg-surface-secondary/90 backdrop-blur p-5 shadow-lg shadow-black/30">
          <h2 className="font-semibold text-text-primary mb-3">Saved devices</h2>
          <p className="text-sm text-text-secondary mb-4">
            Reconnect without the pairing dialog. The device must be powered on and in range.
          </p>
          <ul className="space-y-2">
            {grantedDevices.map((granted) => (
              <li
                key={granted.id}
                className="flex items-center justify-between gap-3 p-3 rounded-xl bg-surface-tertiary/50 border border-border/50"
              >
                <div>
                  <p className="font-medium text-text-primary">{granted.name || 'Micropad'}</p>
                  <p className="text-xs text-text-tertiary">{granted.id}</p>
                </div>
                <button
                  onClick={() => granted.device && reconnectToGranted(granted.device)}
                  disabled={isConnecting}
                  className="px-4 py-2 bg-brand-blue hover:bg-brand-blue/90 disabled:opacity-50 text-white rounded-full font-medium text-sm transition"
                >
                  Reconnect
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Device info card — shown when connected */}
      {isConfigConnected && deviceInfo && (
        <div className="rounded-2xl border border-border/80 bg-surface-secondary/90 backdrop-blur p-5 shadow-lg shadow-black/30">
          <h2 className="font-semibold text-text-primary mb-4">Device Details</h2>
          <div className="grid grid-cols-2 gap-x-8 gap-y-3 text-sm">
            <div>
              <p className="text-text-tertiary text-xs uppercase tracking-wide mb-0.5">Device</p>
              <p className="text-text-primary font-medium">{deviceInfo.deviceId}</p>
            </div>
            <div>
              <p className="text-text-tertiary text-xs uppercase tracking-wide mb-0.5">Firmware</p>
              <p className="text-text-primary font-medium">v{deviceInfo.firmwareVersion}</p>
            </div>
            <div>
              <p className="text-text-tertiary text-xs uppercase tracking-wide mb-0.5">Hardware</p>
              <p className="text-text-primary font-medium">v{deviceInfo.hardwareVersion}</p>
            </div>
            <div>
              <p className="text-text-tertiary text-xs uppercase tracking-wide mb-0.5">Battery</p>
              <p className="text-text-primary font-medium">{deviceInfo.batteryLevel}%</p>
            </div>
            {deviceCaps && (
              <>
                <div>
                  <p className="text-text-tertiary text-xs uppercase tracking-wide mb-0.5">Profile slots</p>
                  <p className="text-text-primary font-medium">{deviceCaps.maxProfiles}</p>
                </div>
                <div>
                  <p className="text-text-tertiary text-xs uppercase tracking-wide mb-0.5">Free storage</p>
                  <p className="text-text-primary font-medium">{Math.round(deviceCaps.freeBytes / 1024)}KB</p>
                </div>
              </>
            )}
            {connectionStatus && (
              <>
                <div>
                  <p className="text-text-tertiary text-xs uppercase tracking-wide mb-0.5">PC keyboard</p>
                  <p className="text-text-primary font-medium">{connectionStatus.hidReady ? 'Active' : connectionStatus.hidHostConnected ? 'Connecting' : 'Not paired'}</p>
                </div>
                <div>
                  <p className="text-text-tertiary text-xs uppercase tracking-wide mb-0.5">Config channel</p>
                  <p className="text-text-primary font-medium">{connectionStatus.configConnected ? 'Connected' : 'Not connected'}</p>
                </div>
              </>
            )}
          </div>
          {deviceCaps && (
            <div className="mt-4 pt-4 border-t border-border/50">
              <p className="text-xs text-text-tertiary">
                Supported: Keys, Encoders, Profiles{deviceCaps.supportsMacros ? ', Macros' : ''}{deviceCaps.supportsLayers ? ', Layers' : ''}
              </p>
            </div>
          )}
        </div>
      )}

      {/* Help text */}
      <div className="rounded-lg bg-surface-tertiary/30 px-4 py-3">
        <p className="text-xs text-text-tertiary">
          The Micropad can be connected to your PC as a keyboard and to this app for configuration at the same time.
          If connection fails, try turning the Micropad off and on, or unpair it from Windows Bluetooth settings first.
        </p>
      </div>
    </div>
  );
}
