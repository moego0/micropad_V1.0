/** Action types matching firmware enum (profile.h) */
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
  ScrollDown = 4
}

/** Key action config — flat structure matching firmware JSON format */
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
  macroSteps?: MacroStepConfig[];
}

/** Encoder action — uses numeric types matching firmware */
export interface EncoderActionConfig {
  type: number;       // ActionType numeric value
  value?: number;
  key?: number;
  modifiers?: number;
  function?: number;  // MediaFunction numeric value
  action?: number;    // MouseAction numeric value
}

export interface EncoderConfig {
  index: number;
  acceleration: boolean;
  stepsPerDetent: number;
  cwAction?: EncoderActionConfig;
  ccwAction?: EncoderActionConfig;
  pressAction?: EncoderActionConfig;
}

export interface ComboAssignment {
  key1: number;
  key2: number;
  action?: EncoderActionConfig;
}

export interface Profile {
  id: number;
  name: string;
  version: number;
  keys: KeyConfig[];
  encoders: EncoderConfig[];
}

/** Macro step for on-device execution (matches firmware MacroStepConfig) */
export interface MacroStepConfig {
  stepType: number;  // 0=none, 1=delay, 2=keyPress, 3=text, 4=media
  delayMs?: number;
  key?: number;
  modifiers?: number;
  text?: string;
  mediaFunction?: number;
}

/** Macro asset saved in browser IndexedDB */
export interface MacroAsset {
  macroId: string;
  name: string;
  tags: string[];
  steps: MacroStepConfig[];
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
  maxKeys?: number;
  maxEncoders?: number;
  supportedActions?: number[];
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
  | 'error';

export interface ConnectionStatus {
  configConnected: boolean;
  hidHostConnected: boolean;
  hidReady: boolean;
  advertising: boolean;
  clientCount: number;
  canAcceptConfigConnection: boolean;
  reason: string;
}

export interface GrantedDeviceInfo {
  id: string;
  name: string;
  device?: BluetoothDevice;
}
