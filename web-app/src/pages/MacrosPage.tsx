import { useEffect, useState } from 'react';
import { useMacrosStore } from '../stores/macrosStore';
import type { MacroStepConfig } from '../models/types';

const STEP_LABELS: Record<number, string> = {
  0: 'None',
  1: 'Delay',
  2: 'Key Press',
  3: 'Type Text',
  4: 'Media Key',
};

function stepDisplay(s: MacroStepConfig): string {
  switch (s.stepType) {
    case 1: return `Delay ${s.delayMs ?? 0}ms`;
    case 2: return `Key: 0x${(s.key ?? 0).toString(16).toUpperCase()}${s.modifiers ? ` +mod` : ''}`;
    case 3: return `Text: ${(s.text ?? '').slice(0, 20)}${(s.text?.length ?? 0) > 20 ? '…' : ''}`;
    case 4: return `Media: ${s.mediaFunction ?? 0}`;
    default: return 'Unknown step';
  }
}

export default function MacrosPage() {
  const macroAssets = useMacrosStore((s) => s.macroAssets);
  const currentName = useMacrosStore((s) => s.currentName);
  const currentSteps = useMacrosStore((s) => s.currentSteps);
  const selectedStepIndex = useMacrosStore((s) => s.selectedStepIndex);
  const statusText = useMacrosStore((s) => s.statusText);

  const loadMacros = useMacrosStore((s) => s.loadMacros);
  const setCurrentName = useMacrosStore((s) => s.setCurrentName);
  const addStep = useMacrosStore((s) => s.addStep);
  const removeStep = useMacrosStore((s) => s.removeStep);
  const moveStep = useMacrosStore((s) => s.moveStep);
  const updateStep = useMacrosStore((s) => s.updateStep);
  const setSelectedStepIndex = useMacrosStore((s) => s.setSelectedStepIndex);
  const clearCurrent = useMacrosStore((s) => s.clearCurrent);
  const saveCurrent = useMacrosStore((s) => s.saveCurrent);
  const loadMacroIntoCurrent = useMacrosStore((s) => s.loadMacroIntoCurrent);
  const deleteMacro = useMacrosStore((s) => s.deleteMacro);

  const [searchText, setSearchText] = useState('');

  useEffect(() => { loadMacros(); }, [loadMacros]);

  const filteredMacros = macroAssets.filter((m) => {
    const q = searchText.trim().toLowerCase();
    if (!q) return true;
    return m.name.toLowerCase().includes(q) || m.tags.some((t) => t.toLowerCase().includes(q));
  });

  return (
    <div className="p-6 animate-fade-in">
      <h1 className="text-2xl font-bold text-text-primary mb-2">Macros</h1>
      <div className="h-1 w-12 bg-brand-blue rounded mb-4" />
      <p className="text-text-secondary mb-6 max-w-2xl">
        Build step-by-step macros with delays, key presses, and text. Assign them to keys in the Profiles tab.
        Macros are saved to your browser and embedded into the device when you push a profile.
      </p>

      <div className="flex gap-5 flex-wrap">
        {/* Library */}
        <div className="w-72 flex-shrink-0">
          <div className="rounded-xl border border-border bg-surface-secondary p-4">
            <h2 className="font-semibold text-text-primary mb-3 text-sm">Macro Library</h2>
            {macroAssets.length > 3 && (
              <input
                value={searchText}
                onChange={(e) => setSearchText(e.target.value)}
                placeholder="Search…"
                className="w-full bg-surface-input text-text-primary rounded-lg px-3 py-1.5 border border-border text-sm mb-3"
              />
            )}
            {filteredMacros.length === 0 ? (
              <div className="text-center py-8">
                <p className="text-text-tertiary text-sm mb-2">No macros yet</p>
                <p className="text-xs text-text-tertiary">Create one using the editor on the right.</p>
              </div>
            ) : (
              <ul className="space-y-1 max-h-80 overflow-auto">
                {filteredMacros.map((m) => (
                  <li
                    key={m.macroId}
                    className="flex items-center justify-between px-3 py-2 rounded-lg cursor-pointer text-text-secondary hover:bg-surface-tertiary hover:text-text-primary transition"
                  >
                    <span className="text-sm truncate" onClick={() => loadMacroIntoCurrent(m)}>{m.name}</span>
                    <button onClick={() => deleteMacro(m.macroId)} className="text-xs text-red-400 hover:text-red-300 ml-2 flex-shrink-0">&times;</button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>

        {/* Editor */}
        <div className="flex-1 min-w-0">
          <div className="rounded-xl border border-border bg-surface-secondary p-6">
            <h2 className="font-semibold text-text-primary mb-4 text-sm">Macro Editor</h2>

            <label className="block text-text-secondary text-xs font-medium mb-1">Name</label>
            <input
              value={currentName}
              onChange={(e) => setCurrentName(e.target.value)}
              placeholder="My Macro"
              className="w-full max-w-xs bg-surface-input text-text-primary rounded-lg px-3 py-2 border border-border mb-4"
            />

            <div className="flex flex-wrap gap-2 mb-4">
              <button onClick={() => addStep({ stepType: 1, delayMs: 100 })} className="px-3 py-1.5 border border-border rounded-lg text-sm text-text-primary hover:bg-surface-tertiary transition">
                + Delay
              </button>
              <button onClick={() => addStep({ stepType: 2, key: 0x04, modifiers: 0 })} className="px-3 py-1.5 border border-border rounded-lg text-sm text-text-primary hover:bg-surface-tertiary transition">
                + Key Press
              </button>
              <button onClick={() => addStep({ stepType: 3, text: '' })} className="px-3 py-1.5 border border-border rounded-lg text-sm text-text-primary hover:bg-surface-tertiary transition">
                + Text
              </button>
              <button onClick={() => addStep({ stepType: 4, mediaFunction: 0 })} className="px-3 py-1.5 border border-border rounded-lg text-sm text-text-primary hover:bg-surface-tertiary transition">
                + Media
              </button>
              <div className="flex-1" />
              <button onClick={() => saveCurrent()} disabled={currentSteps.length === 0} className="px-4 py-1.5 bg-brand-blue text-white rounded-lg text-sm font-medium disabled:opacity-40 transition">
                Save Macro
              </button>
              <button onClick={() => clearCurrent()} className="px-3 py-1.5 text-text-tertiary hover:text-text-primary text-sm transition">
                Clear
              </button>
            </div>

            <p className="text-xs text-text-tertiary mb-2">
              Steps ({currentSteps.length}/16) — {currentSteps.length === 0 ? 'add steps to build your macro' : 'click a step to edit it'}
            </p>

            {currentSteps.length === 0 ? (
              <div className="rounded-lg border border-dashed border-border py-8 text-center">
                <p className="text-text-tertiary text-sm">No steps yet</p>
                <p className="text-xs text-text-tertiary mt-1">Use the buttons above to add delay, key press, text, or media steps.</p>
              </div>
            ) : (
              <ul className="space-y-1.5">
                {currentSteps.map((s, i) => (
                  <li
                    key={i}
                    onClick={() => setSelectedStepIndex(i)}
                    className={`flex items-center justify-between rounded-lg border px-3 py-2.5 cursor-pointer transition ${
                      selectedStepIndex === i ? 'bg-brand-blue/5 border-brand-blue' : 'bg-surface-tertiary/50 border-border hover:border-border'
                    }`}
                  >
                    <div className="flex items-center gap-2 min-w-0">
                      <span className="text-xs text-text-tertiary font-mono w-5 text-right">{i + 1}</span>
                      <span className="text-sm text-text-primary truncate">{stepDisplay(s)}</span>
                    </div>
                    <div className="flex gap-1 flex-shrink-0">
                      <button onClick={(e) => { e.stopPropagation(); moveStep(i, Math.max(0, i - 1)); }} className="text-xs px-1.5 py-0.5 text-text-tertiary hover:text-text-primary">↑</button>
                      <button onClick={(e) => { e.stopPropagation(); moveStep(i, Math.min(currentSteps.length - 1, i + 1)); }} className="text-xs px-1.5 py-0.5 text-text-tertiary hover:text-text-primary">↓</button>
                      <button onClick={(e) => { e.stopPropagation(); removeStep(i); }} className="text-xs px-1.5 py-0.5 text-red-400 hover:text-red-300">✕</button>
                    </div>
                  </li>
                ))}
              </ul>
            )}

            {/* Step editor */}
            {selectedStepIndex >= 0 && currentSteps[selectedStepIndex] && (
              <div className="mt-4 p-4 rounded-lg border border-border bg-surface-tertiary/50">
                <p className="text-xs text-text-tertiary font-medium uppercase tracking-wide mb-2">
                  Edit Step {selectedStepIndex + 1}: {STEP_LABELS[currentSteps[selectedStepIndex].stepType] ?? 'Unknown'}
                </p>
                {currentSteps[selectedStepIndex].stepType === 1 && (
                  <div>
                    <label className="text-xs text-text-secondary">Delay (ms)</label>
                    <input
                      type="number"
                      min={1}
                      max={5000}
                      value={currentSteps[selectedStepIndex].delayMs ?? 100}
                      onChange={(e) => updateStep(selectedStepIndex, { delayMs: parseInt(e.target.value, 10) || 100 })}
                      className="w-32 bg-surface-input text-text-primary rounded-lg px-2 py-1.5 text-sm border border-border ml-2"
                    />
                  </div>
                )}
                {currentSteps[selectedStepIndex].stepType === 2 && (
                  <div className="space-y-2">
                    <div>
                      <label className="text-xs text-text-secondary">Key code (hex)</label>
                      <input
                        type="number"
                        min={0}
                        max={255}
                        value={currentSteps[selectedStepIndex].key ?? 0}
                        onChange={(e) => updateStep(selectedStepIndex, { key: parseInt(e.target.value, 10) || 0 })}
                        className="w-24 bg-surface-input text-text-primary rounded-lg px-2 py-1.5 text-sm border border-border ml-2"
                      />
                    </div>
                    <div>
                      <label className="text-xs text-text-secondary">Modifiers (bitmask)</label>
                      <input
                        type="number"
                        min={0}
                        max={15}
                        value={currentSteps[selectedStepIndex].modifiers ?? 0}
                        onChange={(e) => updateStep(selectedStepIndex, { modifiers: parseInt(e.target.value, 10) || 0 })}
                        className="w-24 bg-surface-input text-text-primary rounded-lg px-2 py-1.5 text-sm border border-border ml-2"
                      />
                      <p className="text-[10px] text-text-tertiary mt-0.5">1=Ctrl, 2=Shift, 4=Alt, 8=Win</p>
                    </div>
                  </div>
                )}
                {currentSteps[selectedStepIndex].stepType === 3 && (
                  <div>
                    <label className="text-xs text-text-secondary">Text</label>
                    <textarea
                      value={currentSteps[selectedStepIndex].text ?? ''}
                      onChange={(e) => updateStep(selectedStepIndex, { text: e.target.value })}
                      maxLength={31}
                      rows={2}
                      className="w-full bg-surface-input text-text-primary rounded-lg px-2 py-1.5 text-sm border border-border mt-1 resize-none"
                    />
                    <p className="text-[10px] text-text-tertiary">Max 31 characters per step</p>
                  </div>
                )}
                {currentSteps[selectedStepIndex].stepType === 4 && (
                  <div>
                    <label className="text-xs text-text-secondary">Media function</label>
                    <select
                      value={currentSteps[selectedStepIndex].mediaFunction ?? 0}
                      onChange={(e) => updateStep(selectedStepIndex, { mediaFunction: parseInt(e.target.value, 10) })}
                      className="bg-surface-input text-text-primary rounded-lg px-2 py-1.5 text-sm border border-border ml-2"
                    >
                      <option value={0}>Volume Up</option>
                      <option value={1}>Volume Down</option>
                      <option value={2}>Mute</option>
                      <option value={3}>Play/Pause</option>
                      <option value={4}>Next Track</option>
                      <option value={5}>Previous Track</option>
                      <option value={6}>Stop</option>
                    </select>
                  </div>
                )}
              </div>
            )}

            {statusText && (
              <p className={`text-sm mt-4 ${statusText.includes('saved') || statusText.includes('Loaded') ? 'text-emerald-400' : 'text-text-secondary'}`}>
                {statusText}
              </p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
