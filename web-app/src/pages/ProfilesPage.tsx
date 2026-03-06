import { useEffect, useState } from 'react';
import { useProfilesStore } from '../stores/profilesStore';
import { ActionType } from '../models/types';
import ActionEditModal from '../components/ActionEditModal';
import { useMacrosStore } from '../stores/macrosStore';

const KEY_COUNT = 12;
const actionTypeLabels: Record<number, string> = {
  [ActionType.None]: 'Not set',
  [ActionType.Hotkey]: 'Hotkey',
  [ActionType.Text]: 'Text',
  [ActionType.Media]: 'Media',
  [ActionType.Mouse]: 'Mouse',
  [ActionType.Profile]: 'Profile',
  [ActionType.App]: 'App',
  [ActionType.Url]: 'URL',
  [ActionType.Macro]: 'Macro'
};

function getKeyDisplayName (config: { type: number; key?: number; text?: string; macroId?: string }): string {
  if (config.type === ActionType.None) return 'Not set';
  if (config.type === ActionType.Text) return (config.text?.slice(0, 20) || 'Text') + (config.text && config.text.length > 20 ? '…' : '');
  if (config.type === ActionType.Macro) return config.macroId ? `Macro: ${config.macroId.slice(0, 8)}…` : 'Macro';
  return actionTypeLabels[config.type] ?? 'Key';
}

