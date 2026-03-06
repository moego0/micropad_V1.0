import { useState } from 'react';
import type { KeyConfig, MacroAsset } from '../models/types';
import { ActionType, MediaFunction, MouseAction } from '../models/types';

const KEY_NAMES: Record<string, number> = {
  A: 0x04, B: 0x05, C: 0x06, D: 0x07, E: 0x08, F: 0x09, G: 0x0a, H: 0x0b, I: 0x0c, J: 0x0d, K: 0x0e, L: 0x0f,
  M: 0x10, N: 0x11, O: 0x12, P: 0x13, Q: 0x14, R: 0x15, S: 0x16, T: 0x17, U: 0x18, V: 0x19, W: 0x1a, X: 0x1b, Y: 0x1c, Z: 0x1d,
  '1': 0x1e, '2': 0x1f, '3': 0x20, '4': 0x21, '5': 0x22, '6': 0x23, '7': 0x24, '8': 0x25, '9': 0x26, '0': 0x27,
  Enter: 0x28, Esc: 0x29, Backspace: 0x2a, Tab: 0x2b, Space: 0x2c,
  F1: 0x3a, F2: 0x3b, F3: 0x3c, F4: 0x3d, F5: 0x3e, F6: 0x3f, F7: 0x40, F8: 0x41, F9: 0x42, F10: 0x43, F11: 0x44, F12: 0x45,
  Insert: 0x49, Delete: 0x4c, Home: 0x4a, End: 0x4d, PageUp: 0x4b, PageDown: 0x4e, Left: 0x50, Up: 0x52, Right: 0x4f, Down: 0x51, Win: 0xe3
};
const TYPE_OPTIONS: { value: ActionType; label: string }[] = [
  { value: ActionType.None, label: 'Not Assigned' },
  { value: ActionType.Hotkey, label: 'Hotkey' },
  { value: ActionType.Text, label: 'Text' },
  { value: ActionType.Media, label: 'Media Key' },
  { value: ActionType.Mouse, label: 'Mouse' },
  { value: ActionType.Profile, label: 'Switch Profile' },
  { value: ActionType.App, label: 'Launch App' },
  { value: ActionType.Url, label: 'Open URL' },
  { value: ActionType.Macro, label: 'Macro' }
];

interface ActionEditModalProps {
  keyConfig: KeyConfig;
  keyIndex: number;
  macroAssets: MacroAsset[];
  onSave: (config: Partial<KeyConfig>) => void;
  onClose: () => void;
}

