import { create } from 'zustand';
import type { Profile, KeyConfig, EncoderActionConfig } from '../models/types';
import { ActionType } from '../models/types';
import { useDeviceStore } from './deviceStore';
import * as profileStorage from '../storage/profileStorage';

const KEY_COUNT = 12;

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
  profile.keys.forEach((k, i) => { k.index = i; });
}

function ensureEncoders(profile: Profile): void {
  if (!profile.encoders) profile.encoders = [];
  while (profile.encoders.length < 2) {
    profile.encoders.push({
      index: profile.encoders.length,
      acceleration: true,
      stepsPerDetent: 4,
      cwAction: { type: ActionType.None },
      ccwAction: { type: ActionType.None },
      pressAction: { type: ActionType.None }
    });
  }
}

function cloneProfile(p: Profile): Profile {
  return JSON.parse(JSON.stringify(p));
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
  isPushInProgress: boolean;
  pushStepText: string;
  lastSyncTime: string | null;
  loadProfiles: () => Promise<void>;
  selectProfile: (p: Profile | null) => void;
  setEditingProfile: (p: Profile | null) => void;
  setSelectedKeySlotIndex: (i: number) => void;
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
  setStatus: (s: string) => void;
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
  isPushInProgress: false,
  pushStepText: '',
  lastSyncTime: null,

  loadProfiles: async () => {
    set((s) => ({ ...s, isProfilesLoading: true }));
    try {
      const sync = useDeviceStore.getState().syncService;
      const local = await profileStorage.getAllProfiles();
      let list = local;
      if (sync && useDeviceStore.getState().ble?.isConnected) {
        const deviceList = await sync.listProfiles();
        const deviceIds = new Set(deviceList.map((p) => p.id));
        for (const p of local) {
          if (!deviceIds.has(p.id)) list = [...list, p];
        }
        for (const p of deviceList) {
          if (!list.some((x) => x.id === p.id)) list = [...list, p];
        }
        list.sort((a, b) => a.id - b.id);
        const activeId = await sync.getActiveProfileId();
        const caps = await sync.getCaps();
        const capsText = caps
          ? `${list.length}/${caps.maxProfiles} profiles · ${Math.round(caps.freeBytes / 1024)}KB free`
          : '';
        set((s) => ({ ...s, profiles: list, activeProfileId: activeId ?? null, deviceCapsText: capsText }));
      } else {
        set((s) => ({ ...s, profiles: list }));
      }
      set((s) => ({ ...s, statusText: list.length > 0 ? `${list.length} profile(s) loaded` : 'No profiles yet. Create one to get started.' }));
    } catch (e) {
      set((s) => ({ ...s, statusText: `Could not load profiles: ${(e as Error).message}` }));
    } finally {
      set((s) => ({ ...s, isProfilesLoading: false }));
    }
  },

  selectProfile: (selectedProfile) => {
    set((s) => ({ ...s, selectedProfile, selectedKeySlotIndex: -1 }));
    if (selectedProfile) {
      const full = cloneProfile(selectedProfile);
      ensureKeys(full);
      ensureEncoders(full);
      set((s) => ({ ...s, editingProfile: full }));
    } else {
      set((s) => ({ ...s, editingProfile: null }));
    }
  },

  setEditingProfile: (editingProfile) => set((s) => ({ ...s, editingProfile })),
  setSelectedKeySlotIndex: (selectedKeySlotIndex) => set((s) => ({ ...s, selectedKeySlotIndex })),

  getKeySlots: () => {
    const { editingProfile } = get();
    if (!editingProfile) return [];
    ensureKeys(editingProfile);
    const out: KeyConfig[] = [];
    for (let i = 0; i < KEY_COUNT; i++) {
      out.push(editingProfile.keys[i] ?? { index: i, type: 0, modifiers: 0, key: 0, function: 0, action: 0, value: 0, profileId: 0 });
    }
    return out;
  },

  updateKeyAt: (keyIndex, config) => {
    const { editingProfile } = get();
    if (!editingProfile) return;
    const arr = editingProfile.keys;
    if (keyIndex >= 0 && keyIndex < arr.length) {
      arr[keyIndex] = { ...arr[keyIndex], ...config, index: keyIndex };
      set((s) => ({ ...s, editingProfile: { ...editingProfile } }));
    }
  },

  pushToDevice: async () => {
    const { editingProfile } = get();
    if (!editingProfile) return;
    const sync = useDeviceStore.getState().syncService;
    if (!sync) { set((s) => ({ ...s, statusText: 'Not connected. Open Devices page to connect.' })); return; }
    set((s) => ({ ...s, isPushInProgress: true, pushStepText: 'Preparing…' }));
    try {
      await new Promise((r) => setTimeout(r, 120));
      set((s) => ({ ...s, pushStepText: 'Sending to device…' }));
      const ok = await sync.pushProfile(editingProfile);
      if (ok) {
        await sync.saveProfileLocally(editingProfile);
        const now = new Date().toLocaleTimeString();
        set((s) => ({
          ...s,
          pushStepText: 'Saved!',
          statusText: `"${editingProfile.name}" saved to device`,
          lastSyncTime: now
        }));
      } else {
        set((s) => ({
          ...s,
          pushStepText: 'Failed',
          statusText: 'Device did not respond. Try disconnecting and reconnecting on the Devices page.'
        }));
      }
    } catch (e) {
      const msg = (e as Error).message;
      set((s) => ({
        ...s,
        pushStepText: 'Error',
        statusText: msg.includes('Not connected')
          ? 'Not connected. Open the Devices page to connect first.'
          : `Save failed: ${msg}`
      }));
    } finally {
      setTimeout(() => set((s) => ({ ...s, isPushInProgress: false, pushStepText: '' })), 1500);
    }
  },

  pullFromDevice: async () => {
    const { selectedProfile } = get();
    if (!selectedProfile) return;
    const sync = useDeviceStore.getState().syncService;
    if (!sync) { set((s) => ({ ...s, statusText: 'Not connected. Open Devices page to connect.' })); return; }
    try {
      set((s) => ({ ...s, statusText: 'Loading from device…' }));
      const full = await sync.pullProfile(selectedProfile.id);
      if (!full) {
        set((s) => ({ ...s, statusText: 'Device did not respond. Try disconnecting and reconnecting.' }));
        return;
      }
      ensureKeys(full);
      ensureEncoders(full);
      await sync.saveProfileLocally(full);
      const list = get().profiles;
      const idx = list.findIndex((p) => p.id === full.id);
      const newList = idx >= 0 ? [...list.slice(0, idx), full, ...list.slice(idx + 1)] : [...list, full].sort((a, b) => a.id - b.id);
      const now = new Date().toLocaleTimeString();
      set((s) => ({
        ...s,
        profiles: newList,
        selectedProfile: full,
        editingProfile: cloneProfile(full),
        statusText: `"${full.name}" loaded from device`,
        lastSyncTime: now
      }));
    } catch (e) {
      const msg = (e as Error).message;
      set((s) => ({
        ...s,
        statusText: msg.includes('Not connected')
          ? 'Not connected. Open the Devices page to connect first.'
          : `Load failed: ${msg}`
      }));
    }
  },

  saveLocally: async () => {
    const { editingProfile } = get();
    if (!editingProfile) return;
    await profileStorage.saveProfile(editingProfile);
    const list = get().profiles;
    const idx = list.findIndex((p) => p.id === editingProfile.id);
    const newList = idx >= 0 ? [...list.slice(0, idx), editingProfile, ...list.slice(idx + 1)] : [...list, editingProfile].sort((a, b) => a.id - b.id);
    set((s) => ({ ...s, profiles: newList, statusText: `"${editingProfile.name}" saved locally` }));
  },

  createProfile: async () => {
    const list = get().profiles;
    const used = new Set(list.map((p) => p.id));
    let nextId = 0;
    while (used.has(nextId) && nextId < 64) nextId++;
    const profile: Profile = {
      id: nextId,
      name: `Profile ${nextId + 1}`,
      version: 1,
      keys: [],
      encoders: []
    };
    ensureKeys(profile);
    ensureEncoders(profile);
    await profileStorage.saveProfile(profile);
    const newList = [...list, profile].sort((a, b) => a.id - b.id);
    set((s) => ({
      ...s,
      profiles: newList,
      selectedProfile: profile,
      editingProfile: cloneProfile(profile),
      statusText: 'New profile created. Assign keys and save to device.'
    }));
  },

  createProfileFromPreset: async (presetName) => {
    const list = get().profiles;
    const used = new Set(list.map((p) => p.id));
    let nextId = 0;
    while (used.has(nextId) && nextId < 64) nextId++;
    const profile: Profile = {
      id: nextId,
      name: presetName,
      version: 1,
      keys: [],
      encoders: []
    };
    ensureKeys(profile);
    ensureEncoders(profile);
    await profileStorage.saveProfile(profile);
    const newList = [...list, profile].sort((a, b) => a.id - b.id);
    set((s) => ({
      ...s,
      profiles: newList,
      selectedProfile: profile,
      editingProfile: cloneProfile(profile),
      statusText: `"${presetName}" created. Customize keys in the Profiles tab.`
    }));
  },

  duplicateProfile: async () => {
    const { editingProfile, profiles } = get();
    if (!editingProfile) return;
    const used = new Set(profiles.map((p) => p.id));
    let nextId = 0;
    while (used.has(nextId) && nextId < 64) nextId++;
    const copy = cloneProfile(editingProfile);
    copy.id = nextId;
    copy.name = `Copy of ${editingProfile.name || 'Unnamed'}`;
    copy.version = 1;
    await profileStorage.saveProfile(copy);
    const newList = [...profiles, copy].sort((a, b) => a.id - b.id);
    set((s) => ({
      ...s,
      profiles: newList,
      selectedProfile: copy,
      editingProfile: cloneProfile(copy),
      statusText: `Duplicated as "${copy.name}"`
    }));
  },

  deleteLocal: async () => {
    const { selectedProfile, profiles } = get();
    if (!selectedProfile) return;
    await profileStorage.deleteProfile(selectedProfile.id);
    const newList = profiles.filter((p) => p.id !== selectedProfile.id);
    const next = newList[0] ?? null;
    set((s) => ({
      ...s,
      profiles: newList,
      selectedProfile: next,
      editingProfile: next ? cloneProfile(next) : null,
      statusText: 'Profile deleted locally'
    }));
  },

  deleteFromDevice: async () => {
    const { selectedProfile, profiles } = get();
    if (!selectedProfile) return;
    const sync = useDeviceStore.getState().syncService;
    if (!sync) { set((s) => ({ ...s, statusText: 'Not connected. Open Devices page to connect.' })); return; }
    try {
      const ok = await sync.deleteProfileFromDevice(selectedProfile.id);
      if (ok) {
        const newList = profiles.filter((p) => p.id !== selectedProfile.id);
        const next = newList[0] ?? null;
        set((s) => ({
          ...s,
          profiles: newList,
          selectedProfile: next,
          editingProfile: next ? cloneProfile(next) : null,
          statusText: 'Profile deleted from device'
        }));
      } else {
        set((s) => ({ ...s, statusText: 'Device did not respond. Try disconnecting and reconnecting.' }));
      }
    } catch (e) {
      const msg = (e as Error).message;
      set((s) => ({ ...s, statusText: msg.includes('Not connected') ? 'Not connected. Open the Devices page first.' : `Delete failed: ${msg}` }));
    }
  },

  setActiveProfileOnDevice: async () => {
    const { selectedProfile } = get();
    if (!selectedProfile) return;
    const sync = useDeviceStore.getState().syncService;
    if (!sync) { set((s) => ({ ...s, statusText: 'Not connected. Open Devices page to connect.' })); return; }
    try {
      const ok = await sync.setActiveProfile(selectedProfile.id);
      if (ok) set((s) => ({ ...s, activeProfileId: selectedProfile.id, statusText: `"${selectedProfile.name}" is now the active profile` }));
      else set((s) => ({ ...s, statusText: 'Device did not respond. Try disconnecting and reconnecting.' }));
    } catch (e) {
      const msg = (e as Error).message;
      set((s) => ({ ...s, statusText: msg.includes('Not connected') ? 'Not connected.' : `Failed: ${msg}` }));
    }
  },

  setStatus: (statusText) => set((s) => ({ ...s, statusText })),

  renameProfile: (name: string) => {
    const { editingProfile } = get();
    if (!editingProfile) return;
    editingProfile.name = name;
    set((s) => ({ ...s, editingProfile: { ...editingProfile } }));
  },

  applyEncoderPreset: (encoderIndex, preset) => {
    const { editingProfile } = get();
    if (!editingProfile?.encoders?.[encoderIndex]) return;
    const enc = editingProfile.encoders[encoderIndex];
    switch (preset) {
      case 'Volume':
        enc.cwAction = { type: ActionType.Media, function: MediaFunction.VolumeUp };
        enc.ccwAction = { type: ActionType.Media, function: MediaFunction.VolumeDown };
        enc.pressAction = { type: ActionType.Media, function: MediaFunction.Mute };
        break;
      case 'Scroll':
        enc.cwAction = { type: ActionType.Mouse, action: MouseAction.ScrollUp, value: 1 };
        enc.ccwAction = { type: ActionType.Mouse, action: MouseAction.ScrollDown, value: 1 };
        enc.pressAction = ENCODER_NONE;
        break;
      case 'Zoom':
        enc.cwAction = { type: ActionType.Hotkey, modifiers: 0x01, key: 0x2E };  // Ctrl + ]
        enc.ccwAction = { type: ActionType.Hotkey, modifiers: 0x01, key: 0x2D };  // Ctrl + [
        enc.pressAction = ENCODER_NONE;
        break;
      case 'Media':
        enc.cwAction = { type: ActionType.Media, function: MediaFunction.Next };
        enc.ccwAction = { type: ActionType.Media, function: MediaFunction.Prev };
        enc.pressAction = { type: ActionType.Media, function: MediaFunction.PlayPause };
        break;
      default:
        enc.cwAction = enc.ccwAction = enc.pressAction = { ...ENCODER_NONE };
    }
    set((s) => ({ ...s, editingProfile: { ...editingProfile } }));
  }
}));

// Re-export MediaFunction / MouseAction for encoder presets
import { MediaFunction, MouseAction } from '../models/types';
