/**
 * Web Bluetooth connection to Micropad.
 * Config Service: 4fafc201-1fb5-459e-8fcc-c5c9c331914b
 * CMD (write): 4fafc201-1fb5-459e-8fcc-c5c9c331914c
 * EVT (notify): 4fafc201-1fb5-459e-8fcc-c5c9c331914d
 *
 * Chunked send: small chunks, fixed delay between each. No acks.
 * One send at a time. Firmware processes setProfile in main loop to avoid blocking.
 */

const CONFIG_SERVICE = '4fafc201-1fb5-459e-8fcc-c5c9c331914b';
const CMD_CHAR = '4fafc201-1fb5-459e-8fcc-c5c9c331914c';
const EVT_CHAR = '4fafc201-1fb5-459e-8fcc-c5c9c331914d';

export type ConnectionState =
  | 'idle'
  | 'requestingAccess'
  | 'reconnectingGrantedDevice'
  | 'connectingGatt'
  | 'configConnected'
  | 'hidConnected'
  | 'hidReady'
  | 'busyWithOtherHost'
  | 'error';

export interface BleConnectionCallbacks {
  onMessage: (json: string) => void;
  onStateChange: (state: ConnectionState, error?: string) => void;
  onConnected: () => void;
  onDisconnected: () => void;
}

export function isWebBluetoothSupported(): boolean {
  return typeof navigator !== 'undefined' && 'bluetooth' in navigator;
}

export async function getGrantedDevices(): Promise<BluetoothDevice[]> {
  if (!isWebBluetoothSupported() || typeof navigator.bluetooth.getDevices !== 'function') return [];
  try {
    return Array.from(await navigator.bluetooth.getDevices());
  } catch {
    return [];
  }
}

function normalizeError(err: unknown): string {
  if (err instanceof Error) {
    const msg = err.message || '';
    if (msg.includes('canceled') || msg.includes('cancelled') || msg.includes('User cancelled')) return 'Chooser cancelled';
    if (msg.includes('GATT Server is disconnected') || msg.includes('(Re)connect first')) return 'Device is busy (connected to PC). Disconnect from Windows Bluetooth first.';
    if (msg.includes('GATT') && msg.includes('unavailable')) return 'GATT unavailable';
    if (msg.includes('not found') || msg.includes('No device')) return 'Device not found or not advertising';
    if (msg.includes('Permission') || msg.includes('denied')) return 'Permission denied';
    if (msg.includes('busy') || msg.includes('already connected')) return 'Device busy with another host';
    if (msg.includes('not available') || msg.includes('unavailable')) return 'Device or service not available';
    if (msg.includes('GATT operation failed') || msg.includes('Unknown error')) return 'Bluetooth error. Disconnect, reconnect, then try again.';
    if (msg.includes('already in progress')) return 'Wait a few seconds and try again.';
    return msg || String(err);
  }
  return String(err);
}

const CHUNK_PAYLOAD_BYTES = 80;
const CHUNK_DELAY_MS = 250;
const SEND_RETRIES = 2;
const RETRY_DELAY_MS = 800;

export class BleConnection {
  private device: BluetoothDevice | null = null;
  private cmdChar: BluetoothRemoteGATTCharacteristic | null = null;
  private evtChar: BluetoothRemoteGATTCharacteristic | null = null;
  private state: ConnectionState = 'idle';
  private lastError: string | null = null;
  private callbacks: BleConnectionCallbacks;
  private evtListener: ((e: Event) => void) | null = null;
  private disconnectHandler: (() => void) | null = null;
  private sendLock: Promise<void> = Promise.resolve();

  constructor(callbacks: BleConnectionCallbacks) {
    this.callbacks = callbacks;
  }

  getState(): ConnectionState { return this.state; }
  getLastError(): string | null { return this.lastError; }
  get isConnected(): boolean { return this.device?.gatt?.connected ?? false; }
  get deviceName(): string { return this.device?.name ?? ''; }
  get currentDevice(): BluetoothDevice | null { return this.device; }

  private setState(s: ConnectionState, err?: string) {
    this.state = s;
    this.lastError = err ?? null;
    this.callbacks.onStateChange(s, err);
  }

  async requestDeviceAndConnect(): Promise<boolean> {
    if (!isWebBluetoothSupported()) { this.setState('error', 'Web Bluetooth not supported.'); return false; }
    this.setState('requestingAccess');
    try {
      const device = await navigator.bluetooth.requestDevice({ filters: [{ services: [CONFIG_SERVICE] }], optionalServices: [CONFIG_SERVICE] });
      return this.connectToDevice(device);
    } catch (err: unknown) {
      this.setState('error', normalizeError(err));
      this.disposeHandles();
      return false;
    }
  }

