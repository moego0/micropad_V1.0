import { useState } from 'react';
import type { KeyConfig } from '../models/types';
import { ActionType, MediaFunction, MouseAction } from '../models/types';

const KEY_NAMES: Record<string, number> = {
  A: 0x04, B: 0x05, C: 0x06, D: 0x07, E: 0x08, F: 0x09, G: 0x0a, H: 0x0b, I: 0x0c, J: 0x0d, K: 0x0e, L: 0x0f,
  M: 0x10, N: 0x11, O: 0x12, P: 0x13, Q: 0x14, R: 0x15, S: 0x16, T: 0x17, U: 0x18, V: 0x19, W: 0x1a, X: 0x1b, Y: 0x1c, Z: 0x1d,
  '1': 0x1e, '2': 0x1f, '3': 0x20, '4': 0x21, '5': 0x22, '6': 0x23, '7': 0x24, '8': 0x25, '9': 0x26, '0': 0x27,
  Enter: 0x28, Esc: 0x29, Backspace: 0x2a, Tab: 0x2b, Space: 0x2c,
  F1: 0x3a, F2: 0x3b, F3: 0x3c, F4: 0x3d, F5: 0x3e, F6: 0x3f, F7: 0x40, F8: 0x41, F9: 0x42, F10: 0x43, F11: 0x44, F12: 0x45,
  Insert: 0x49, Delete: 0x4c, Home: 0x4a, End: 0x4d, PageUp: 0x4b, PageDown: 0x4e, Left: 0x50, Up: 0x52, Right: 0x4f, Down: 0x51
};

interface ActionEditModalProps {
  keyConfig: KeyConfig;
  keyIndex: number;
  supportedActions?: number[];
  supportsMacros: boolean;
  maxProfiles?: number;
  onSave: (config: Partial<KeyConfig>) => void;
  onClose: () => void;
}

interface TypeOption { value: ActionType; label: string; description: string }

const ALL_TYPE_OPTIONS: TypeOption[] = [
  { value: ActionType.None, label: 'Not Assigned', description: 'No action' },
  { value: ActionType.Hotkey, label: 'Keyboard Shortcut', description: 'Send a key combination (e.g. Ctrl+C)' },
  { value: ActionType.Text, label: 'Type Text', description: 'Type a text string (letters, numbers, basic punctuation)' },
  { value: ActionType.Media, label: 'Media Control', description: 'Volume, play/pause, next/previous track' },
  { value: ActionType.Mouse, label: 'Mouse Action', description: 'Click, right-click, or scroll' },
  { value: ActionType.Profile, label: 'Switch Profile', description: 'Switch to a different profile on the device' },
  { value: ActionType.Macro, label: 'Run Macro', description: 'Execute a sequence of actions with delays' },
];

