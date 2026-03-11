import { useEffect, useState } from 'react';
import { useProfilesStore } from '../stores/profilesStore';
import { useDeviceStore } from '../stores/deviceStore';
import { ActionType, MediaFunction, MouseAction } from '../models/types';
import type { KeyConfig, EncoderActionConfig } from '../models/types';
import ActionEditModal from '../components/ActionEditModal';

const KEY_COUNT = 12;

function actionLabel(config: KeyConfig): string {
  switch (config.type) {
    case ActionType.None: return '';
    case ActionType.Hotkey: {
      const mods: string[] = [];
      if (config.modifiers & 0x01) mods.push('Ctrl');
      if (config.modifiers & 0x02) mods.push('Shift');
      if (config.modifiers & 0x04) mods.push('Alt');
      if (config.modifiers & 0x08) mods.push('Win');
      const keyName = KEY_CODE_NAMES[config.key] ?? `0x${config.key.toString(16)}`;
      return mods.length > 0 ? `${mods.join('+')}+${keyName}` : keyName;
    }
    case ActionType.Text: return config.text?.slice(0, 16) || 'Text';
    case ActionType.Media: return MEDIA_LABELS[config.function] ?? 'Media';
    case ActionType.Mouse: return MOUSE_LABELS[config.action] ?? 'Mouse';
    case ActionType.Profile: return `Profile ${config.profileId}`;
    case ActionType.Macro: return 'Macro';
    default: return 'Unknown';
  }
}

function actionTypeLabel(type: ActionType): string {
  switch (type) {
    case ActionType.None: return 'Not assigned';
    case ActionType.Hotkey: return 'Hotkey';
    case ActionType.Text: return 'Text';
    case ActionType.Media: return 'Media';
    case ActionType.Mouse: return 'Mouse';
    case ActionType.Profile: return 'Profile';
    case ActionType.Macro: return 'Macro';
    default: return '';
  }
}

function encoderActionLabel(action?: EncoderActionConfig): string {
  if (!action || action.type === ActionType.None) return 'None';
  if (action.type === ActionType.Media) return MEDIA_LABELS[action.function ?? 0] ?? 'Media';
  if (action.type === ActionType.Mouse) return MOUSE_LABELS[action.action ?? 0] ?? 'Mouse';
  if (action.type === ActionType.Hotkey) return 'Hotkey';
  return 'Action';
}

const MEDIA_LABELS: Record<number, string> = {
  [MediaFunction.VolumeUp]: 'Volume Up',
  [MediaFunction.VolumeDown]: 'Volume Down',
  [MediaFunction.Mute]: 'Mute',
  [MediaFunction.PlayPause]: 'Play/Pause',
  [MediaFunction.Next]: 'Next Track',
  [MediaFunction.Prev]: 'Previous Track',
  [MediaFunction.Stop]: 'Stop',
};

const MOUSE_LABELS: Record<number, string> = {
  [MouseAction.Click]: 'Click',
  [MouseAction.RightClick]: 'Right Click',
  [MouseAction.MiddleClick]: 'Middle Click',
  [MouseAction.ScrollUp]: 'Scroll Up',
  [MouseAction.ScrollDown]: 'Scroll Down',
};

const KEY_CODE_NAMES: Record<number, string> = {
  0x04: 'A', 0x05: 'B', 0x06: 'C', 0x07: 'D', 0x08: 'E', 0x09: 'F', 0x0a: 'G', 0x0b: 'H',
  0x0c: 'I', 0x0d: 'J', 0x0e: 'K', 0x0f: 'L', 0x10: 'M', 0x11: 'N', 0x12: 'O', 0x13: 'P',
  0x14: 'Q', 0x15: 'R', 0x16: 'S', 0x17: 'T', 0x18: 'U', 0x19: 'V', 0x1a: 'W', 0x1b: 'X',
  0x1c: 'Y', 0x1d: 'Z', 0x1e: '1', 0x1f: '2', 0x20: '3', 0x21: '4', 0x22: '5', 0x23: '6',
  0x24: '7', 0x25: '8', 0x26: '9', 0x27: '0', 0x28: 'Enter', 0x29: 'Esc', 0x2a: 'Backspace',
  0x2b: 'Tab', 0x2c: 'Space', 0x3a: 'F1', 0x3b: 'F2', 0x3c: 'F3', 0x3d: 'F4', 0x3e: 'F5',
  0x3f: 'F6', 0x40: 'F7', 0x41: 'F8', 0x42: 'F9', 0x43: 'F10', 0x44: 'F11', 0x45: 'F12',
  0x49: 'Insert', 0x4c: 'Delete', 0x4a: 'Home', 0x4d: 'End', 0x4b: 'PgUp', 0x4e: 'PgDn',
  0x50: 'Left', 0x52: 'Up', 0x4f: 'Right', 0x51: 'Down',
};

