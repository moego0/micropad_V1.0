import { useEffect, useState } from 'react';
import { useMacrosStore } from '../stores/macrosStore';
import type { MacroStep } from '../models/types';

export default function MacrosPage () {
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

  const [textToAdd, setTextToAdd] = useState('');
  const [searchText, setSearchText] = useState('');

  useEffect(() => {
    loadMacros();
  }, [loadMacros]);

  function stepDisplay (s: MacroStep): string {
    if (s.action === 'delay') return `Delay: ${s.ms ?? 0}ms`;
    if (s.action === 'textType') return `Text: ${(s.text ?? '').slice(0, 20)}${(s.text?.length ?? 0) > 20 ? '…' : ''}`;
    if (s.action === 'keyPress' || s.action === 'keyDown' || s.action === 'keyUp') return `${s.action}: ${s.key ?? ''}`;
    return `${s.action}`;
  }

  const filteredMacros = macroAssets.filter((m) => {
    const q = searchText.trim().toLowerCase();
    if (!q) return true;
    return (
      m.name.toLowerCase().includes(q) ||
      m.tags.some((t) => t.toLowerCase().includes(q))
    );
  });

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-text-primary mb-2">Macros</h1>
      <div className="h-1 w-12 bg-brand-blue rounded mb-4" />
      <p className="text-text-secondary mb-6">Build and save macros, then assign them to keys in Profiles.</p>

      <div className="flex gap-6 flex-wrap">
        <div className="w-80 rounded-lg border border-border bg-surface-secondary p-4">
          <h2 className="font-semibold text-text-primary mb-3">Macro library</h2>
          <input
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            placeholder="Search macros or tags…"
            className="w-full bg-surface-input text-text-primary rounded px-3 py-1.5 border border-border text-sm mb-3"
          />
          <ul className="space-y-1 max-h-64 overflow-auto">
            {filteredMacros.map((m) => (
              <li
                key={m.macroId}
                onClick={() => loadMacroIntoCurrent(m)}
                className="px-3 py-2 rounded cursor-pointer text-text-secondary hover:bg-surface-tertiary hover:text-text-primary"
              >
                {m.name}
              </li>
            ))}
          </ul>
        </div>

        <div className="flex-1 min-w-0 rounded-lg border border-border bg-surface-secondary p-6">
          <h2 className="font-semibold text-text-primary mb-4">Current macro</h2>
          <label className="block text-text-secondary text-sm mb-1">Name</label>
          <input
            value={currentName}
            onChange={(e) => setCurrentName(e.target.value)}
            className="w-full max-w-xs bg-surface-input text-text-primary rounded px-3 py-2 border border-border mb-4"
          />

          <div className="flex flex-wrap gap-2 mb-4">
            <button onClick={() => addStep({ action: 'delay', ms: 100 })} className="px-3 py-1.5 border border-border rounded text-sm text-text-primary">
              Add delay
            </button>
            <button onClick={() => addStep({ action: 'textType', text: textToAdd || ' ' })} className="px-3 py-1.5 border border-border rounded text-sm text-text-primary">
              Add text
            </button>
            <input
              value={textToAdd}
              onChange={(e) => setTextToAdd(e.target.value)}
              placeholder="Text to add"
              className="bg-surface-input text-text-primary rounded px-2 py-1 border border-border w-40 text-sm"
            />
            <button onClick={() => saveCurrent()} className="px-3 py-1.5 bg-green-600 text-white rounded text-sm">
              Save macro
            </button>
            <button onClick={() => clearCurrent()} className="px-3 py-1.5 border border-border rounded text-sm text-text-primary">
              Clear
            </button>
          </div>

          <p className="text-sm text-text-secondary mb-2">Steps</p>
          {currentSteps.length === 0 ? (
            <p className="text-text-tertiary text-sm">No steps. Add delay or text, or load a macro from the library.</p>
          ) : (
            <ul className="space-y-2">
              {currentSteps.map((s, i) => (
                <li
                  key={i}
                  className={`flex items-center justify-between rounded border border-border px-3 py-2 ${
                    selectedStepIndex === i ? 'bg-surface-input border-brand-blue' : 'bg-surface-tertiary'
                  }`}
                >
                  <span className="text-sm text-text-primary" onClick={() => setSelectedStepIndex(i)}>
                    {stepDisplay(s)}
                  </span>
                  <div className="flex gap-1">
                    <button onClick={() => moveStep(i, Math.max(0, i - 1))} className="text-xs px-1 text-text-secondary">↑</button>
                    <button onClick={() => moveStep(i, Math.min(currentSteps.length - 1, i + 1))} className="text-xs px-1 text-text-secondary">↓</button>
                    <button onClick={() => removeStep(i)} className="text-xs px-1 text-red-400">✕</button>
                  </div>
                </li>
              ))}
            </ul>
          )}

          {selectedStepIndex >= 0 && currentSteps[selectedStepIndex] && (
            <div className="mt-4 p-3 rounded border border-border bg-surface-tertiary">
              <p className="text-xs text-text-secondary mb-2">Edit step</p>
              {currentSteps[selectedStepIndex].action === 'delay' && (
                <input
                  type="number"
                  value={currentSteps[selectedStepIndex].ms ?? 0}
                  onChange={(e) => updateStep(selectedStepIndex, { ms: parseInt(e.target.value, 10) || 0 })}
                  className="w-24 bg-surface-input text-text-primary rounded px-2 py-1 text-sm"
                />
              )}
              {currentSteps[selectedStepIndex].action === 'textType' && (
                <textarea
                  value={currentSteps[selectedStepIndex].text ?? ''}
                  onChange={(e) => updateStep(selectedStepIndex, { text: e.target.value })}
                  rows={2}
                  className="w-full bg-surface-input text-text-primary rounded px-2 py-1 text-sm"
                />
              )}
            </div>
          )}

          <p className="text-sm text-text-secondary mt-4">{statusText}</p>
        </div>
      </div>
    </div>
  );
}