  async connectToDevice(device: BluetoothDevice): Promise<boolean> {
    if (!device?.gatt) { this.setState('error', 'No device or GATT'); return false; }
    if (this.device && this.device !== device) { this.disposeHandles(); this.device = null; }
    this.device = device;
    this.setState(this.state === 'reconnectingGrantedDevice' ? 'reconnectingGrantedDevice' : 'connectingGatt');
    try {
      const server = await this.device.gatt.connect();
      const service = await server.getPrimaryService(CONFIG_SERVICE);
      this.cmdChar = await service.getCharacteristic(CMD_CHAR);
      this.evtChar = await service.getCharacteristic(EVT_CHAR);
      this.evtListener = (e: Event) => {
        const val = (e.target as BluetoothRemoteGATTCharacteristic)?.value;
        if (val) this.callbacks.onMessage(new TextDecoder().decode(val));
      };
      this.evtChar.addEventListener('characteristicvaluechanged', this.evtListener);
      await this.evtChar.startNotifications();
      this.setState('configConnected');
      this.callbacks.onConnected();
      this.clearDisconnectHandler();
      const handler = () => { this.clearDisconnectHandler(); this.disposeHandles(); this.setState('idle'); this.callbacks.onDisconnected(); };
      this.device.addEventListener('gattserverdisconnected', handler);
      this.disconnectHandler = () => { this.device?.removeEventListener('gattserverdisconnected', handler); this.disconnectHandler = null; };
      return true;
    } catch (err: unknown) {
      this.setState('error', normalizeError(err));
      this.disposeHandles();
      return false;
    }
  }

  async connect(): Promise<boolean> { return this.requestDeviceAndConnect(); }

  async disconnect(): Promise<void> {
    this.clearDisconnectHandler();
    this.disposeHandles();
    if (this.device?.gatt?.connected) this.device.gatt.disconnect();
    this.device = null;
    this.setState('idle');
    this.callbacks.onDisconnected();
  }

  private clearDisconnectHandler() {
    if (this.disconnectHandler) { this.disconnectHandler(); this.disconnectHandler = null; }
  }

  private disposeHandles() {
    if (this.evtChar && this.evtListener) {
      try { this.evtChar.removeEventListener('characteristicvaluechanged', this.evtListener); this.evtChar.stopNotifications().catch(() => {}); } catch { /* ignore */ }
    }
    this.evtListener = null;
    this.evtChar = null;
    this.cmdChar = null;
  }

  async sendMessage(json: string): Promise<void> {
    if (!this.cmdChar || !this.isConnected) throw new Error('Not connected');
    const bytes = new TextEncoder().encode(json);
    let lastErr: unknown;
    for (let attempt = 0; attempt <= SEND_RETRIES; attempt++) {
      try {
        if (attempt > 0) await new Promise((r) => setTimeout(r, RETRY_DELAY_MS));
        await this.sendLock;
        let release: () => void;
        this.sendLock = new Promise<void>((r) => { release = r; });
        try {
          if (bytes.length > 512) await this.sendChunked(bytes);
          else await this.cmdChar.writeValueWithoutResponse(bytes);
        } finally {
          release!();
        }
        return;
      } catch (err) {
        lastErr = err;
        const msg = err instanceof Error ? err.message : String(err);
        if (msg.includes('already in progress') || attempt === SEND_RETRIES) throw err;
        if (!msg.includes('GATT') && !msg.includes('unknown reason')) throw err;
      }
    }
    throw lastErr;
  }

  private async sendChunked(utf8Bytes: Uint8Array): Promise<void> {
    if (!this.cmdChar) return;
    const totalChunks = Math.ceil(utf8Bytes.length / CHUNK_PAYLOAD_BYTES);
    for (let i = 0; i < totalChunks; i++) {
      const start = i * CHUNK_PAYLOAD_BYTES;
      const end = Math.min(start + CHUNK_PAYLOAD_BYTES, utf8Bytes.length);
      const segment = utf8Bytes.slice(start, end);
      const dataB64 = btoa(String.fromCharCode(...segment));
      const chunk = JSON.stringify({ chunk: i, total: totalChunks, dataB64 });
      const chunkBytes = new TextEncoder().encode(chunk);
      await this.cmdChar.writeValueWithoutResponse(chunkBytes);
      if (i < totalChunks - 1) {
        await new Promise((r) => setTimeout(r, CHUNK_DELAY_MS));
      }
    }
  }
}
