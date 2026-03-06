import { getDB, STORE_MACROS } from './db';
import type { MacroAsset } from '../models/types';
import { macroAssetSchema } from '../schemas/profileSchema';

export async function getAllMacros (): Promise<MacroAsset[]> {
  const db = await getDB();
  const list = await db.getAll(STORE_MACROS);
  const out: MacroAsset[] = [];
  for (const raw of list) {
    const parsed = macroAssetSchema.safeParse(raw);
    if (parsed.success) out.push(parsed.data as unknown as MacroAsset);
  }
  return out.sort((a, b) => a.name.localeCompare(b.name));
}

export async function getMacro (macroId: string): Promise<MacroAsset | null> {
  const db = await getDB();
  const raw = await db.get(STORE_MACROS, macroId);
  if (!raw) return null;
  const parsed = macroAssetSchema.safeParse(raw);
  return parsed.success ? (parsed.data as unknown as MacroAsset) : null;
}

export async function saveMacro (asset: MacroAsset): Promise<void> {
  const updated = { ...asset, updatedAt: new Date().toISOString() };
  const db = await getDB();
  await db.put(STORE_MACROS, updated as unknown as { macroId: string; [k: string]: unknown });
}

export async function deleteMacro (macroId: string): Promise<void> {
  const db = await getDB();
  await db.delete(STORE_MACROS, macroId);
}

export async function searchMacros (searchText?: string, tagFilter?: string[]): Promise<MacroAsset[]> {
  const all = await getAllMacros();
  if (!searchText?.trim() && (!tagFilter || tagFilter.length === 0)) return all;
  const q = (searchText ?? '').trim().toLowerCase();
  return all.filter((m) => {
    if (q && !m.name.toLowerCase().includes(q) && !m.tags.some((t) => t.toLowerCase().includes(q))) return false;
    if (tagFilter?.length && !tagFilter.some((t) => m.tags.includes(t))) return false;
    return true;
  });
}
