import { create } from 'zustand';
import { BleConnection, isWebBluetoothSupported, getGrantedDevices } from '../device/bleConnection';
import { ProtocolHandler } from '../device/protocolHandler';
import type { ConnectionState } from '../device/bleConnection';
import type { DeviceInfo, DeviceCaps, ConnectionStatus, GrantedDeviceInfo } from '../models/types';
import { loadSettings, saveSettings } from '../storage/settingsStorage';
import { ProfileSyncService } from '../services/profileSyncService';

interface DeviceStore {
  connectionState: ConnectionState;
  connectionStatus: ConnectionStatus | null;
  lastError: string | null;
  deviceName: string;
  deviceInfo: DeviceInfo | null;
  deviceCaps: DeviceCaps | null;
  batteryLevel: number | null;
  grantedDevices: GrantedDeviceInfo[];
  ble: BleConnection | null;
  protocol: ProtocolHandler | null;
  syncService: ProfileSyncService | null;
  init: () => void;
  connect: () => Promise<boolean>;
  requestAccess: () => Promise<boolean>;
  reconnectToGranted: (device: BluetoothDevice) => Promise<boolean>;
  disconnect: () => Promise<void>;
  refreshGrantedDevices: () => Promise<void>;
  setDeviceInfo: (info: DeviceInfo | null) => void;
  setCaps: (caps: DeviceCaps | null) => void;
  isWebBluetoothSupported: () => boolean;
}

async function fetchConnectionStatusAndUpdate(protocol: ProtocolHandler | null, set: (fn: (s: DeviceStore) => Partial<DeviceStore>) => void) {
  if (!protocol) return;
  try {
    const status = await protocol.getConnectionStatus();
    if (status) {
      set((s) => ({
        ...s,
        connectionStatus: status,
        connectionState: deriveConnectionState(s.connectionState, status)
      }));
    }
  } catch {
    // ignore
  }
}

function deriveConnectionState(current: ConnectionState, status: ConnectionStatus): ConnectionState {
  if (current === 'idle' || current === 'error') return current;
  if (status.configConnected && status.hidReady) return 'hidReady';
  if (status.configConnected && status.hidHostConnected) return 'hidConnected';
  if (status.configConnected) return 'configConnected';
  if (status.reason === 'busy_with_hid_host') return 'busyWithOtherHost';
  return current;
}

export const useDeviceStore = create<DeviceStore>((set, get) => {
  let ble: BleConnection | null = null;
  let protocol: ProtocolHandler | null = null;
  let syncService: ProfileSyncService | null = null;

  return {
    connectionState: 'idle',
    connectionStatus: null,
    lastError: null,
    deviceName: '',
    deviceInfo: null,
    deviceCaps: null,
    batteryLevel: null,
    grantedDevices: [],
    ble: null,
    protocol: null,
    syncService: null,

    isWebBluetoothSupported: () => isWebBluetoothSupported(),

    init: () => {
      if (ble) {
        get().refreshGrantedDevices();
        return;
      }
      ble = new BleConnection({
        onMessage: (json) => {
          protocol?.handleMessage(json);
        },
        onStateChange: (state, error) => {
          set((s) => ({ ...s, connectionState: state, lastError: error ?? null }));
        },
        onConnected: async () => {
          set((s) => ({ ...s, lastError: null }));
          const pro = get().protocol;
          if (pro) {
            try {
              const info = await pro.getDeviceInfo();
              if (info) {
                const deviceInfo = info as unknown as DeviceInfo;
                set((s) => ({
                  ...s,
                  deviceInfo,
                  deviceName: deviceInfo.deviceId || 'Micropad',
                  batteryLevel: deviceInfo.batteryLevel ?? null
                }));
              }
              const caps = await pro.getCaps();
              set((s) => ({ ...s, deviceCaps: caps as unknown as DeviceCaps | null }));
              await fetchConnectionStatusAndUpdate(pro, set);
            } catch {
              // keep configConnected at least
            }
          }
          const id = get().deviceInfo?.deviceId;
          if (id) {
            const settings = await loadSettings();
            await saveSettings({ ...settings, lastDeviceId: id });
          }
        },
        onDisconnected: () => {
          set((s) => ({
            ...s,
            connectionState: 'idle',
            connectionStatus: null,
            deviceName: '',
            deviceInfo: null,
            deviceCaps: null,
            batteryLevel: null
          }));
        }
      });

      protocol = new ProtocolHandler(
        (json) => ble!.sendMessage(json),
        {
          onEvent: (msg) => {
            if (msg.event === 'profileChanged') {
              // could refresh active profile id in profiles store
            }
          }
        }
      );

      syncService = new ProfileSyncService(
        () => get().ble?.isConnected ?? false,
        () => get().protocol
      );

      set((s) => ({ ...s, ble, protocol, syncService }));
      get().refreshGrantedDevices();
    },

    connect: async () => {
      get().init();
      const b = get().ble;
      if (!b) return false;
      return b.requestDeviceAndConnect();
    },

    requestAccess: async () => {
      return get().connect();
    },

    reconnectToGranted: async (device: BluetoothDevice) => {
      get().init();
      const b = get().ble;
      if (!b) return false;
      set((s) => ({ ...s, connectionState: 'reconnectingGrantedDevice', lastError: null }));
      const ok = await b.connectToDevice(device);
      return ok;
    },

    disconnect: async () => {
      const b = get().ble;
      if (b) await b.disconnect();
      set((s) => ({
        ...s,
        connectionState: 'idle',
        connectionStatus: null,
        deviceName: '',
        deviceInfo: null,
        deviceCaps: null,
        batteryLevel: null
      }));
    },

    refreshGrantedDevices: async () => {
      const devices = await getGrantedDevices();
      const list: GrantedDeviceInfo[] = devices.map((d) => ({
        id: (d as { id?: string }).id ?? d.name ?? 'unknown',
        name: d.name ?? 'Micropad',
        device: d
      }));
      set((s) => ({ ...s, grantedDevices: list }));
    },

    setDeviceInfo: (deviceInfo) => set((s) => ({ ...s, deviceInfo })),
    setCaps: (deviceCaps) => set((s) => ({ ...s, deviceCaps }))
  };
});
