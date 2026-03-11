import { useEffect } from 'react';
import { useDeviceStore } from '../stores/deviceStore';

function stateLabel(state: string): string {
  switch (state) {
    case 'idle':
      return 'Idle';
    case 'requestingAccess':
      return 'Requesting access…';
    case 'reconnectingGrantedDevice':
      return 'Reconnecting…';
    case 'connectingGatt':
      return 'Connecting GATT…';
    case 'configConnected':
      return 'Config channel connected';
    case 'hidConnected':
      return 'HID host connected';
    case 'hidReady':
      return 'Ready (config + HID)';
    case 'busyWithOtherHost':
      return 'Device busy with PC';
    case 'error':
      return 'Error';
    default:
      return state;
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
  const grantedDevices = useDeviceStore((s) => s.grantedDevices);
  const currentBleDevice = useDeviceStore((s) => s.ble?.currentDevice ?? null);
  const isSupported = useDeviceStore((s) => s.isWebBluetoothSupported);

  useEffect(() => {
    init();
  }, [init]);

  const isConnecting =
    connectionState === 'requestingAccess' ||
    connectionState === 'reconnectingGrantedDevice' ||
    connectionState === 'connectingGatt';
  const isConfigConnected =
    connectionState === 'configConnected' ||
    connectionState === 'hidConnected' ||
    connectionState === 'hidReady';
  const isFullyReady = connectionState === 'hidReady';
  const isBusyWithHost = connectionState === 'busyWithOtherHost';

  const handleRequestAccess = () => {
    requestAccess();
  };

  const handleReconnect = (device: BluetoothDevice) => {
    reconnectToGranted(device);
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
        Connect to your Micropad over Bluetooth. Use <span className="font-semibold text-text-primary">Reconnect</span> for
        a device you’ve already allowed, or <span className="font-semibold text-text-primary">Request access</span> to
        open the browser pairing dialog (user gesture required).
      </p>
      <p className="text-sm text-amber-600 dark:text-amber-400 mb-4 max-w-2xl rounded-lg border border-amber-500/40 bg-amber-500/10 px-3 py-2">
        <strong>If the Micropad is already connected to the PC</strong> (as a keyboard), use <strong>Reconnect</strong> below—not Request access—to connect from the browser. Request access may not show the device when it’s paired to the PC. Ensure the firmware allows 2 BLE connections (see MULTI_CONNECTION.md).
      </p>

      {/* Connection status card */}
      <div className="rounded-2xl border border-border/80 bg-surface-secondary/90 backdrop-blur p-5 shadow-lg shadow-black/40">
        <h2 className="font-semibold text-text-primary mb-3">Connection status</h2>
        <p className="text-sm text-text-secondary mb-2">
          <span className="font-medium text-text-primary">State:</span> {stateLabel(connectionState)}
        </p>
        {connectionStatus && (
          <div className="text-xs text-text-tertiary space-y-1 mb-3">
            <p>Config channel: {connectionStatus.configConnected ? 'Yes' : 'No'}</p>
            <p>HID host connected: {connectionStatus.hidHostConnected ? 'Yes' : 'No'}</p>
            <p>HID ready (reports enabled): {connectionStatus.hidReady ? 'Yes' : 'No'}</p>
            <p>Can accept config: {connectionStatus.canAcceptConfigConnection ? 'Yes' : 'No'}</p>
            <p>Reason: {connectionStatus.reason}</p>
          </div>
        )}
        {isBusyWithHost && (
          <p className="text-sm text-amber-500 mb-3">
            Config channel unavailable while the device is connected to the PC as an input device. Disconnect from
            Windows Bluetooth or wait until the PC disconnects to configure from the browser.
          </p>
        )}
        {isConfigConnected && !isFullyReady && !isBusyWithHost && (
          <p className="text-sm text-text-tertiary mb-3">
            Config channel is connected. Keys and encoders will only send input to the PC when the Micropad is also
            connected as a HID device (e.g. paired in Windows).
          </p>
        )}
        {isFullyReady && (
          <p className="text-sm text-emerald-600 dark:text-emerald-400 mb-3">
            You can edit profiles here and use the device as a keyboard/encoder on the PC at the same time.
          </p>
        )}
        <div className="flex flex-wrap gap-2">
          <button
            onClick={handleRequestAccess}
            disabled={isConnecting}
            className="px-5 py-2.5 bg-emerald-500 hover:bg-emerald-400 disabled:opacity-50 text-white rounded-full font-semibold text-sm shadow-md shadow-emerald-500/40 transition"
          >
            Request access
          </button>
          <button
            onClick={() => {
              const first = grantedDevices[0];
              if (first?.device) reconnectToGranted(first.device);
            }}
            disabled={isConnecting || grantedDevices.length === 0 || isConfigConnected}
            title={grantedDevices.length === 0 ? 'Connect once with Request access to enable Reconnect' : undefined}
            className="px-5 py-2.5 bg-brand-blue hover:bg-brand-blue/90 disabled:opacity-50 text-white rounded-full font-semibold text-sm shadow-md shadow-brand-blue/40 transition"
          >
            Reconnect
          </button>
          <button
            onClick={() => disconnect()}
            disabled={!isConfigConnected}
            className="px-4 py-2 bg-red-500 hover:bg-red-400 disabled:opacity-50 text-white rounded-full font-medium text-sm shadow-md shadow-red-500/40 transition"
          >
            Disconnect
          </button>
          <button
            onClick={() => refreshGrantedDevices()}
            className="px-4 py-2 bg-surface-tertiary hover:bg-surface-tertiary/80 text-text-secondary rounded-full font-medium text-sm transition"
          >
            Refresh list
          </button>
        </div>
        {!isConfigConnected && grantedDevices.length === 0 && (
          <p className="mt-3 text-sm text-text-tertiary">
            <strong>Reconnect</strong> is enabled after you connect once with <strong>Request access</strong>. If the Micropad is already paired to the PC: in Windows, disconnect it from Bluetooth (Quick settings → Bluetooth → Micropad), then click <strong>Request access</strong> here to pair this browser. After that, <strong>Reconnect</strong> will work even when the Micropad is connected to the PC.
          </p>
        )}
        {lastError && (
          <p className="mt-3 text-sm text-red-400" role="alert">
            {lastError}
          </p>
        )}
        {lastError && (lastError.includes('busy') || lastError.includes('PC')) && (
          <p className="mt-2 text-sm text-text-tertiary">
            In Windows: Settings → Bluetooth & devices → find Micropad → click the three dots → Disconnect (or Remove device). Then try Request access or Reconnect again.
          </p>
        )}
      </div>

      {/* Previously granted devices */}
      {grantedDevices.length > 0 && (
        <div className="rounded-2xl border border-border/80 bg-surface-secondary/90 backdrop-blur p-5 shadow-lg shadow-black/40">
          <h2 className="font-semibold text-text-primary mb-3">Previously granted devices</h2>
          <p className="text-sm text-text-secondary mb-4">
            Reconnect without opening the pairing dialog. Device must be on and in range.
          </p>
          <ul className="space-y-3">
            {grantedDevices.map((granted) => {
              const isCurrent = isConfigConnected && granted.device === currentBleDevice;
              return (
                <li
                  key={granted.id}
                  className="flex flex-wrap items-center justify-between gap-2 p-3 rounded-xl bg-surface-tertiary/50 border border-border/50"
                >
                  <div>
                    <p className="font-medium text-text-primary">{granted.name}</p>
                    <p className="text-xs text-text-tertiary">{granted.id}</p>
                  </div>
                  <button
                    onClick={() => granted.device && handleReconnect(granted.device)}
                    disabled={isConnecting || isCurrent}
                    className="px-4 py-2 bg-brand-blue hover:bg-brand-blue/90 disabled:opacity-50 text-white rounded-full font-medium text-sm transition"
                  >
                    {isCurrent ? 'Connected' : 'Reconnect'}
                  </button>
                </li>
              );
            })}
          </ul>
        </div>
      )}

      {/* Device info */}
      <div className="rounded-2xl border border-border/80 bg-surface-secondary/90 backdrop-blur p-5 shadow-lg shadow-black/40">
        <h2 className="font-semibold text-text-primary mb-3">Device information</h2>
        {deviceInfo ? (
          <pre className="text-xs text-text-secondary whitespace-pre-wrap">
            ID: {deviceInfo.deviceId}
            FW: {deviceInfo.firmwareVersion}
            HW: {deviceInfo.hardwareVersion}
            Battery: {deviceInfo.batteryLevel}%
          </pre>
        ) : (
          <p className="text-sm text-text-tertiary">Connect to a device to see info.</p>
        )}
      </div>

      <p className="text-xs text-text-tertiary">
        If the device doesn’t appear: ensure it’s on and advertising. When the device is connected to the PC as HID,
        you can still connect from the browser to edit profiles (if the firmware allows two connections). Use Reconnect
        for a previously granted device when possible. Refreshing the page (F5) closes the connection—use Reconnect to connect again.
      </p>
    </div>
  );
}
