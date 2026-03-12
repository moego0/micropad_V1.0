import { create } from 'zustand';
import type { DeviceCaps, Profile, KeyConfig, EncoderActionConfig } from '../models/types';
import { ActionType, MediaFunction, MouseAction } from '../models/types';
import { useDeviceStore } from './deviceStore';
import * as profileStorage from '../storage/profileStorage';

const KEY_COUNT = 12;
const ENCODER_COUNT = 2;
const DEFAULT_MAX_PROFILES = 8;

function ensureKeys(profile: Profile): void {
  while (profile.keys.length < KEY_COUNT) {
    profile.keys.push({
      index: profile.keys.length,
      type: ActionType.None,
      modifiers: 0,
      key: 0,
      function: 0,
      action: 0,
      value: 0,
      profileId: 0
    });
  }
  profile.keys.forEach((key, index) => { key.index = index; });
}

function ensureEncoders(profile: Profile): void {
  if (!profile.encoders) profile.encoders = [];
  while (profile.encoders.length < ENCODER_COUNT) {
    profile.encoders.push({
      index: profile.encoders.length,
      acceleration: true,
      stepsPerDetent: 4,
      cwAction: { type: ActionType.None },
      ccwAction: { type: ActionType.None },
      pressAction: { type: ActionType.None }
    });
  }
  profile.encoders.forEach((encoder, index) => { encoder.index = index; });
}

function cloneProfile(profile: Profile): Profile {
  return JSON.parse(JSON.stringify(profile)) as Profile;
}

function prepareProfile(profile: Profile): Profile {
  const next = cloneProfile(profile);
  ensureKeys(next);
  ensureEncoders(next);
  return next;
}

function upsertProfile(list: Profile[], profile: Profile): Profile[] {
  const next = prepareProfile(profile);
  const index = list.findIndex((entry) => entry.id === next.id);
  if (index >= 0) {
    return [...list.slice(0, index), next, ...list.slice(index + 1)];
  }
  return [...list, next].sort((a, b) => a.id - b.id);
}

function getProfileLimits(caps: DeviceCaps | null | undefined) {
  return {
    maxProfiles: caps?.maxProfiles ?? DEFAULT_MAX_PROFILES,
    maxKeys: caps?.maxKeys ?? KEY_COUNT,
    maxEncoders: caps?.maxEncoders ?? ENCODER_COUNT
  };
}

export function getNextAvailableProfileId(profiles: Profile[], maxProfiles: number): number | null {
  const used = new Set(profiles.map((profile) => profile.id));
  for (let nextId = 0; nextId < maxProfiles; nextId += 1) {
    if (!used.has(nextId)) return nextId;
  }
  return null;
}

export function getProfileValidationError(profile: Profile, caps: DeviceCaps | null | undefined): string | null {
  const limits = getProfileLimits(caps);
  if (profile.name.trim().length === 0) {
    return 'Give this profile a name before saving.';
  }
  if (profile.name.length > 31) {
    return 'Profile names can be up to 31 characters on this Micropad.';
  }
  if (profile.id >= limits.maxProfiles) {
    return `This profile ID exceeds your device's capacity (max ${limits.maxProfiles} profiles).`;
  }
  if (profile.keys.length > limits.maxKeys) {
    return `This profile has ${profile.keys.length} keys, but the device supports ${limits.maxKeys}.`;
  }
  if (profile.encoders.length > limits.maxEncoders) {
    return `This profile has ${profile.encoders.length} encoders, but the device supports ${limits.maxEncoders}.`;
  }
  return null;
}

export function getPushErrorMessage(error: unknown, maxProfiles: number): string {
  const message = error instanceof Error ? error.message : String(error ?? '');
  const lowered = message.toLowerCase();

  if (lowered.includes('not connected') || lowered.includes('gatt') || lowered.includes('disconnected') || lowered.includes('networkerror')) {
    return 'Lost connection while saving. Reconnect and try again.';
  }
  if (lowered.includes('protocol not ready')) {
    return 'Connect your Micropad on the Devices page first.';
  }
  if (lowered.includes('profile id exceeds device limit') || lowered.includes('device limit') || lowered.includes('out of range')) {
    return `This profile ID exceeds your device's capacity (max ${maxProfiles} profiles).`;
  }
  if (lowered.includes('save failed') || lowered.includes('invalid profile') || lowered.includes('device could not save')) {
    return 'The device could not save this profile. It may be full.';
  }

  return `Save failed: ${message}`;
}

const ENCODER_NONE: EncoderActionConfig = { type: ActionType.None };

