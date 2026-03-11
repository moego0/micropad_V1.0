import { create } from 'zustand';
import type { MacroAsset, MacroStepConfig } from '../models/types';
import * as macroStorage from '../storage/macroStorage';

interface MacrosStore {
  macroAssets: MacroAsset[];
  currentName: string;
  currentSteps: MacroStepConfig[];
  selectedStepIndex: number;
  statusText: string;
  loadMacros: () => Promise<void>;
  setCurrentName: (name: string) => void;
  setCurrentSteps: (steps: MacroStepConfig[]) => void;
  addStep: (step: MacroStepConfig) => void;
  removeStep: (index: number) => void;
  moveStep: (from: number, to: number) => void;
  updateStep: (index: number, step: Partial<MacroStepConfig>) => void;
  setSelectedStepIndex: (i: number) => void;
  clearCurrent: () => void;
  saveCurrent: () => Promise<void>;
  loadMacroIntoCurrent: (asset: MacroAsset) => void;
  deleteMacro: (macroId: string) => Promise<void>;
  setStatus: (s: string) => void;
}

export const useMacrosStore = create<MacrosStore>((set, get) => ({
  macroAssets: [],
  currentName: '',
  currentSteps: [],
  selectedStepIndex: -1,
  statusText: '',

  loadMacros: async () => {
    const list = await macroStorage.getAllMacros();
    set((s) => ({ ...s, macroAssets: list }));
  },

  setCurrentName: (currentName) => set((s) => ({ ...s, currentName })),
  setCurrentSteps: (currentSteps) => set((s) => ({ ...s, currentSteps })),
  setSelectedStepIndex: (selectedStepIndex) => set((s) => ({ ...s, selectedStepIndex })),
  setStatus: (statusText) => set((s) => ({ ...s, statusText })),

  addStep: (step) => {
    set((s) => ({ ...s, currentSteps: [...s.currentSteps, step] }));
  },

  removeStep: (index) => {
    set((s) => ({
      ...s,
      currentSteps: s.currentSteps.filter((_, i) => i !== index),
      selectedStepIndex: s.selectedStepIndex === index ? -1 : s.selectedStepIndex > index ? s.selectedStepIndex - 1 : s.selectedStepIndex
    }));
  },

  moveStep: (from, to) => {
    const { currentSteps } = get();
    if (from < 0 || from >= currentSteps.length || to < 0 || to >= currentSteps.length) return;
    const arr = [...currentSteps];
    const [removed] = arr.splice(from, 1);
    arr.splice(to, 0, removed);
    set((s) => ({ ...s, currentSteps: arr }));
  },

  updateStep: (index, patch) => {
    set((s) => {
      const steps = [...s.currentSteps];
      if (index >= 0 && index < steps.length) steps[index] = { ...steps[index], ...patch };
      return { ...s, currentSteps: steps };
    });
  },

  clearCurrent: () => {
    set((s) => ({ ...s, currentName: '', currentSteps: [], selectedStepIndex: -1, statusText: '' }));
  },

  saveCurrent: async () => {
    const { currentName, currentSteps } = get();
    const name = currentName.trim() || 'Unnamed Macro';
    if (currentSteps.length === 0) {
      set((s) => ({ ...s, statusText: 'Add at least one step before saving.' }));
      return;
    }
    const asset: MacroAsset = {
      macroId: crypto.randomUUID().replace(/-/g, ''),
      name,
      tags: [],
      steps: currentSteps.map((s) => ({ ...s })),
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      version: 1
    };
    await macroStorage.saveMacro(asset);
    const list = await macroStorage.getAllMacros();
    set((s) => ({ ...s, macroAssets: list, statusText: `"${name}" saved to library` }));
  },

  loadMacroIntoCurrent: (asset) => {
    set((s) => ({
      ...s,
      currentName: asset.name,
      currentSteps: asset.steps.map((x) => ({ ...x })),
      selectedStepIndex: -1,
      statusText: `Loaded "${asset.name}"`
    }));
  },

  deleteMacro: async (macroId: string) => {
    await macroStorage.deleteMacro(macroId);
    const list = await macroStorage.getAllMacros();
    set((s) => ({ ...s, macroAssets: list, statusText: 'Macro deleted' }));
  }
}));
