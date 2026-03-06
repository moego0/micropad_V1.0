import { useState } from 'react';
import { useProfilesStore } from '../stores/profilesStore';

const PRESETS = [
  {
    id: 'vscode',
    name: 'VS Code Editing',
    category: 'Development',
    description: 'Quick access to command palette, file search, run/debug, and integrated terminal.'
  },
  {
    id: 'figma',
    name: 'Figma Design',
    category: 'Design',
    description: 'Zoom, pan, layers, and export shortcuts for design work.'
  },
  {
    id: 'obs',
    name: 'OBS Streaming',
    category: 'Content',
    description: 'Scene switching, recording, mute/unmute, and stream controls.'
  },
  {
    id: 'browsing',
    name: 'Browser & Media',
    category: 'Everyday',
    description: 'Media control, tab navigation, screenshots, and favorite websites.'
  }
] as const;

export default function PresetsPage () {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const createProfileFromPreset = useProfilesStore((s) => s.createProfileFromPreset);

  const handleCreate = async () => {
    if (!selectedId) return;
    const preset = PRESETS.find((p) => p.id === selectedId);
    if (!preset) return;
    setStatus(null);
    await createProfileFromPreset(preset.name);
    setStatus(`Preset '${preset.name}' created. You can now fine‑tune keys in Profiles.`);
  };

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-text-primary mb-2">Templates</h1>
      <div className="h-1 w-12 bg-brand-blue rounded mb-4" />
      <p className="text-text-secondary mb-6">
        Start from ready‑made templates for popular apps, then customize every key in the Profiles tab.
      </p>

      <div className="grid gap-4 md:grid-cols-2">
        {PRESETS.map((preset) => (
          <button
            key={preset.id}
            type="button"
            onClick={() => setSelectedId(preset.id)}
            className={`text-left rounded-xl border px-4 py-4 bg-surface-secondary/80 backdrop-blur transition hover:-translate-y-0.5 hover:shadow-lg hover:border-brand-blue ${
              selectedId === preset.id ? 'border-brand-blue ring-1 ring-brand-blue/60' : 'border-border'
            }`}
          >
            <div className="flex items-center justify-between mb-1">
              <span className="text-sm font-semibold uppercase tracking-wide text-text-tertiary">{preset.category}</span>
            </div>
            <h2 className="text-lg font-semibold text-text-primary mb-1">{preset.name}</h2>
            <p className="text-sm text-text-secondary">{preset.description}</p>
          </button>
        ))}
      </div>

      <div className="mt-6 flex items-center justify-between flex-wrap gap-3">
        <div>
          <button
            type="button"
            disabled={!selectedId}
            onClick={handleCreate}
            className="px-5 py-2.5 rounded-full bg-brand-blue text-white text-sm font-semibold disabled:opacity-40 disabled:cursor-not-allowed shadow-md shadow-brand-blue/40 hover:shadow-lg hover:shadow-brand-blue/60 transition"
          >
            Create profile from selected template
          </button>
        </div>
        <p className="text-xs text-text-tertiary">
          Templates are stored locally in your browser. You can export and share them from the Settings page.
        </p>
      </div>

      {status && (
        <p className="mt-4 text-sm text-green-400">
          {status}
        </p>
      )}
    </div>
  );
}
