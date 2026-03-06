import { create } from 'zustand';
import { BleConnection, isWebBluetoothSupported } from '../device/bleConnection';
import { ProtocolHandler } from '../device/protocolHandler';
import type { ConnectionState } from '../device/bleConnection';
import type { DeviceInfo, DeviceCaps } from '../models/types';
import { loadSettings, saveSettings } from '../storage/settingsStorage';
import { ProfileSyncService } from '../services/profileSyncService';

interface DeviceStore {
  connectionState: ConnectionState;
  lastError: string | null;
  deviceName: string;
  deviceInfo: DeviceInfo | null;
  deviceCaps: DeviceCaps | null;
  batteryLevel: number | null;
  ble: BleConnection | null;
  protocol: ProtocolHandler | null;
  syncService: ProfileSyncService | null;
  init: () => void;
  connect: () => Promise<boolean>;
  disconnect: () => Promise<void>;
  setDeviceInfo: (info: DeviceInfo | null) => void;
  setCaps: (caps: DeviceCaps | null) => void;
  isWebBluetoothSupported: () => boolean;
}

export const useDeviceStore = create<DeviceStore>((set, get) => {
  let ble: BleConnection | null = null;
  let protocol: ProtocolHandler | null = null;
  let syncService: ProfileSyncService | null = null;

  return {
    connectionState: 'idle',
    lastError: null,
    deviceName: '',
    deviceInfo: null,
    deviceCaps: null,
    batteryLevel: null,
    ble: null,
    protocol: null,
    syncService: null,

    isWebBluetoothSupported: () => isWebBluetoothSupported(),

    init: () => {
      if (ble) return;
      ble = new BleConnection({
        onMessage: (json) => {
          protocol?.handleMessage(json);
        },
        onStateChange: (state, error) => {
          set((s) => ({ ...s, connectionState: state, lastError: error ?? null }));
        },
        onConnected: async () => {
          set((s) => ({ ...s, connectionState: 'ready', lastError: null }));
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
            } catch {
              // ignore
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
    },

    connect: async () => {
      get().init();
      const b = get().ble;
      if (!b) return false;
      const ok = await b.connect();
      return ok;
    },

    disconnect: async () => {
      const b = get().ble;
      if (b) await b.disconnect();
      set((s) => ({
        ...s,
        connectionState: 'idle',
        deviceName: '',
        deviceInfo: null,
        deviceCaps: null,
        batteryLevel: null
      }));
    },

    setDeviceInfo: (deviceInfo) => set((s) => ({ ...s, deviceInfo })),
    setCaps: (deviceCaps) => set((s) => ({ ...s, deviceCaps }))
  };
});
