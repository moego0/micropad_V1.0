/** Action types for key assignment (matches firmware/Core). */
export enum ActionType {
  None = 0,
  Hotkey = 1,
  Macro = 2,
  Text = 3,
  Media = 4,
  Mouse = 5,
  Layer = 6,
  Profile = 7,
  App = 8,
  Url = 9
}

export enum MediaFunction {
  VolumeUp = 0,
  VolumeDown = 1,
  Mute = 2,
  PlayPause = 3,
  Next = 4,
  Prev = 5,
  Stop = 6
}

export enum MouseAction {
  Click = 0,
  RightClick = 1,
  MiddleClick = 2,
  ScrollUp = 3,
  ScrollDown = 4,
  ScrollLeft = 5,
  ScrollRight = 6
}

export interface KeyAction {
  type: ActionType;
  modifiers?: number;
  key?: number;
  text?: string;
  function?: number;
  action?: number;
  value?: number;
  profileId?: number;
  path?: string;
  url?: string;
  macroId?: string;
}

export interface KeyConfig {
  index: number;
  type: ActionType;
  modifiers: number;
  key: number;
  text?: string;
  function: number;
  action: number;
  value: number;
  profileId: number;
  AppPath?: string;
  url?: string;
  macroId?: string;
  macroSnapshot?: MacroStep[];
  tapAction?: KeyAction;
  holdAction?: KeyAction;
  doubleTapAction?: KeyAction;
}

export interface EncoderActionConfig {
  type: string;
  value?: number;
  key?: number;
  modifiers?: number;
  mediaFunction?: number;
}

export interface EncoderConfig {
  index: number;
  acceleration: boolean;
  stepsPerDetent: number;
  stepSize?: number;
  accelerationCurve?: string;
  smoothing?: boolean;
  mode?: number;
  cwAction?: EncoderActionConfig;
  ccwAction?: EncoderActionConfig;
  pressAction?: EncoderActionConfig;
  holdAction?: EncoderActionConfig;
  pressRotateCwAction?: EncoderActionConfig;
  pressRotateCcwAction?: EncoderActionConfig;
  holdRotateCwAction?: EncoderActionConfig;
  holdRotateCcwAction?: EncoderActionConfig;
}

export interface ComboAssignment {
  key1: number;
  key2: number;
  action?: KeyAction;
}

export interface Profile {
  id: number;
  name: string;
  version: number;
  keys: KeyConfig[];
  encoders: EncoderConfig[];
  layer1Keys?: KeyConfig[];
  layer2Keys?: KeyConfig[];
  combos?: ComboAssignment[];
}

export interface MacroStep {
  action: string;
  key?: string;
  ms?: number;
  text?: string;
  value?: number;
  vkCode?: number;
  mediaFunction?: number;
}

export interface MacroAsset {
  macroId: string;
  name: string;
  tags: string[];
  steps: MacroStep[];
  createdAt: string;
  updatedAt: string;
  version: number;
}

export interface DeviceInfo {
  deviceId: string;
  firmwareVersion: string;
  hardwareVersion: string;
  batteryLevel: number;
  capabilities: string[];
  uptime?: number;
  freeHeap?: number;
}

export interface DeviceCaps {
  maxProfiles: number;
  freeBytes: number;
  supportsLayers: boolean;
  supportsMacros: boolean;
  supportsEncoders: boolean;
}

export type ConnectionState =
  | 'idle'
  | 'requestingAccess'
  | 'reconnectingGrantedDevice'
  | 'connectingGatt'
  | 'configConnected'
  | 'hidConnected'
  | 'hidReady'
  | 'busyWithOtherHost'
  | 'error'
  | 'scanning'
  | 'pairing'
  | 'connecting'
  | 'ready'
  | 'reconnecting';

/** Connection status from firmware getConnectionStatus (truthful) */
export interface ConnectionStatus {
  configConnected: boolean;
  hidHostConnected: boolean;
  hidReady: boolean;
  advertising: boolean;
  clientCount: number;
  canAcceptConfigConnection: boolean;
  reason: string;
}

/** For device manager UI (previously granted device) */
export interface GrantedDeviceInfo {
  id: string;
  name: string;
  device?: BluetoothDevice;
}
