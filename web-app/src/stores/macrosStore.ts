import { create } from 'zustand';
import type { MacroAsset, MacroStep } from '../models/types';
import * as macroStorage from '../storage/macroStorage';

interface MacrosStore {
  macroAssets: MacroAsset[];
  currentName: string;
  currentSteps: MacroStep[];
  selectedStepIndex: number;
  statusText: string;
  loadMacros: () => Promise<void>;
  setCurrentName: (name: string) => void;
  setCurrentSteps: (steps: MacroStep[]) => void;
  addStep: (step: MacroStep) => void;
  removeStep: (index: number) => void;
  moveStep: (from: number, to: number) => void;
  updateStep: (index: number, step: Partial<MacroStep>) => void;
  setSelectedStepIndex: (i: number) => void;
  clearCurrent: () => void;
  saveCurrent: () => Promise<void>;
  loadMacroIntoCurrent: (asset: MacroAsset) => void;
  setStatus: (s: string) => void;
}

export const useMacrosStore = create<MacrosStore>((set, get) => ({
  macroAssets: [],
  currentName: 'New Macro',
  currentSteps: [],
  selectedStepIndex: -1,
  statusText: 'Create a macro or add steps.',

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
    set((s) => ({ ...s, currentName: 'New Macro', currentSteps: [], selectedStepIndex: -1, statusText: 'Macro cleared.' }));
  },

  saveCurrent: async () => {
    const { currentName, currentSteps } = get();
    const name = currentName.trim() || 'Unnamed';
    if (currentSteps.length === 0) {
      set((s) => ({ ...s, statusText: 'Add steps first.' }));
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
    set((s) => ({ ...s, macroAssets: list, statusText: `Saved macro '${name}'.` }));
  },

  loadMacroIntoCurrent: (asset) => {
    set((s) => ({
      ...s,
      currentName: asset.name,
      currentSteps: asset.steps.map((x) => ({ ...x })),
      selectedStepIndex: -1,
      statusText: `Loaded '${asset.name}'`
    }));
  }
}));