export default function ProfilesPage() {
  const [editKeyIndex, setEditKeyIndex] = useState<number | null>(null);
  const [searchText, setSearchText] = useState('');
  const [isRenaming, setIsRenaming] = useState(false);

  const profiles = useProfilesStore((s) => s.profiles);
  const selectedProfile = useProfilesStore((s) => s.selectedProfile);
  const editingProfile = useProfilesStore((s) => s.editingProfile);
  const activeProfileId = useProfilesStore((s) => s.activeProfileId);
  const selectedKeySlotIndex = useProfilesStore((s) => s.selectedKeySlotIndex);
  const deviceCapsText = useProfilesStore((s) => s.deviceCapsText);
  const statusText = useProfilesStore((s) => s.statusText);
  const isPushInProgress = useProfilesStore((s) => s.isPushInProgress);
  const pushStepText = useProfilesStore((s) => s.pushStepText);

  const loadProfiles = useProfilesStore((s) => s.loadProfiles);
  const selectProfile = useProfilesStore((s) => s.selectProfile);
  const setSelectedKeySlotIndex = useProfilesStore((s) => s.setSelectedKeySlotIndex);
  const getKeySlots = useProfilesStore((s) => s.getKeySlots);
  const updateKeyAt = useProfilesStore((s) => s.updateKeyAt);
  const pushToDevice = useProfilesStore((s) => s.pushToDevice);
  const pullFromDevice = useProfilesStore((s) => s.pullFromDevice);
  const saveLocally = useProfilesStore((s) => s.saveLocally);
  const createProfile = useProfilesStore((s) => s.createProfile);
  const duplicateProfile = useProfilesStore((s) => s.duplicateProfile);
  const deleteLocal = useProfilesStore((s) => s.deleteLocal);
  const deleteFromDevice = useProfilesStore((s) => s.deleteFromDevice);
  const setActiveProfileOnDevice = useProfilesStore((s) => s.setActiveProfileOnDevice);
  const applyEncoderPreset = useProfilesStore((s) => s.applyEncoderPreset);
  const renameProfile = useProfilesStore((s) => s.renameProfile);

  const isConnected = useDeviceStore((s) => ['configConnected', 'hidConnected', 'hidReady'].includes(s.connectionState));
  const deviceCaps = useDeviceStore((s) => s.deviceCaps);

  useEffect(() => { loadProfiles(); }, [loadProfiles]);

  const keySlots = editingProfile ? getKeySlots() : [];

  const filteredProfiles = profiles.filter((p) => {
    const q = searchText.trim().toLowerCase();
    if (!q) return true;
    return p.name.toLowerCase().includes(q) || String(p.id).includes(q);
  });

  const handleEditKey = (index: number) => {
    setEditKeyIndex(index);
    setSelectedKeySlotIndex(index);
  };

  const handleSaveKey = (config: Partial<KeyConfig>) => {
    if (editKeyIndex !== null) {
      updateKeyAt(editKeyIndex, config);
      setEditKeyIndex(null);
    }
  };

  return (
    <div className="p-6 animate-fade-in">
      <h1 className="text-2xl font-bold text-text-primary mb-2">Profiles</h1>
      <div className="h-1 w-12 bg-brand-blue rounded mb-4" />

      <div className="flex gap-5 flex-wrap">
        {/* Profile list sidebar */}
        <div className="w-64 flex-shrink-0 space-y-3">
          <div className="rounded-xl border border-border bg-surface-secondary p-4">
            <div className="flex items-center justify-between mb-3">
              <h2 className="font-semibold text-text-primary text-sm">Your Profiles</h2>
              <button
                onClick={() => loadProfiles()}
                className="text-xs text-brand-blue hover:underline"
              >
                Refresh
              </button>
            </div>

            {profiles.length > 3 && (
              <input
                value={searchText}
                onChange={(e) => setSearchText(e.target.value)}
                placeholder="Search…"
                className="w-full bg-surface-input text-text-primary rounded-lg px-3 py-1.5 border border-border text-sm mb-2"
              />
            )}

            {filteredProfiles.length === 0 ? (
              <div className="text-center py-6">
                <p className="text-text-tertiary text-sm mb-3">No profiles yet</p>
                <button onClick={() => createProfile()} className="px-4 py-2 bg-brand-blue text-white rounded-lg text-sm font-medium">
                  Create your first profile
                </button>
              </div>
            ) : (
              <ul className="space-y-1 max-h-80 overflow-auto">
                {filteredProfiles.map((p) => (
                  <li
                    key={p.id}
                    onClick={() => selectProfile(p)}
                    className={`px-3 py-2.5 rounded-lg cursor-pointer flex items-center justify-between transition ${
                      selectedProfile?.id === p.id
                        ? 'bg-brand-blue/10 border border-brand-blue/30 text-text-primary'
                        : 'text-text-secondary hover:bg-surface-tertiary'
                    }`}
                  >
                    <div className="min-w-0">
                      <p className="font-medium text-sm truncate">{p.name}</p>
                      <p className="text-xs text-text-tertiary">ID {p.id}</p>
                    </div>
                    {p.id === activeProfileId && (
                      <span className="flex-shrink-0 text-[10px] bg-emerald-500/20 text-emerald-400 px-1.5 py-0.5 rounded-full font-medium">
                        Active
                      </span>
                    )}
                  </li>
                ))}
              </ul>
            )}

            <div className="flex gap-2 mt-3 pt-3 border-t border-border/50">
              <button onClick={() => createProfile()} className="flex-1 py-1.5 bg-brand-blue text-white rounded-lg text-xs font-medium">
                New
              </button>
              <button onClick={() => duplicateProfile()} disabled={!editingProfile} className="flex-1 py-1.5 border border-border rounded-lg text-xs text-text-secondary disabled:opacity-30">
                Duplicate
              </button>
            </div>
          </div>

          {deviceCapsText && (
            <p className="text-xs text-text-tertiary px-1">{deviceCapsText}</p>
          )}
        </div>

        {/* Editor area */}
        <div className="flex-1 min-w-0">
          {!editingProfile ? (
            <div className="rounded-xl border border-border bg-surface-secondary p-12 text-center">
              <div className="text-4xl mb-4 opacity-20">⌨️</div>
              <p className="text-text-primary font-medium mb-2">Select a profile to edit</p>
              <p className="text-text-secondary text-sm mb-6">Choose a profile from the list, or create a new one.</p>
              <button onClick={() => createProfile()} className="px-5 py-2.5 bg-brand-blue text-white rounded-full font-medium text-sm">
                Create profile
              </button>
            </div>
          ) : (
            <div className="space-y-5">
              {/* Profile header */}
              <div className="rounded-xl border border-border bg-surface-secondary p-5">
                <div className="flex items-start justify-between gap-4 mb-4">
                  <div className="min-w-0">
                    {isRenaming ? (
                      <input
                        autoFocus
                        value={editingProfile.name}
                        onChange={(e) => renameProfile(e.target.value)}
                        onBlur={() => setIsRenaming(false)}
                        onKeyDown={(e) => e.key === 'Enter' && setIsRenaming(false)}
                        className="text-lg font-bold text-text-primary bg-surface-input rounded px-2 py-1 border border-brand-blue w-full max-w-xs"
                      />
                    ) : (
                      <h2
                        className="text-lg font-bold text-text-primary cursor-pointer hover:text-brand-blue transition"
                        onClick={() => setIsRenaming(true)}
                        title="Click to rename"
                      >
                        {editingProfile.name}
                        <span className="text-text-tertiary text-xs font-normal ml-2">click to rename</span>
                      </h2>
                    )}
                    <p className="text-xs text-text-tertiary mt-1">Profile ID {editingProfile.id} · Version {editingProfile.version}</p>
                  </div>
                  {editingProfile.id === activeProfileId && (
                    <span className="text-xs bg-emerald-500/20 text-emerald-400 px-2.5 py-1 rounded-full font-medium flex-shrink-0">
                      Active on device
                    </span>
                  )}
                </div>

                <div className="flex flex-wrap gap-2">
                  <button
                    onClick={() => pushToDevice()}
                    disabled={isPushInProgress || !isConnected}
                    className="px-4 py-2 bg-brand-blue hover:bg-brand-blue/90 text-white rounded-lg text-sm font-medium disabled:opacity-40 transition shadow-sm"
                    title={!isConnected ? 'Connect to device first' : undefined}
                  >
                    {isPushInProgress ? pushStepText : 'Save to device'}
                  </button>
                  <button
                    onClick={() => pullFromDevice()}
                    disabled={!isConnected}
                    className="px-4 py-2 border border-border rounded-lg text-sm text-text-primary disabled:opacity-40 hover:bg-surface-tertiary transition"
                    title={!isConnected ? 'Connect to device first' : undefined}
                  >
                    Load from device
                  </button>
                  <button
                    onClick={() => setActiveProfileOnDevice()}
                    disabled={!isConnected}
                    className="px-4 py-2 border border-emerald-500/40 rounded-lg text-sm text-emerald-400 disabled:opacity-40 hover:bg-emerald-500/10 transition"
                    title={!isConnected ? 'Connect to device first' : undefined}
                  >
                    Set as active
                  </button>
                  <button onClick={() => saveLocally()} className="px-4 py-2 border border-border rounded-lg text-sm text-text-secondary hover:bg-surface-tertiary transition">
                    Save locally
                  </button>
                  <div className="flex-1" />
                  <button
                    onClick={() => deleteFromDevice()}
                    disabled={!isConnected}
                    className="px-3 py-2 text-xs text-red-400 hover:bg-red-500/10 rounded-lg disabled:opacity-30 transition"
                    title={!isConnected ? 'Connect to device first' : undefined}
                  >
                    Delete from device
                  </button>
                  <button onClick={() => deleteLocal()} className="px-3 py-2 text-xs text-red-400 hover:bg-red-500/10 rounded-lg transition">
                    Delete locally
                  </button>
                </div>
              </div>

              {/* Key grid — 3x4 layout */}
              <div className="rounded-xl border border-border bg-surface-secondary p-5">
                <h3 className="text-sm font-semibold text-text-primary mb-3">Keys</h3>
                <p className="text-xs text-text-tertiary mb-4">Click a key to assign an action. Your Micropad has 12 keys in a 3×4 grid.</p>
                <div className="grid grid-cols-4 gap-2.5 max-w-lg">
                  {Array.from({ length: KEY_COUNT }, (_, i) => {
                    const slot = keySlots[i];
                    const hasAction = slot && slot.type !== ActionType.None;
                    const isSelected = selectedKeySlotIndex === i;
                    return (
                      <button
                        key={i}
                        onClick={() => handleEditKey(i)}
                        className={`relative rounded-xl border-2 p-3 text-left min-h-[80px] transition-all hover:scale-[1.02] ${
                          isSelected
                            ? 'border-brand-blue bg-brand-blue/5 shadow-md shadow-brand-blue/20'
                            : hasAction
                              ? 'border-border bg-surface-tertiary hover:border-brand-blue/50'
                              : 'border-border/50 bg-surface-tertiary/50 hover:border-border'
                        }`}
                      >
                        <span className="text-[10px] text-text-tertiary font-medium">K{i + 1}</span>
                        {hasAction ? (
                          <>
                            <p className="text-[10px] text-brand-blue font-medium mt-0.5">{actionTypeLabel(slot.type)}</p>
                            <p className="text-xs text-text-primary truncate mt-0.5 font-medium">{actionLabel(slot)}</p>
                          </>
                        ) : (
                          <p className="text-xs text-text-tertiary mt-1 italic">Empty</p>
                        )}
                      </button>
                    );
                  })}
                </div>
              </div>

              {/* Encoders */}
              <div className="rounded-xl border border-border bg-surface-secondary p-5">
                <h3 className="text-sm font-semibold text-text-primary mb-3">Encoders</h3>
                <p className="text-xs text-text-tertiary mb-4">Choose a preset for each encoder, or assign individual actions.</p>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {editingProfile.encoders?.map((enc, idx) => (
                    <div key={idx} className="rounded-lg border border-border bg-surface-tertiary/50 p-4">
                      <p className="text-sm font-medium text-text-primary mb-3">Encoder {idx + 1}</p>
                      <div className="flex flex-wrap gap-1.5 mb-3">
                        {['Volume', 'Scroll', 'Zoom', 'Media', 'None'].map((preset) => (
                          <button
                            key={preset}
                            onClick={() => applyEncoderPreset(idx, preset)}
                            className="px-3 py-1.5 text-xs bg-surface-input hover:bg-brand-blue/10 hover:text-brand-blue rounded-lg text-text-primary border border-border/50 transition"
                          >
                            {preset}
                          </button>
                        ))}
                      </div>
                      <div className="text-xs text-text-secondary space-y-1">
                        <p>↻ Clockwise: <span className="text-text-primary">{encoderActionLabel(enc.cwAction)}</span></p>
                        <p>↺ Counter-CW: <span className="text-text-primary">{encoderActionLabel(enc.ccwAction)}</span></p>
                        <p>⏎ Press: <span className="text-text-primary">{encoderActionLabel(enc.pressAction)}</span></p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Status */}
              {statusText && (
                <div className={`rounded-lg px-4 py-3 text-sm ${
                  statusText.includes('saved') || statusText.includes('loaded') || statusText.includes('active') || statusText.includes('deleted')
                    ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20'
                    : statusText.includes('Not connected') || statusText.includes('failed') || statusText.includes('error')
                      ? 'bg-red-500/10 text-red-400 border border-red-500/20'
                      : 'bg-surface-tertiary text-text-secondary border border-border/50'
                }`}>
                  {statusText}
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      {editKeyIndex !== null && editingProfile && (
        <ActionEditModal
          keyConfig={keySlots[editKeyIndex]}
          keyIndex={editKeyIndex}
          supportedActions={deviceCaps?.supportedActions}
          supportsMacros={deviceCaps?.supportsMacros ?? false}
          onSave={handleSaveKey}
          onClose={() => setEditKeyIndex(null)}
        />
      )}
    </div>
  );
}
