import { create } from 'zustand';
import type { Profile, KeyConfig, ComboAssignment } from '../models/types';
import { useDeviceStore } from './deviceStore';
import * as profileStorage from '../storage/profileStorage';

const KEY_COUNT = 12;

function ensureKeys (profile: Profile): void {
  while (profile.keys.length < KEY_COUNT) {
    profile.keys.push({
      index: profile.keys.length,
      type: 0,
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

function ensureLayerKeys (profile: Profile): void {
  if (!profile.layer1Keys) {
    profile.layer1Keys = Array.from({ length: KEY_COUNT }, (_, i) => ({
      index: i,
      type: 0,
      modifiers: 0,
      key: 0,
      function: 0,
      action: 0,
      value: 0,
      profileId: 0
    }));
  }
  if (!profile.layer2Keys) {
    profile.layer2Keys = Array.from({ length: KEY_COUNT }, (_, i) => ({
      index: i,
      type: 0,
      modifiers: 0,
      key: 0,
      function: 0,
      action: 0,
      value: 0,
      profileId: 0
    }));
  }
}

function ensureEncoders (profile: Profile): void {
  if (!profile.encoders) profile.encoders = [];
  while (profile.encoders.length < 2) {
    profile.encoders.push({
      index: profile.encoders.length,
      acceleration: false,
      stepsPerDetent: 1,
      cwAction: { type: 'none' },
      ccwAction: { type: 'none' },
      pressAction: { type: 'none' }
    });
  }
}

function cloneProfile (p: Profile): Profile {
  return JSON.parse(JSON.stringify(p));
}

interface ProfilesStore {
  profiles: Profile[];
  selectedProfile: Profile | null;
  editingProfile: Profile | null;
  selectedLayerIndex: number;
  selectedKeySlotIndex: number;
  activeProfileId: number | null;
  deviceCapsText: string;
  statusText: string;
  isProfilesLoading: boolean;
  isPushInProgress: boolean;
  pushStepText: string;
  loadProfiles: () => Promise<void>;
  selectProfile: (p: Profile | null) => void;
  setEditingProfile: (p: Profile | null) => void;
  setSelectedLayerIndex: (i: number) => void;
  setSelectedKeySlotIndex: (i: number) => void;
  getKeySlotsForLayer: () => KeyConfig[];
  refreshKeySlots: () => void;
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
  addCombo: () => void;
  removeCombo: (c: ComboAssignment) => void;
  updateComboKeys: (c: ComboAssignment, key1: number, key2: number) => void;
  getCombos: () => ComboAssignment[];
  applyEncoderPreset: (encoderIndex: number, preset: string) => void;
}

export const useProfilesStore = create<ProfilesStore>((set, get) => ({
  profiles: [],
  selectedProfile: null,
  editingProfile: null,
  selectedLayerIndex: 0,
  selectedKeySlotIndex: -1,
  activeProfileId: null,
  deviceCapsText: '',
  statusText: 'Click Refresh to load profiles',
  isProfilesLoading: false,
  isPushInProgress: false,
  pushStepText: '',

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
          ? `Device: ${list.length}/${caps.maxProfiles} slots, ${caps.freeBytes} bytes free`
          : '';
        set((s) => ({ ...s, profiles: list, activeProfileId: activeId ?? null, deviceCapsText: capsText }));
      } else {
        set((s) => ({ ...s, profiles: list }));
      }
      set((s) => ({ ...s, statusText: `Loaded ${list.length} profile(s)` }));
    } catch (e) {
      set((s) => ({ ...s, statusText: `Failed: ${(e as Error).message}` }));
    } finally {
      set((s) => ({ ...s, isProfilesLoading: false }));
    }
  },

  selectProfile: (selectedProfile) => {
    set((s) => ({ ...s, selectedProfile, selectedKeySlotIndex: -1 }));
    if (selectedProfile) {
      const full = cloneProfile(selectedProfile);
      ensureKeys(full);
      ensureLayerKeys(full);
      ensureEncoders(full);
      set((s) => ({ ...s, editingProfile: full }));
    } else {
      set((s) => ({ ...s, editingProfile: null }));
    }
  },

  setEditingProfile: (editingProfile) => set((s) => ({ ...s, editingProfile })),
  setSelectedLayerIndex: (selectedLayerIndex) => set((s) => ({ ...s, selectedLayerIndex })),
  setSelectedKeySlotIndex: (selectedKeySlotIndex) => set((s) => ({ ...s, selectedKeySlotIndex })),

  getKeySlotsForLayer: () => {
    const { editingProfile, selectedLayerIndex } = get();
    if (!editingProfile) return [];
    ensureKeys(editingProfile);
    ensureLayerKeys(editingProfile);
    const source =
      selectedLayerIndex === 1
        ? editingProfile.layer1Keys
        : selectedLayerIndex === 2
          ? editingProfile.layer2Keys
          : editingProfile.keys;
    const arr = source ?? editingProfile.keys;
    const out: KeyConfig[] = [];
    for (let i = 0; i < KEY_COUNT; i++) {
      out.push(arr[i] ?? { index: i, type: 0, modifiers: 0, key: 0, function: 0, action: 0, value: 0, profileId: 0 });
    }
    return out;
  },

  refreshKeySlots: () => {
    // No-op; key slots are derived via getKeySlotsForLayer in component
  },

  updateKeyAt: (keyIndex, config) => {
    const { editingProfile, selectedLayerIndex } = get();
    if (!editingProfile) return;
    const source =
      selectedLayerIndex === 1
        ? editingProfile.layer1Keys
        : selectedLayerIndex === 2
          ? editingProfile.layer2Keys
          : editingProfile.keys;
    const arr = source ?? editingProfile.keys;
    if (keyIndex >= 0 && keyIndex < arr.length) {
      arr[keyIndex] = { ...arr[keyIndex], ...config, index: keyIndex };
      set((s) => ({ ...s, editingProfile: { ...editingProfile } }));
    }
  },

  pushToDevice: async () => {
    const { editingProfile } = get();
    if (!editingProfile) return;
    const sync = useDeviceStore.getState().syncService;
    if (!sync) return;
    set((s) => ({ ...s, isPushInProgress: true, pushStepText: 'Preparing...' }));
    try {
      await new Promise((r) => setTimeout(r, 120));
      set((s) => ({ ...s, pushStepText: 'Sending to device...' }));
      const ok = await sync.pushProfile(editingProfile);
      set((s) => ({ ...s, pushStepText: ok ? 'Done' : 'Failed' }));
      if (ok) {
        await sync.saveProfileLocally(editingProfile);
        set((s) => ({ ...s, statusText: `Pushed '${editingProfile.name}' to device` }));
      } else {
        set((s) => ({ ...s, statusText: 'Failed to push (check connection)' }));
      }
    } catch (e) {
      set((s) => ({ ...s, pushStepText: 'Error', statusText: `Failed: ${(e as Error).message}` }));
    } finally {
      set((s) => ({ ...s, isPushInProgress: false, pushStepText: '' }));
    }
  },

  pullFromDevice: async () => {
    const { selectedProfile } = get();
    if (!selectedProfile) return;
    const sync = useDeviceStore.getState().syncService;
    if (!sync) return;
    try {
      const full = await sync.pullProfile(selectedProfile.id);
      if (!full) {
        set((s) => ({ ...s, statusText: 'Failed to pull (check connection)' }));
        return;
      }
      ensureKeys(full);
      ensureLayerKeys(full);
      ensureEncoders(full);
      await sync.saveProfileLocally(full);
      const list = get().profiles;
      const idx = list.findIndex((p) => p.id === full.id);
      const newList = idx >= 0 ? [...list.slice(0, idx), full, ...list.slice(idx + 1)] : [...list, full].sort((a, b) => a.id - b.id);
      set((s) => ({
        ...s,
        profiles: newList,
        selectedProfile: full,
        editingProfile: cloneProfile(full),
        statusText: `Pulled '${full.name}' from device`
      }));
    } catch (e) {
      set((s) => ({ ...s, statusText: `Pull failed: ${(e as Error).message}` }));
    }
  },

  saveLocally: async () => {
    const { editingProfile } = get();
    if (!editingProfile) return;
    await profileStorage.saveProfile(editingProfile);
    const list = get().profiles;
    const idx = list.findIndex((p) => p.id === editingProfile.id);
    const newList = idx >= 0 ? [...list.slice(0, idx), editingProfile, ...list.slice(idx + 1)] : [...list, editingProfile].sort((a, b) => a.id - b.id);
    set((s) => ({ ...s, profiles: newList, statusText: `Saved '${editingProfile.name}' locally` }));
  },

  createProfile: async () => {
    const list = get().profiles;
    const used = new Set(list.map((p) => p.id));
    let nextId = 0;
    while (used.has(nextId) && nextId < 64) nextId++;
    const profile: Profile = {
      id: nextId,
      name: 'New profile',
      version: 1,
      keys: [],
      encoders: []
    };
    ensureKeys(profile);
    ensureLayerKeys(profile);
    ensureEncoders(profile);
    await profileStorage.saveProfile(profile);
    const newList = [...list, profile].sort((a, b) => a.id - b.id);
    set((s) => ({
      ...s,
      profiles: newList,
      selectedProfile: profile,
      editingProfile: cloneProfile(profile),
      statusText: "Created profile. Edit and push to device."
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
    ensureLayerKeys(profile);
    ensureEncoders(profile);
    await profileStorage.saveProfile(profile);
    const newList = [...list, profile].sort((a, b) => a.id - b.id);
    set((s) => ({
      ...s,
      profiles: newList,
      selectedProfile: profile,
      editingProfile: cloneProfile(profile),
      statusText: `Created preset '${presetName}'. Edit keys and push to device.`
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
    copy.name = 'Copy of ' + (editingProfile.name || 'Unnamed');
    copy.version = 1;
    await profileStorage.saveProfile(copy);
    const newList = [...profiles, copy].sort((a, b) => a.id - b.id);
    set((s) => ({
      ...s,
      profiles: newList,
      selectedProfile: copy,
      editingProfile: cloneProfile(copy),
      statusText: `Duplicated as '${copy.name}'`
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
      statusText: 'Profile deleted from PC'
    }));
  },

  deleteFromDevice: async () => {
    const { selectedProfile, profiles } = get();
    if (!selectedProfile) return;
    const sync = useDeviceStore.getState().syncService;
    if (!sync) return;
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
      set((s) => ({ ...s, statusText: 'Could not delete from device' }));
    }
  },

  setActiveProfileOnDevice: async () => {
    const { selectedProfile } = get();
    if (!selectedProfile) return;
    const sync = useDeviceStore.getState().syncService;
    if (!sync) return;
    const ok = await sync.setActiveProfile(selectedProfile.id);
    if (ok) set((s) => ({ ...s, activeProfileId: selectedProfile.id, statusText: `Activated: ${selectedProfile.name}` }));
    else set((s) => ({ ...s, statusText: 'Failed to activate' }));
  },

  setStatus: (statusText) => set((s) => ({ ...s, statusText })),

  addCombo: () => {
    const { editingProfile } = get();
    if (!editingProfile) return;
    const combos = editingProfile.combos ?? [];
    editingProfile.combos = [...combos, { key1: 0, key2: 1 }];
    set((s) => ({ ...s, editingProfile: { ...editingProfile } }));
  },

  removeCombo: (combo) => {
    const { editingProfile } = get();
    if (!editingProfile?.combos) return;
    editingProfile.combos = editingProfile.combos.filter((c) => c !== combo);
    set((s) => ({ ...s, editingProfile: { ...editingProfile } }));
  },

  updateComboKeys: (combo, key1, key2) => {
    combo.key1 = key1;
    combo.key2 = key2;
    set((s) => ({ ...s, editingProfile: s.editingProfile ? { ...s.editingProfile } : null }));
  },

  getCombos: () => get().editingProfile?.combos ?? [],

  applyEncoderPreset: (encoderIndex, preset) => {
    const { editingProfile } = get();
    if (!editingProfile?.encoders?.[encoderIndex]) return;
    const enc = editingProfile.encoders[encoderIndex];
    enc.cwAction = enc.cwAction ?? { type: 'none' };
    enc.ccwAction = enc.ccwAction ?? { type: 'none' };
    enc.pressAction = enc.pressAction ?? { type: 'none' };
    switch (preset) {
      case 'Volume':
        enc.cwAction = { type: 'volume', mediaFunction: 0 };
        enc.ccwAction = { type: 'volume', mediaFunction: 1 };
        enc.pressAction = { type: 'media', mediaFunction: 2 };
        break;
      case 'Scroll':
        enc.cwAction = { type: 'scrollV', value: -1 };
        enc.ccwAction = { type: 'scrollV', value: 1 };
        enc.pressAction = { type: 'none' };
        break;
      case 'Zoom':
        enc.cwAction = { type: 'hotkey', modifiers: 0x01, key: 0x35 };
        enc.ccwAction = { type: 'hotkey', modifiers: 0x01, key: 0x36 };
        enc.pressAction = { type: 'none' };
        break;
      case 'Media':
        enc.cwAction = { type: 'media', mediaFunction: 4 };
        enc.ccwAction = { type: 'media', mediaFunction: 5 };
        enc.pressAction = { type: 'media', mediaFunction: 3 };
        break;
      default:
        enc.cwAction = enc.ccwAction = enc.pressAction = { type: 'none' };
    }
    set((s) => ({ ...s, editingProfile: { ...editingProfile } }));
  }
}));
