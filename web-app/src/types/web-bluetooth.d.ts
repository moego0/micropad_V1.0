declare type BluetoothServiceUUID = string;
declare type BluetoothCharacteristicUUID = string;

declare interface RequestDeviceOptions {
  filters?: Array<{ services?: BluetoothServiceUUID[] }>;
  optionalServices?: BluetoothServiceUUID[];
}

declare interface BluetoothRemoteGATTCharacteristic {
  value?: DataView | null;
  writeValue(data: BufferSource | Uint8Array): Promise<void>;
  writeValueWithoutResponse(data: BufferSource | Uint8Array): Promise<void>;
  startNotifications(): Promise<void>;
  stopNotifications(): Promise<void>;
  addEventListener(type: 'characteristicvaluechanged', listener: (e: Event) => void): void;
  removeEventListener(type: 'characteristicvaluechanged', listener: (e: Event) => void): void;
}

declare interface BluetoothRemoteGATTServer {
  connected: boolean;
  connect(): Promise<BluetoothRemoteGATTServer>;
  disconnect(): void;
  getPrimaryService(service: BluetoothServiceUUID): Promise<BluetoothRemoteGATTService>;
}

declare interface BluetoothRemoteGATTService {
  getCharacteristic(characteristic: BluetoothCharacteristicUUID): Promise<BluetoothRemoteGATTCharacteristic>;
}

declare interface BluetoothDevice {
  id?: string;
  gatt?: BluetoothRemoteGATTServer | null;
  name?: string;
  addEventListener(type: 'gattserverdisconnected', listener: (e: Event) => void): void;
}

declare interface NavigatorBluetooth {
  requestDevice(options?: RequestDeviceOptions): Promise<BluetoothDevice>;
  getDevices?(): Promise<BluetoothDevice[]>;
}

declare interface Navigator {
  bluetooth: NavigatorBluetooth;
}