export default function ProfilesPage () {
  const [editKeyIndex, setEditKeyIndex] = useState<number | null>(null);
  const [searchText, setSearchText] = useState('');

  const profiles = useProfilesStore((s) => s.profiles);
  const selectedProfile = useProfilesStore((s) => s.selectedProfile);
  const editingProfile = useProfilesStore((s) => s.editingProfile);
  const selectedLayerIndex = useProfilesStore((s) => s.selectedLayerIndex);
  const selectedKeySlotIndex = useProfilesStore((s) => s.selectedKeySlotIndex);
  const deviceCapsText = useProfilesStore((s) => s.deviceCapsText);
  const statusText = useProfilesStore((s) => s.statusText);
  const isPushInProgress = useProfilesStore((s) => s.isPushInProgress);
  const pushStepText = useProfilesStore((s) => s.pushStepText);

  const loadProfiles = useProfilesStore((s) => s.loadProfiles);
  const selectProfile = useProfilesStore((s) => s.selectProfile);
  const setSelectedLayerIndex = useProfilesStore((s) => s.setSelectedLayerIndex);
  const setSelectedKeySlotIndex = useProfilesStore((s) => s.setSelectedKeySlotIndex);
  const getKeySlotsForLayer = useProfilesStore((s) => s.getKeySlotsForLayer);
  const updateKeyAt = useProfilesStore((s) => s.updateKeyAt);
  const pushToDevice = useProfilesStore((s) => s.pushToDevice);
  const pullFromDevice = useProfilesStore((s) => s.pullFromDevice);
  const saveLocally = useProfilesStore((s) => s.saveLocally);
  const createProfile = useProfilesStore((s) => s.createProfile);
  const duplicateProfile = useProfilesStore((s) => s.duplicateProfile);
  const deleteLocal = useProfilesStore((s) => s.deleteLocal);
  const deleteFromDevice = useProfilesStore((s) => s.deleteFromDevice);
  const setActiveProfileOnDevice = useProfilesStore((s) => s.setActiveProfileOnDevice);
  const addCombo = useProfilesStore((s) => s.addCombo);
  const getCombos = useProfilesStore((s) => s.getCombos);
  const removeCombo = useProfilesStore((s) => s.removeCombo);
  const applyEncoderPreset = useProfilesStore((s) => s.applyEncoderPreset);

  const macroAssets = useMacrosStore((s) => s.macroAssets);
  const loadMacros = useMacrosStore((s) => s.loadMacros);

  useEffect(() => {
    loadProfiles();
    loadMacros();
  }, [loadProfiles, loadMacros]);

  const keySlots = editingProfile ? getKeySlotsForLayer() : [];
  const combos = getCombos();

  const filteredProfiles = profiles.filter((p) => {
    const q = searchText.trim().toLowerCase();
    if (!q) return true;
    return (
      p.name.toLowerCase().includes(q) ||
      String(p.id).includes(q)
    );
  });

  const handleEditKey = (index: number) => {
    setEditKeyIndex(index);
    setSelectedKeySlotIndex(index);
  };

  const handleSaveKey = (config: Parameters<typeof updateKeyAt>[1]) => {
    if (editKeyIndex !== null) {
      updateKeyAt(editKeyIndex, config);
      setEditKeyIndex(null);
    }
  };

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-text-primary mb-2">Profiles</h1>
      <div className="h-1 w-12 bg-brand-blue rounded mb-4" />
      <p className="text-text-secondary mb-6">Manage profiles and key assignments.</p>

      <div className="flex gap-4 flex-wrap">
        {/* Profile list */}
        <div className="w-64 rounded-lg border border-border bg-surface-secondary p-4">
          <button
            onClick={() => loadProfiles()}
            className="w-full py-2 bg-brand-blue text-white rounded font-medium mb-3"
          >
            Refresh
          </button>
          <button onClick={() => createProfile()} className="w-full py-2 border border-border rounded text-text-primary mb-2">
            Create profile
          </button>
          <div className="mb-2">
            <input
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
              placeholder="Search profiles…"
              className="w-full bg-surface-input text-text-primary rounded px-3 py-1.5 border border-border text-sm"
            />
          </div>
          <ul className="space-y-1 max-h-64 overflow-auto">
            {filteredProfiles.map((p) => (
              <li
                key={p.id}
                onClick={() => selectProfile(p)}
                className={`px-3 py-2 rounded cursor-pointer ${
                  selectedProfile?.id === p.id ? 'bg-surface-tertiary text-text-primary' : 'text-text-secondary hover:bg-surface-tertiary'
                }`}
              >
                <span className="font-medium">{p.id}</span> {p.name}
              </li>
            ))}
          </ul>
          <div className="flex gap-1 mt-2 flex-wrap">
            <button onClick={() => duplicateProfile()} className="text-xs text-text-secondary hover:underline">
              Duplicate
            </button>
            <button onClick={() => deleteLocal()} className="text-xs text-red-400 hover:underline">
              Delete local
            </button>
          </div>
        </div>

        {/* Editor area */}
        <div className="flex-1 min-w-0">
          {!editingProfile ? (
            <div className="rounded-lg border border-border bg-surface-secondary p-12 text-center">
              <p className="text-text-primary font-medium mb-2">Select a profile</p>
              <p className="text-text-secondary text-sm mb-4">Choose one from the list or create / import one.</p>
              <button onClick={() => createProfile()} className="px-4 py-2 bg-brand-blue text-white rounded">
                Create profile
              </button>
            </div>
          ) : (
            <div className="rounded-lg border border-border bg-surface-secondary p-6">
              <h2 className="text-lg font-bold text-text-primary mb-1">{editingProfile.name}</h2>
              <p className="text-xs text-text-tertiary mb-4">{deviceCapsText}</p>

              <div className="flex flex-wrap gap-2 mb-4">
                <button onClick={() => setActiveProfileOnDevice()} className="px-3 py-1.5 bg-green-600 text-white rounded text-sm">
                  Activate on device
                </button>
                <button onClick={() => pullFromDevice()} className="px-3 py-1.5 border border-border rounded text-sm text-text-primary">
                  Pull from device
                </button>
                <button
                  onClick={() => pushToDevice()}
                  disabled={isPushInProgress}
                  className="px-3 py-1.5 bg-brand-blue text-white rounded text-sm disabled:opacity-50"
                >
                  {isPushInProgress ? pushStepText : 'Push to device'}
                </button>
                <button onClick={() => saveLocally()} className="px-3 py-1.5 border border-border rounded text-sm text-text-primary">
                  Save locally
                </button>
                <button onClick={() => deleteFromDevice()} className="px-3 py-1.5 bg-red-600 text-white rounded text-sm">
                  Delete from device
                </button>
              </div>

              {/* Layer tabs */}
              <div className="flex items-center gap-4 mb-4">
                <span className="text-text-secondary text-sm">Layer:</span>
                {[0, 1, 2].map((i) => (
                  <label key={i} className="flex items-center gap-1 cursor-pointer">
                    <input
                      type="radio"
                      name="layer"
                      checked={selectedLayerIndex === i}
                      onChange={() => setSelectedLayerIndex(i)}
                      className="rounded"
                    />
                    <span className="text-sm text-text-primary">Layer {i}</span>
                  </label>
                ))}
              </div>

              {/* Key grid 3x4 */}
              <p className="text-sm font-medium text-text-primary mb-2">Keys (click to assign)</p>
              <div className="grid grid-cols-4 gap-2 mb-6 w-full max-w-md">
                {Array.from({ length: KEY_COUNT }, (_, i) => (
                  <button
                    key={i}
                    onClick={() => handleEditKey(i)}
                    className={`rounded-xl border-2 p-3 text-left min-h-[80px] ${
                      selectedKeySlotIndex === i ? 'border-brand-blue bg-surface-input' : 'border-border bg-surface-tertiary hover:border-brand-blue'
                    }`}
                  >
                    <span className="text-xs text-text-tertiary">K{i + 1}</span>
                    <p className="text-xs text-text-primary truncate mt-1">{keySlots[i] ? getKeyDisplayName(keySlots[i]) : 'Not set'}</p>
                  </button>
                ))}
              </div>

              {/* Encoders */}
              <p className="text-sm font-medium text-text-primary mb-2">Encoders</p>
              <div className="flex flex-wrap gap-2 mb-4">
                {editingProfile.encoders?.map((_, idx) => (
                  <div key={idx} className="rounded border border-border bg-surface-tertiary p-2">
                    <span className="text-xs font-medium">Encoder {idx + 1}</span>
                    <div className="flex gap-1 mt-1">
                      {['Volume', 'Scroll', 'Zoom', 'Media', 'None'].map((preset) => (
                        <button
                          key={preset}
                          onClick={() => applyEncoderPreset(idx, preset)}
                          className="px-2 py-0.5 text-xs bg-surface-input rounded text-text-primary"
                        >
                          {preset}
                        </button>
                      ))}
                    </div>
                  </div>
                ))}
              </div>

              {/* Combos */}
              <p className="text-sm font-medium text-text-primary mb-2">Combos</p>
              <button onClick={() => addCombo()} className="px-3 py-1.5 bg-brand-blue text-white rounded text-sm mb-2">
                Add combo
              </button>
              <ul className="space-y-1">
                {combos.map((c, i) => (
                  <li key={i} className="flex items-center gap-2 text-sm">
                    <span>K{c.key1 + 1}+K{c.key2 + 1}</span>
                    <button onClick={() => removeCombo(c)} className="text-red-400 text-xs">Remove</button>
                  </li>
                ))}
              </ul>

              <p className="text-sm text-text-secondary mt-4">{statusText}</p>
            </div>
          )}
        </div>
      </div>

      {editKeyIndex !== null && editingProfile && (
        <ActionEditModal
          keyConfig={keySlots[editKeyIndex]}
          keyIndex={editKeyIndex}
          macroAssets={macroAssets}
          onSave={handleSaveKey}
          onClose={() => setEditKeyIndex(null)}
        />
      )}
    </div>
  );
}
