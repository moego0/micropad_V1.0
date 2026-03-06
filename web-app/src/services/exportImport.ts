import JSZip from 'jszip';
import type { Profile, MacroAsset } from '../models/types';
import { exportBundleSchema, type ExportBundle } from '../schemas/profileSchema';
import * as profileStorage from '../storage/profileStorage';
import * as macroStorage from '../storage/macroStorage';

const EXPORT_FILENAME = 'micropad-export.json';

export async function exportToJson (): Promise<string> {
  const profiles = await profileStorage.getAllProfiles();
  const macros = await macroStorage.getAllMacros();
  const bundle: ExportBundle = {
    schemaVersion: 1,
    exportedAt: new Date().toISOString(),
    profiles: profiles as unknown as ExportBundle['profiles'],
    macros: macros.map((m) => ({
      ...m,
      createdAt: m.createdAt,
      updatedAt: m.updatedAt
    })) as unknown as ExportBundle['macros']
  };
  return JSON.stringify(bundle, null, 2);
}

export async function exportToZip (): Promise<Blob> {
  const json = await exportToJson();
  const zip = new JSZip();
  zip.file(EXPORT_FILENAME, json);
  return zip.generateAsync({ type: 'blob' });
}

export async function importFromJson (json: string): Promise<{ profiles: number; macros: number }> {
  const raw = JSON.parse(json) as unknown;
  const parsed = exportBundleSchema.safeParse(raw);
  if (!parsed.success) throw new Error('Invalid export format');

  const bundle = parsed.data;
  let profilesCount = 0;
  let macrosCount = 0;

  for (const p of bundle.profiles) {
    const profile = p as unknown as Profile;
    await profileStorage.saveProfile(profile);
    profilesCount++;
  }

  for (const m of bundle.macros) {
    const macro: MacroAsset = {
      macroId: m.macroId || crypto.randomUUID().replace(/-/g, ''),
      name: m.name,
      tags: m.tags || [],
      steps: m.steps || [],
      createdAt: m.createdAt || new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      version: m.version || 1
    };
    await macroStorage.saveMacro(macro);
    macrosCount++;
  }

  return { profiles: profilesCount, macros: macrosCount };
}

export async function importFromFile (file: File): Promise<{ profiles: number; macros: number }> {
  const text = await file.text();
  if (file.name.endsWith('.zip')) {
    const zip = await JSZip.loadAsync(file);
    const entry = zip.file(EXPORT_FILENAME) || zip.file(/\.json$/)[0];
    if (!entry) throw new Error('No JSON found in zip');
    const json = await entry.async('string');
    return importFromJson(json);
  }
  return importFromJson(text);
}

export function downloadBlob (blob: Blob, filename: string): void {
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = filename;
  a.click();
  URL.revokeObjectURL(a.href);
}