interface ProfilesStore {
  profiles: Profile[];
  selectedProfile: Profile | null;
  editingProfile: Profile | null;
  selectedKeySlotIndex: number;
  activeProfileId: number | null;
  deviceCapsText: string;
  statusText: string;
  isProfilesLoading: boolean;
  isLoadingProfile: boolean;
  isPushInProgress: boolean;
  isDirty: boolean;
  pushStepText: string;
  lastSyncTime: string | null;
  loadProfiles: () => Promise<void>;
  selectProfile: (profile: Profile | null) => Promise<void>;
  setEditingProfile: (profile: Profile | null) => void;
  setSelectedKeySlotIndex: (index: number) => void;
  getKeySlots: () => KeyConfig[];
  updateKeyAt: (keyIndex: number, config: Partial<KeyConfig>) => void;
  pushToDevice: () => Promise<void>;
  pullFromDevice: () => Promise<void>;
  saveLocally: () => Promise<void>;
  createProfile: () => Promise<void>;
  createProfileFromPreset: (presetName: string) => Promise<void>;
  duplicateProfile: () => Promise<void>;
  deleteLocal: () => Promise<void>;
  deleteFromDevice: () => Promise<void>;
  setActiveProfileOnDevice: () => Promise<void>;
  setStatus: (status: string) => void;
  applyEncoderPreset: (encoderIndex: number, preset: string) => void;
  renameProfile: (name: string) => void;
}

