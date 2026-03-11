import { useState } from 'react';
import { exportToZip, importFromFile, downloadBlob } from '../services/exportImport';
import { useDeviceStore } from '../stores/deviceStore';

export default function SettingsPage() {
  const [importResult, setImportResult] = useState<string | null>(null);
  const [fileInputKey, setFileInputKey] = useState(0);
  const [deviceAction, setDeviceAction] = useState<string | null>(null);

  const protocol = useDeviceStore((s) => s.protocol);
  const isConnected = useDeviceStore((s) => ['configConnected', 'hidConnected', 'hidReady'].includes(s.connectionState));

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
      setImportResult(`Import failed: ${(err as Error).message}`);
    }
  };

  const handleFactoryReset = async () => {
    if (!protocol || !confirm('This will erase all profiles on the device and restore defaults. Continue?')) return;
    try {
      setDeviceAction('Resetting…');
      await protocol.factoryReset();
      setDeviceAction('Factory reset complete. The device will restart with default profiles.');
    } catch {
      setDeviceAction('Reset failed. Try disconnecting and reconnecting.');
    }
  };

  const handleReboot = async () => {
    if (!protocol) return;
    try {
      setDeviceAction('Rebooting…');
      await protocol.reboot();
      setDeviceAction('Device is rebooting. It will reconnect automatically in a few seconds.');
    } catch {
      setDeviceAction('Reboot command failed.');
    }
  };

  return (
    <div className="p-6 max-w-2xl animate-fade-in">
      <h1 className="text-2xl font-bold text-text-primary mb-2">Settings</h1>
      <div className="h-1 w-12 bg-brand-blue rounded mb-6" />

      {/* Export/Import */}
      <div className="rounded-xl border border-border bg-surface-secondary p-6 mb-6">
        <h2 className="font-semibold text-text-primary mb-2">Backup & Restore</h2>
        <p className="text-sm text-text-secondary mb-4">
          Export saves all your profiles and macros to a .zip file. Import restores them from a .zip or .json file.
        </p>
        <div className="flex flex-wrap gap-3">
          <button onClick={handleExport} className="px-5 py-2.5 bg-brand-blue text-white rounded-lg font-medium text-sm shadow-sm transition hover:bg-brand-blue/90">
            Export backup
          </button>
          <label className="px-5 py-2.5 border border-border rounded-lg font-medium text-sm text-text-primary cursor-pointer hover:bg-surface-tertiary transition">
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
          <p className={`mt-3 text-sm ${importResult.startsWith('Import failed') ? 'text-red-400' : 'text-emerald-400'}`}>
            {importResult}
          </p>
        )}
      </div>

      {/* Device actions */}
      <div className="rounded-xl border border-border bg-surface-secondary p-6">
        <h2 className="font-semibold text-text-primary mb-2">Device</h2>
        {!isConnected ? (
          <p className="text-sm text-text-tertiary">Connect to your Micropad on the Devices page to access device settings.</p>
        ) : (
          <>
            <p className="text-sm text-text-secondary mb-4">
              These actions affect your physical Micropad device.
            </p>
            <div className="flex flex-wrap gap-3">
              <button
                onClick={handleReboot}
                className="px-4 py-2 border border-border rounded-lg text-sm text-text-primary hover:bg-surface-tertiary transition"
              >
                Restart device
              </button>
              <button
                onClick={handleFactoryReset}
                className="px-4 py-2 border border-red-500/40 rounded-lg text-sm text-red-400 hover:bg-red-500/10 transition"
              >
                Factory reset
              </button>
            </div>
            {deviceAction && (
              <p className="mt-3 text-sm text-text-secondary">{deviceAction}</p>
            )}
          </>
        )}
      </div>
    </div>
  );
}
