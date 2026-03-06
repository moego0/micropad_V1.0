/**
 * Web Bluetooth connection to Micropad (same GATT as Windows app).
 * Config Service: 4fafc201-1fb5-459e-8fcc-c5c9c331914b
 * CMD (write): 4fafc201-1fb5-459e-8fcc-c5c9c331914c
 * EVT (notify): 4fafc201-1fb5-459e-8fcc-c5c9c331914d
 */

const CONFIG_SERVICE = '4fafc201-1fb5-459e-8fcc-c5c9c331914b';
const CMD_CHAR = '4fafc201-1fb5-459e-8fcc-c5c9c331914c';
const EVT_CHAR = '4fafc201-1fb5-459e-8fcc-c5c9c331914d';

export type ConnectionState = 'idle' | 'scanning' | 'pairing' | 'connecting' | 'ready' | 'reconnecting' | 'error';

export interface BleConnectionCallbacks {
  onMessage: (json: string) => void;
  onStateChange: (state: ConnectionState, error?: string) => void;
  onConnected: () => void;
  onDisconnected: () => void;
}

export function isWebBluetoothSupported (): boolean {
  return typeof navigator !== 'undefined' && 'bluetooth' in navigator;
}

const CHUNK_PAYLOAD_BYTES = 400;

export class BleConnection {
  private device: BluetoothDevice | null = null;
  private cmdChar: BluetoothRemoteGATTCharacteristic | null = null;
  private evtChar: BluetoothRemoteGATTCharacteristic | null = null;
  private state: ConnectionState = 'idle';
  private lastError: string | null = null;
  private callbacks: BleConnectionCallbacks;
  private evtListener: ((e: Event) => void) | null = null;

  constructor (callbacks: BleConnectionCallbacks) {
    this.callbacks = callbacks;
  }

  getState (): ConnectionState {
    return this.state;
  }

  getLastError (): string | null {
    return this.lastError;
  }

  get isConnected (): boolean {
    return this.device?.gatt?.connected ?? false;
  }

  get deviceName (): string {
    return this.device?.name ?? '';
  }

  private setState (s: ConnectionState, err?: string) {
    this.state = s;
    this.lastError = err ?? null;
    this.callbacks.onStateChange(s, err);
  }

  /** Request device and connect (user gesture required). */
  async connect (): Promise<boolean> {
    if (!isWebBluetoothSupported()) {
      this.setState('error', 'Web Bluetooth is not supported in this browser.');
      return false;
    }

    this.setState('connecting');

    try {
      this.device = await navigator.bluetooth.requestDevice({
        filters: [{ services: [CONFIG_SERVICE] }],
        optionalServices: [CONFIG_SERVICE]
      });

      if (!this.device.gatt) {
        this.setState('error', 'GATT not available');
        return false;
      }

      const server = await this.device.gatt.connect();
      const service = await server.getPrimaryService(CONFIG_SERVICE);
      this.cmdChar = await service.getCharacteristic(CMD_CHAR);
      this.evtChar = await service.getCharacteristic(EVT_CHAR);

      this.evtListener = (e: Event) => {
        const ev = e as CustomEvent<DataView>;
        const val = ev.target && (ev.target as unknown as BluetoothRemoteGATTCharacteristic).value;
        if (val) {
          const str = new TextDecoder().decode(val);
          this.callbacks.onMessage(str);
        }
      };
      this.evtChar.addEventListener('characteristicvaluechanged', this.evtListener);
      await this.evtChar.startNotifications();

      this.setState('ready');
      this.callbacks.onConnected();

      this.device.addEventListener('gattserverdisconnected', () => {
        this.disposeHandles();
        this.setState('idle');
        this.callbacks.onDisconnected();
      });

      return true;
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : String(err);
      this.setState('error', msg);
      this.disposeHandles();
      return false;
    }
  }

  async disconnect (): Promise<void> {
    this.disposeHandles();
    if (this.device?.gatt?.connected) {
      this.device.gatt.disconnect();
    }
    this.device = null;
    this.setState('idle');
    this.callbacks.onDisconnected();
  }

  private disposeHandles () {
    if (this.evtChar && this.evtListener) {
      this.evtChar.removeEventListener('characteristicvaluechanged', this.evtListener);
      this.evtChar.stopNotifications().catch(() => {});
    }
    this.evtListener = null;
    this.evtChar = null;
    this.cmdChar = null;
  }

  async sendMessage (json: string): Promise<void> {
    if (!this.cmdChar || !this.isConnected) {
      throw new Error('Not connected');
    }

    const bytes = new TextEncoder().encode(json);

    if (bytes.length > 512) {
      await this.sendChunked(bytes);
    } else {
      await this.cmdChar.writeValueWithoutResponse(bytes);
    }
  }

  private async sendChunked (utf8Bytes: Uint8Array): Promise<void> {
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
      await new Promise((r) => setTimeout(r, 10));
    }
  }
}
