import { useState } from 'react';
import { useProfilesStore } from '../stores/profilesStore';

const PRESETS = [
  {
    id: 'vscode',
    name: 'VS Code Editing',
    category: 'Development',
    description: 'Command palette, file search, run/debug, and terminal shortcuts.'
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
    description: 'Media control, tab navigation, screenshots, and bookmarks.'
  }
] as const;

export default function PresetsPage() {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const createProfileFromPreset = useProfilesStore((s) => s.createProfileFromPreset);

  const handleCreate = async () => {
    if (!selectedId) return;
    const preset = PRESETS.find((p) => p.id === selectedId);
    if (!preset) return;
    setStatus(null);
    await createProfileFromPreset(preset.name);
    setStatus(`"${preset.name}" created. Go to the Profiles tab to customize keys and save to your device.`);
  };

  return (
    <div className="p-6 max-w-3xl animate-fade-in">
      <h1 className="text-2xl font-bold text-text-primary mb-2">Templates</h1>
      <div className="h-1 w-12 bg-brand-blue rounded mb-4" />
      <p className="text-text-secondary mb-6">
        Start with a pre-made template, then customize every key in the Profiles tab.
        Templates create a blank profile with the right name — you assign the actual keys.
      </p>

      <div className="grid gap-3 sm:grid-cols-2">
        {PRESETS.map((preset) => (
          <button
            key={preset.id}
            type="button"
            onClick={() => setSelectedId(preset.id)}
            className={`text-left rounded-xl border px-5 py-4 bg-surface-secondary/80 backdrop-blur transition hover:shadow-lg ${
              selectedId === preset.id ? 'border-brand-blue ring-1 ring-brand-blue/40' : 'border-border hover:border-brand-blue/50'
            }`}
          >
            <span className="text-[10px] font-semibold uppercase tracking-wider text-text-tertiary">{preset.category}</span>
            <h2 className="text-base font-semibold text-text-primary mt-0.5">{preset.name}</h2>
            <p className="text-sm text-text-secondary mt-1">{preset.description}</p>
          </button>
        ))}
      </div>

      <div className="mt-6">
        <button
          type="button"
          disabled={!selectedId}
          onClick={handleCreate}
          className="px-6 py-2.5 rounded-full bg-brand-blue text-white text-sm font-semibold disabled:opacity-30 shadow-md shadow-brand-blue/30 hover:shadow-lg transition"
        >
          Create from template
        </button>
      </div>

      {status && (
        <p className="mt-4 text-sm text-emerald-400 bg-emerald-500/10 border border-emerald-500/20 rounded-lg px-4 py-3">
          {status}
        </p>
      )}
    </div>
  );
}