export default function ActionEditModal ({ keyConfig, keyIndex, macroAssets, onSave, onClose }: ActionEditModalProps) {
  const [type, setType] = useState<ActionType>(keyConfig.type);
  const [modifiers, setModifiers] = useState({ ctrl: !!(keyConfig.modifiers & 0x01), shift: !!(keyConfig.modifiers & 0x02), alt: !!(keyConfig.modifiers & 0x04), win: !!(keyConfig.modifiers & 0x08) });
  const [key, setKey] = useState(stringKey(keyConfig.key));
  const [text, setText] = useState(keyConfig.text ?? '');
  const [mediaFunction, setMediaFunction] = useState(keyConfig.function ?? 0);
  const [mouseAction, setMouseAction] = useState(keyConfig.action ?? 0);
  const [profileId, setProfileId] = useState(String(keyConfig.profileId ?? 0));
  const [appPath, setAppPath] = useState(keyConfig.AppPath ?? '');
  const [url, setUrl] = useState(keyConfig.url ?? '');
  const [macroId, setMacroId] = useState(keyConfig.macroId ?? '');
  const [embedMacro, setEmbedMacro] = useState(!!(keyConfig.macroSnapshot && keyConfig.macroSnapshot.length > 0));

  function stringKey (code: number): string {
    const entry = Object.entries(KEY_NAMES).find(([, v]) => v === code);
    return entry ? entry[0] : 'A';
  }

  const handleSave = () => {
    let mod = 0;
    if (modifiers.ctrl) mod |= 0x01;
    if (modifiers.shift) mod |= 0x02;
    if (modifiers.alt) mod |= 0x04;
    if (modifiers.win) mod |= 0x08;
    const keyCode = KEY_NAMES[key] ?? 0x04;

    const config: Partial<KeyConfig> = {
      type,
      modifiers: mod,
      key: keyCode,
      text: text || undefined,
      function: mediaFunction,
      action: mouseAction,
      value: 0,
      profileId: parseInt(profileId, 10) || 0,
      AppPath: appPath || undefined,
      url: url || undefined,
      macroId: macroId || undefined,
      macroSnapshot: embedMacro && macroId ? (macroAssets.find((m) => m.macroId === macroId)?.steps?.length ? macroAssets.find((m) => m.macroId === macroId)!.steps : undefined) : undefined
    };
    onSave(config);
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" onClick={onClose}>
      <div className="bg-surface-secondary border border-border rounded-lg shadow-xl w-full max-w-md mx-4 p-6" onClick={(e) => e.stopPropagation()}>
        <h2 className="text-lg font-semibold text-text-primary mb-4">Configure Key K{keyIndex + 1}</h2>

        <label className="block text-text-secondary text-sm mb-1">Action type</label>
        <select
          value={type}
          onChange={(e) => setType(Number(e.target.value))}
          className="w-full bg-surface-input text-text-primary rounded px-3 py-2 border border-border mb-4"
        >
          {TYPE_OPTIONS.map((opt) => (
            <option key={opt.value} value={opt.value}>{opt.label}</option>
          ))}
        </select>

        {type === ActionType.Hotkey && (
          <div className="space-y-2 mb-4">
            <div className="flex gap-2 flex-wrap">
              {(['ctrl', 'shift', 'alt', 'win'] as const).map((m) => (
                <label key={m} className="flex items-center gap-1 cursor-pointer text-text-primary text-sm">
                  <input type="checkbox" checked={modifiers[m]} onChange={(e) => setModifiers((s) => ({ ...s, [m]: e.target.checked }))} />
                  {m}
                </label>
              ))}
            </div>
            <select value={key} onChange={(e) => setKey(e.target.value)} className="w-full bg-surface-input text-text-primary rounded px-3 py-2 border border-border">
              {Object.keys(KEY_NAMES).sort().map((k) => (
                <option key={k} value={k}>{k}</option>
              ))}
            </select>
          </div>
        )}

        {type === ActionType.Text && (
          <div className="mb-4">
            <label className="block text-text-secondary text-sm mb-1">Text to type</label>
            <textarea value={text} onChange={(e) => setText(e.target.value)} rows={3} className="w-full bg-surface-input text-text-primary rounded px-3 py-2 border border-border" />
          </div>
        )}

        {type === ActionType.Media && (
          <div className="mb-4">
            <select value={mediaFunction} onChange={(e) => setMediaFunction(Number(e.target.value))} className="w-full bg-surface-input text-text-primary rounded px-3 py-2 border border-border">
              {Object.entries(MediaFunction).filter(([k]) => isNaN(Number(k))).map(([label, v]) => (
                <option key={v} value={v}>{label}</option>
              ))}
            </select>
          </div>
        )}

        {type === ActionType.Mouse && (
          <div className="mb-4">
            <select value={mouseAction} onChange={(e) => setMouseAction(Number(e.target.value))} className="w-full bg-surface-input text-text-primary rounded px-3 py-2 border border-border">
              {Object.entries(MouseAction).filter(([k]) => isNaN(Number(k))).map(([label, v]) => (
                <option key={v} value={v}>{label}</option>
              ))}
            </select>
          </div>
        )}

        {type === ActionType.Profile && (
          <div className="mb-4">
            <label className="block text-text-secondary text-sm mb-1">Profile ID</label>
            <input type="number" min={0} value={profileId} onChange={(e) => setProfileId(e.target.value)} className="w-full bg-surface-input text-text-primary rounded px-3 py-2 border border-border" />
          </div>
        )}

        {type === ActionType.App && (
          <div className="mb-4">
            <label className="block text-text-secondary text-sm mb-1">Application path</label>
            <input type="text" value={appPath} onChange={(e) => setAppPath(e.target.value)} placeholder="e.g. C:\Program Files\..." className="w-full bg-surface-input text-text-primary rounded px-3 py-2 border border-border" />
          </div>
        )}

        {type === ActionType.Url && (
          <div className="mb-4">
            <label className="block text-text-secondary text-sm mb-1">URL</label>
            <input type="url" value={url} onChange={(e) => setUrl(e.target.value)} placeholder="https://..." className="w-full bg-surface-input text-text-primary rounded px-3 py-2 border border-border" />
          </div>
        )}

        {type === ActionType.Macro && (
          <div className="mb-4">
            <label className="block text-text-secondary text-sm mb-1">Macro</label>
            <select value={macroId} onChange={(e) => setMacroId(e.target.value)} className="w-full bg-surface-input text-text-primary rounded px-3 py-2 border border-border">
              <option value="">(None)</option>
              {macroAssets.map((m) => (
                <option key={m.macroId} value={m.macroId}>{m.name}</option>
              ))}
            </select>
            <label className="flex items-center gap-2 mt-2 text-text-primary text-sm">
              <input type="checkbox" checked={embedMacro} onChange={(e) => setEmbedMacro(e.target.checked)} />
              Embed copy (portable)
            </label>
          </div>
        )}

        <div className="flex justify-end gap-2 mt-6">
          <button onClick={onClose} className="px-4 py-2 border border-border rounded text-text-primary">Cancel</button>
          <button onClick={handleSave} className="px-4 py-2 bg-brand-blue text-white rounded">Save</button>
        </div>
      </div>
    </div>
  );
}