export const useProfilesStore = create<ProfilesStore>((set, get) => ({
  profiles: [],
  selectedProfile: null,
  editingProfile: null,
  selectedKeySlotIndex: -1,
  activeProfileId: null,
  deviceCapsText: '',
  statusText: '',
  isProfilesLoading: false,
  isLoadingProfile: false,
  isPushInProgress: false,
  isDirty: false,
  pushStepText: '',
  lastSyncTime: null,

  loadProfiles: async () => {
    set((state) => ({ ...state, isProfilesLoading: true }));
    try {
      const deviceState = useDeviceStore.getState();
      const sync = deviceState.syncService;
      const local = (await profileStorage.getAllProfiles()).map(prepareProfile);
      let list = local;

      if (sync && deviceState.ble?.isConnected) {
        const deviceList = await sync.listProfiles();
        const deviceIds = new Set(deviceList.map((profile) => profile.id));

        for (const localProfile of local) {
          if (!deviceIds.has(localProfile.id)) list = [...list, localProfile];
        }
        for (const deviceProfile of deviceList) {
          if (!list.some((entry) => entry.id === deviceProfile.id)) list = [...list, deviceProfile];
        }

        list.sort((a, b) => a.id - b.id);
        const activeId = await sync.getActiveProfileId();
        const caps = await sync.getCaps();
        const capsText = caps
          ? `${list.length}/${caps.maxProfiles} profiles · ${Math.round(caps.freeBytes / 1024)}KB free`
          : '';

        set((state) => ({
          ...state,
          profiles: list,
          activeProfileId: activeId ?? null,
          deviceCapsText: capsText
        }));
      } else {
        set((state) => ({ ...state, profiles: list, deviceCapsText: '' }));
      }

      set((state) => ({
        ...state,
        statusText: list.length > 0 ? `${list.length} profile(s) loaded` : 'No profiles yet. Create one to get started.'
      }));
    } catch (error) {
      set((state) => ({ ...state, statusText: `Could not load profiles: ${(error as Error).message}` }));
    } finally {
      set((state) => ({ ...state, isProfilesLoading: false }));
    }
  },

  selectProfile: async (selectedProfile) => {
    if (!selectedProfile) {
      set((state) => ({
        ...state,
        selectedProfile: null,
        editingProfile: null,
        selectedKeySlotIndex: -1,
        isLoadingProfile: false,
        isDirty: false
      }));
      return;
    }

    set((state) => ({
      ...state,
      selectedProfile,
      selectedKeySlotIndex: -1,
      isLoadingProfile: true,
      statusText: `Loading "${selectedProfile.name}"…`
    }));

    try {
      const deviceState = useDeviceStore.getState();
      const sync = deviceState.syncService;
      let fullProfile: Profile | null = null;

      if (sync && deviceState.ble?.isConnected) {
        fullProfile = await sync.pullProfile(selectedProfile.id);
        if (!fullProfile) {
          set((state) => ({
            ...state,
            isLoadingProfile: false,
            editingProfile: null,
            isDirty: false,
            statusText: `Could not load "${selectedProfile.name}" from the device. Try again.`
          }));
          return;
        }
        await sync.saveProfileLocally(fullProfile);
      } else {
        fullProfile = await profileStorage.getProfile(selectedProfile.id);
        if (!fullProfile) {
          set((state) => ({
            ...state,
            isLoadingProfile: false,
            editingProfile: null,
            isDirty: false,
            statusText: 'This profile only exists on the device. Connect your Micropad to load it.'
          }));
          return;
        }
      }

      const readyProfile = prepareProfile(fullProfile);
      const profiles = upsertProfile(get().profiles, readyProfile);

      set((state) => ({
        ...state,
        profiles,
        selectedProfile: readyProfile,
        editingProfile: cloneProfile(readyProfile),
        isLoadingProfile: false,
        isDirty: false,
        statusText: deviceState.ble?.isConnected
          ? `"${readyProfile.name}" loaded from device`
          : `"${readyProfile.name}" loaded locally`
      }));
    } catch (error) {
      set((state) => ({
        ...state,
        isLoadingProfile: false,
        editingProfile: null,
        isDirty: false,
        statusText: `Could not load "${selectedProfile.name}" from device: ${(error as Error).message}`
      }));
    }
  },

  setEditingProfile: (editingProfile) => set((state) => ({ ...state, editingProfile })),
  setSelectedKeySlotIndex: (selectedKeySlotIndex) => set((state) => ({ ...state, selectedKeySlotIndex })),

  getKeySlots: () => {
    const { editingProfile } = get();
    if (!editingProfile) return [];

    ensureKeys(editingProfile);
    const out: KeyConfig[] = [];
    for (let index = 0; index < KEY_COUNT; index += 1) {
      out.push(editingProfile.keys[index] ?? {
        index,
        type: ActionType.None,
        modifiers: 0,
        key: 0,
        function: 0,
        action: 0,
        value: 0,
        profileId: 0
      });
    }
    return out;
  },

  updateKeyAt: (keyIndex, config) => {
    const { editingProfile } = get();
    if (!editingProfile) return;

    ensureKeys(editingProfile);
    if (keyIndex >= 0 && keyIndex < editingProfile.keys.length) {
      editingProfile.keys[keyIndex] = { ...editingProfile.keys[keyIndex], ...config, index: keyIndex };
      set((state) => ({ ...state, editingProfile: { ...editingProfile }, isDirty: true }));
    }
  },

  pushToDevice: async () => {
    const { editingProfile } = get();
    if (!editingProfile) return;

    const deviceState = useDeviceStore.getState();
    const sync = deviceState.syncService;
    if (!sync || !deviceState.ble?.isConnected) {
      set((state) => ({ ...state, statusText: 'Connect your Micropad on the Devices page first.' }));
      return;
    }

    const preparedProfile = prepareProfile(editingProfile);
    const validationError = getProfileValidationError(preparedProfile, deviceState.deviceCaps);
    const { maxProfiles } = getProfileLimits(deviceState.deviceCaps);

    if (validationError) {
      set((state) => ({ ...state, statusText: validationError }));
      return;
    }

    set((state) => ({ ...state, isPushInProgress: true, pushStepText: 'Preparing…' }));

    try {
      await new Promise((resolve) => setTimeout(resolve, 120));
      set((state) => ({ ...state, pushStepText: 'Sending to device…' }));

      const ok = await sync.pushProfile(preparedProfile);
      if (!ok) {
        set((state) => ({
          ...state,
          pushStepText: 'Failed',
          statusText: 'The device could not save this profile. It may be full.'
        }));
        return;
      }

      await sync.saveProfileLocally(preparedProfile);
      const now = new Date().toLocaleTimeString();
      const profiles = upsertProfile(get().profiles, preparedProfile);

      set((state) => ({
        ...state,
        profiles,
        selectedProfile: preparedProfile,
        editingProfile: cloneProfile(preparedProfile),
        isDirty: false,
        pushStepText: 'Saved!',
        statusText: `"${preparedProfile.name}" saved to device`,
        lastSyncTime: now
      }));
    } catch (error) {
      set((state) => ({
        ...state,
        pushStepText: 'Error',
        statusText: getPushErrorMessage(error, maxProfiles)
      }));
    } finally {
      setTimeout(() => {
        set((state) => ({ ...state, isPushInProgress: false, pushStepText: '' }));
      }, 1500);
    }
  },

  pullFromDevice: async () => {
    const { selectedProfile } = get();
    if (!selectedProfile) return;

    const deviceState = useDeviceStore.getState();
    const sync = deviceState.syncService;
    if (!sync || !deviceState.ble?.isConnected) {
      set((state) => ({ ...state, statusText: 'Connect your Micropad on the Devices page first.' }));
      return;
    }

    try {
      set((state) => ({ ...state, isLoadingProfile: true, statusText: 'Loading from device…' }));
      const full = await sync.pullProfile(selectedProfile.id);
      if (!full) {
        set((state) => ({
          ...state,
          isLoadingProfile: false,
          statusText: 'Could not load profile from device. Reconnect and try again.'
        }));
        return;
      }

      const readyProfile = prepareProfile(full);
      await sync.saveProfileLocally(readyProfile);
      const now = new Date().toLocaleTimeString();
      const profiles = upsertProfile(get().profiles, readyProfile);

      set((state) => ({
        ...state,
        profiles,
        selectedProfile: readyProfile,
        editingProfile: cloneProfile(readyProfile),
        isLoadingProfile: false,
        isDirty: false,
        statusText: `"${readyProfile.name}" loaded from device`,
        lastSyncTime: now
      }));
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error ?? '');
      set((state) => ({
        ...state,
        isLoadingProfile: false,
        statusText: message.toLowerCase().includes('not connected')
          ? 'Connect your Micropad on the Devices page first.'
          : `Load failed: ${message}`
      }));
    }
  },

  saveLocally: async () => {
    const { editingProfile } = get();
    if (!editingProfile) return;

    const preparedProfile = prepareProfile(editingProfile);
    await profileStorage.saveProfile(preparedProfile);
    const profiles = upsertProfile(get().profiles, preparedProfile);

    set((state) => ({
      ...state,
      profiles,
      selectedProfile: preparedProfile,
      editingProfile: cloneProfile(preparedProfile),
      isDirty: false,
      statusText: `"${preparedProfile.name}" saved locally`
    }));
  },

  createProfile: async () => {
    const profiles = get().profiles;
    const { maxProfiles } = getProfileLimits(useDeviceStore.getState().deviceCaps);
    const nextId = getNextAvailableProfileId(profiles, maxProfiles);

    if (nextId === null) {
      set((state) => ({
        ...state,
        statusText: `This device supports up to ${maxProfiles} profiles. Delete one first.`
      }));
      return;
    }

    const profile = prepareProfile({
      id: nextId,
      name: `Profile ${nextId + 1}`,
      version: 1,
      keys: [],
      encoders: []
    });

    await profileStorage.saveProfile(profile);
    const nextProfiles = upsertProfile(profiles, profile);

    set((state) => ({
      ...state,
      profiles: nextProfiles,
      selectedProfile: profile,
      editingProfile: cloneProfile(profile),
      isDirty: false,
      statusText: 'New profile created. Assign keys and save to device.'
    }));
  },

  createProfileFromPreset: async (presetName) => {
    const profiles = get().profiles;
    const { maxProfiles } = getProfileLimits(useDeviceStore.getState().deviceCaps);
    const nextId = getNextAvailableProfileId(profiles, maxProfiles);

    if (nextId === null) {
      set((state) => ({
        ...state,
        statusText: `This device supports up to ${maxProfiles} profiles. Delete one first.`
      }));
      return;
    }

    const profile = prepareProfile({
      id: nextId,
      name: presetName,
      version: 1,
      keys: [],
      encoders: []
    });

    await profileStorage.saveProfile(profile);
    const nextProfiles = upsertProfile(profiles, profile);

    set((state) => ({
      ...state,
      profiles: nextProfiles,
      selectedProfile: profile,
      editingProfile: cloneProfile(profile),
      isDirty: false,
      statusText: `"${presetName}" created. Customize keys in the Profiles tab.`
    }));
  },

  duplicateProfile: async () => {
    const { editingProfile, profiles } = get();
    if (!editingProfile) return;

    const { maxProfiles } = getProfileLimits(useDeviceStore.getState().deviceCaps);
    const nextId = getNextAvailableProfileId(profiles, maxProfiles);

    if (nextId === null) {
      set((state) => ({
        ...state,
        statusText: `This device supports up to ${maxProfiles} profiles. Delete one first.`
      }));
      return;
    }

    const copy = prepareProfile(editingProfile);
    copy.id = nextId;
    copy.name = `Copy of ${editingProfile.name || 'Unnamed'}`;
    copy.version = 1;

    await profileStorage.saveProfile(copy);
    const nextProfiles = upsertProfile(profiles, copy);

    set((state) => ({
      ...state,
      profiles: nextProfiles,
      selectedProfile: copy,
      editingProfile: cloneProfile(copy),
      isDirty: false,
      statusText: `Duplicated as "${copy.name}"`
    }));
  },

  deleteLocal: async () => {
    const { selectedProfile, profiles } = get();
    if (!selectedProfile) return;

    await profileStorage.deleteProfile(selectedProfile.id);
    const nextProfiles = profiles.filter((profile) => profile.id !== selectedProfile.id);

    set((state) => ({
      ...state,
      profiles: nextProfiles,
      selectedProfile: null,
      editingProfile: null,
      isDirty: false,
      statusText: 'Profile deleted locally'
    }));
  },

  deleteFromDevice: async () => {
    const { selectedProfile, profiles } = get();
    if (!selectedProfile) return;

    const deviceState = useDeviceStore.getState();
    const sync = deviceState.syncService;
    if (!sync || !deviceState.ble?.isConnected) {
      set((state) => ({ ...state, statusText: 'Connect your Micropad on the Devices page first.' }));
      return;
    }

    try {
      const ok = await sync.deleteProfileFromDevice(selectedProfile.id);
      if (!ok) {
        set((state) => ({ ...state, statusText: 'Device did not respond. Try reconnecting and trying again.' }));
        return;
      }

      const nextProfiles = profiles.filter((profile) => profile.id !== selectedProfile.id);

      set((state) => ({
        ...state,
        profiles: nextProfiles,
        selectedProfile: null,
        editingProfile: null,
        isDirty: false,
        statusText: 'Profile deleted from device'
      }));
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error ?? '');
      set((state) => ({
        ...state,
        statusText: message.toLowerCase().includes('not connected')
          ? 'Connect your Micropad on the Devices page first.'
          : `Delete failed: ${message}`
      }));
    }
  },

  setActiveProfileOnDevice: async () => {
    const { selectedProfile } = get();
    if (!selectedProfile) return;

    const deviceState = useDeviceStore.getState();
    const sync = deviceState.syncService;
    if (!sync || !deviceState.ble?.isConnected) {
      set((state) => ({ ...state, statusText: 'Connect your Micropad on the Devices page first.' }));
      return;
    }

    try {
      const ok = await sync.setActiveProfile(selectedProfile.id);
      if (ok) {
        set((state) => ({
          ...state,
          activeProfileId: selectedProfile.id,
          statusText: `"${selectedProfile.name}" is now the active profile`
        }));
      } else {
        set((state) => ({ ...state, statusText: 'Device did not respond. Try reconnecting and trying again.' }));
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error ?? '');
      set((state) => ({
        ...state,
        statusText: message.toLowerCase().includes('not connected')
          ? 'Connect your Micropad on the Devices page first.'
          : `Failed: ${message}`
      }));
    }
  },

  setStatus: (statusText) => set((state) => ({ ...state, statusText })),

  renameProfile: (name) => {
    const { editingProfile } = get();
    if (!editingProfile) return;

    editingProfile.name = name;
    set((state) => ({ ...state, editingProfile: { ...editingProfile }, isDirty: true }));
  },

  applyEncoderPreset: (encoderIndex, preset) => {
    const { editingProfile } = get();
    if (!editingProfile?.encoders?.[encoderIndex]) return;

    const encoder = editingProfile.encoders[encoderIndex];
    switch (preset) {
      case 'Volume':
        encoder.cwAction = { type: ActionType.Media, function: MediaFunction.VolumeUp };
        encoder.ccwAction = { type: ActionType.Media, function: MediaFunction.VolumeDown };
        encoder.pressAction = { type: ActionType.Media, function: MediaFunction.Mute };
        break;
      case 'Scroll':
        encoder.cwAction = { type: ActionType.Mouse, action: MouseAction.ScrollUp, value: 1 };
        encoder.ccwAction = { type: ActionType.Mouse, action: MouseAction.ScrollDown, value: 1 };
        encoder.pressAction = { ...ENCODER_NONE };
        break;
      case 'Zoom':
        encoder.cwAction = { type: ActionType.Hotkey, modifiers: 0x01, key: 0x2E };
        encoder.ccwAction = { type: ActionType.Hotkey, modifiers: 0x01, key: 0x2D };
        encoder.pressAction = { ...ENCODER_NONE };
        break;
      case 'Media':
        encoder.cwAction = { type: ActionType.Media, function: MediaFunction.Next };
        encoder.ccwAction = { type: ActionType.Media, function: MediaFunction.Prev };
        encoder.pressAction = { type: ActionType.Media, function: MediaFunction.PlayPause };
        break;
      default:
        encoder.cwAction = { ...ENCODER_NONE };
        encoder.ccwAction = { ...ENCODER_NONE };
        encoder.pressAction = { ...ENCODER_NONE };
        break;
    }

    set((state) => ({ ...state, editingProfile: { ...editingProfile }, isDirty: true }));
  }
}));