export default function ActionEditModal({ keyConfig, keyIndex, supportedActions, supportsMacros, maxProfiles = 8, onSave, onClose }: ActionEditModalProps) {
  const [type, setType] = useState<ActionType>(keyConfig.type);
  const [modifiers, setModifiers] = useState({ ctrl: !!(keyConfig.modifiers & 0x01), shift: !!(keyConfig.modifiers & 0x02), alt: !!(keyConfig.modifiers & 0x04), win: !!(keyConfig.modifiers & 0x08) });
  const [key, setKey] = useState(stringKey(keyConfig.key));
  const [text, setText] = useState(keyConfig.text ?? '');
  const [mediaFunction, setMediaFunction] = useState(keyConfig.function ?? 0);
  const [mouseAction, setMouseAction] = useState(keyConfig.action ?? 0);
  const [profileId, setProfileId] = useState(String(keyConfig.profileId ?? 0));

  // Filter action types based on device capabilities
  const typeOptions = ALL_TYPE_OPTIONS.filter((opt) => {
    if (opt.value === ActionType.None) return true;
    if (opt.value === ActionType.Macro && !supportsMacros) return false;
    if (supportedActions && supportedActions.length > 0) {
      return supportedActions.includes(opt.value);
    }
    return true;
  });

  function stringKey(code: number): string {
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
      text: type === ActionType.Text ? text : undefined,
      function: mediaFunction,
      action: mouseAction,
      value: 0,
      profileId: parseInt(profileId, 10) || 0,
    };
    onSave(config);
    onClose();
  };

  const handleClear = () => {
    onSave({
      type: ActionType.None,
      modifiers: 0,
      key: 0,
      text: undefined,
      function: 0,
      action: 0,
      value: 0,
      profileId: 0,
    });
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm" onClick={onClose}>
      <div className="bg-surface-secondary border border-border rounded-2xl shadow-2xl w-full max-w-md mx-4 p-6 animate-fade-in" onClick={(e) => e.stopPropagation()}>
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-lg font-semibold text-text-primary">Key {keyIndex + 1}</h2>
          <button onClick={onClose} className="text-text-tertiary hover:text-text-primary text-xl leading-none">&times;</button>
        </div>

        <label className="block text-text-secondary text-xs font-medium uppercase tracking-wide mb-1.5">Action type</label>
        <div className="space-y-1.5 mb-5">
          {typeOptions.map((opt) => (
            <label
              key={opt.value}
              className={`flex items-start gap-3 p-2.5 rounded-lg cursor-pointer border transition ${
                type === opt.value
                  ? 'border-brand-blue bg-brand-blue/5'
                  : 'border-transparent hover:bg-surface-tertiary'
              }`}
            >
              <input
                type="radio"
                name="actionType"
                checked={type === opt.value}
                onChange={() => setType(opt.value)}
                className="mt-0.5"
              />
              <div>
                <p className="text-sm font-medium text-text-primary">{opt.label}</p>
                <p className="text-xs text-text-tertiary">{opt.description}</p>
              </div>
            </label>
          ))}
        </div>

        {type === ActionType.Hotkey && (
          <div className="space-y-3 mb-5">
            <div className="flex gap-3 flex-wrap">
              {(['ctrl', 'shift', 'alt', 'win'] as const).map((m) => (
                <label key={m} className={`flex items-center gap-1.5 cursor-pointer px-3 py-1.5 rounded-lg border text-sm transition ${
                  modifiers[m] ? 'border-brand-blue bg-brand-blue/10 text-brand-blue' : 'border-border text-text-secondary'
                }`}>
                  <input type="checkbox" checked={modifiers[m]} onChange={(e) => setModifiers((s) => ({ ...s, [m]: e.target.checked }))} className="sr-only" />
                  {m.charAt(0).toUpperCase() + m.slice(1)}
                </label>
              ))}
            </div>
            <select value={key} onChange={(e) => setKey(e.target.value)} className="w-full bg-surface-input text-text-primary rounded-lg px-3 py-2 border border-border">
              {Object.keys(KEY_NAMES).sort().map((k) => (
                <option key={k} value={k}>{k}</option>
              ))}
            </select>
          </div>
        )}

        {type === ActionType.Text && (
          <div className="mb-5">
            <label className="block text-text-secondary text-xs font-medium mb-1.5">Text to type</label>
            <textarea
              value={text}
              onChange={(e) => setText(e.target.value)}
              maxLength={127}
              rows={3}
              placeholder="Type your text here…"
              className="w-full bg-surface-input text-text-primary rounded-lg px-3 py-2 border border-border resize-none"
            />
            <p className="text-xs text-text-tertiary mt-1">Supports letters, numbers, spaces, and basic punctuation. {127 - text.length} characters remaining.</p>
          </div>
        )}

        {type === ActionType.Media && (
          <div className="mb-5">
            <label className="block text-text-secondary text-xs font-medium mb-1.5">Media function</label>
            <select value={mediaFunction} onChange={(e) => setMediaFunction(Number(e.target.value))} className="w-full bg-surface-input text-text-primary rounded-lg px-3 py-2 border border-border">
              <option value={MediaFunction.VolumeUp}>Volume Up</option>
              <option value={MediaFunction.VolumeDown}>Volume Down</option>
              <option value={MediaFunction.Mute}>Mute</option>
              <option value={MediaFunction.PlayPause}>Play / Pause</option>
              <option value={MediaFunction.Next}>Next Track</option>
              <option value={MediaFunction.Prev}>Previous Track</option>
              <option value={MediaFunction.Stop}>Stop</option>
            </select>
          </div>
        )}

        {type === ActionType.Mouse && (
          <div className="mb-5">
            <label className="block text-text-secondary text-xs font-medium mb-1.5">Mouse action</label>
            <select value={mouseAction} onChange={(e) => setMouseAction(Number(e.target.value))} className="w-full bg-surface-input text-text-primary rounded-lg px-3 py-2 border border-border">
              <option value={MouseAction.Click}>Left Click</option>
              <option value={MouseAction.RightClick}>Right Click</option>
              <option value={MouseAction.MiddleClick}>Middle Click</option>
              <option value={MouseAction.ScrollUp}>Scroll Up</option>
              <option value={MouseAction.ScrollDown}>Scroll Down</option>
            </select>
          </div>
        )}

        {type === ActionType.Profile && (
          <div className="mb-5">
            <label className="block text-text-secondary text-xs font-medium mb-1.5">Target profile ID</label>
            <input type="number" min={0} max={Math.max(0, maxProfiles - 1)} value={profileId} onChange={(e) => setProfileId(e.target.value)} className="w-full bg-surface-input text-text-primary rounded-lg px-3 py-2 border border-border" />
            <p className="text-xs text-text-tertiary mt-1">Profile ID 0-{Math.max(0, maxProfiles - 1)}. The device will switch to this profile when the key is pressed.</p>
          </div>
        )}

        {type === ActionType.Macro && (
          <div className="mb-5 rounded-lg bg-surface-tertiary/50 p-3 border border-border/50">
            <p className="text-sm text-text-secondary">
              Macros are created in the <strong>Macros</strong> tab. When you push this profile to the device, the macro steps will be embedded in the key assignment.
            </p>
            <p className="text-xs text-text-tertiary mt-2">
              Macros support delays, key presses, text typing, and media keys. Up to 16 steps per macro.
            </p>
          </div>
        )}

        <div className="flex items-center justify-between mt-6">
          <button onClick={handleClear} className="text-xs text-red-400 hover:underline">Clear action</button>
          <div className="flex gap-2">
            <button onClick={onClose} className="px-4 py-2 border border-border rounded-lg text-text-primary text-sm hover:bg-surface-tertiary transition">Cancel</button>
            <button onClick={handleSave} className="px-5 py-2 bg-brand-blue text-white rounded-lg text-sm font-medium shadow-sm hover:bg-brand-blue/90 transition">Save</button>
          </div>
        </div>
      </div>
    </div>
  );
}
