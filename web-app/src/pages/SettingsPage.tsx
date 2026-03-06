import { useState } from 'react';
import { exportToZip, importFromFile, downloadBlob } from '../services/exportImport';

export default function SettingsPage () {
  const [importResult, setImportResult] = useState<string | null>(null);
  const [fileInputKey, setFileInputKey] = useState(0);

  const handleExport = async () => {
    const blob = await exportToZip();
    downloadBlob(blob, 'micropad-backup.zip');
  };

  const handleImport = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setImportResult(null);
    try {
      const { profiles, macros } = await importFromFile(file);
      setImportResult(`Imported ${profiles} profile(s) and ${macros} macro(s).`);
      setFileInputKey((k) => k + 1);
    } catch (err) {
      setImportResult(`Error: ${(err as Error).message}`);
    }
  };

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-text-primary mb-2">Settings</h1>
      <div className="h-1 w-12 bg-brand-blue rounded mb-4" />
      <p className="text-text-secondary mb-6">Backup and restore your data.</p>

      <div className="rounded-lg border border-border bg-surface-secondary p-6 max-w-lg">
        <h2 className="font-semibold text-text-primary mb-4">Export / Import</h2>
        <p className="text-sm text-text-secondary mb-4">
          Export saves all profiles and macros to a single .zip file. Import restores from a .zip or .json file.
        </p>
        <div className="flex flex-wrap gap-3">
          <button onClick={handleExport} className="px-4 py-2 bg-brand-blue text-white rounded font-medium">
            Export backup (.zip)
          </button>
          <label className="px-4 py-2 border border-border rounded font-medium text-text-primary cursor-pointer hover:bg-surface-tertiary">
            Import from file
            <input
              key={fileInputKey}
              type="file"
              accept=".zip,.json"
              onChange={handleImport}
              className="hidden"
            />
          </label>
        </div>
        {importResult && (
          <p className={`mt-3 text-sm ${importResult.startsWith('Error') ? 'text-red-400' : 'text-green-400'}`}>
            {importResult}
          </p>
        )}
      </div>
    </div>
  );
}
