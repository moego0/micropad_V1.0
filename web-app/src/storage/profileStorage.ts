import { getDB, STORE_PROFILES } from './db';
import type { Profile } from '../models/types';
import { profileSchema } from '../schemas/profileSchema';

export async function getAllProfiles (): Promise<Profile[]> {
  const db = await getDB();
  const list = await db.getAll(STORE_PROFILES);
  const out: Profile[] = [];
  for (const raw of list) {
    const parsed = profileSchema.safeParse({ ...raw, schemaVersion: 1, deviceModel: 'micropad' });
    if (parsed.success) {
      out.push(parsed.data as unknown as Profile);
    }
  }
  return out.sort((a, b) => a.id - b.id);
}

export async function getProfile (id: number): Promise<Profile | null> {
  const db = await getDB();
  const raw = await db.get(STORE_PROFILES, id);
  if (!raw) return null;
  const parsed = profileSchema.safeParse({ ...raw, schemaVersion: 1, deviceModel: 'micropad' });
  return parsed.success ? (parsed.data as unknown as Profile) : null;
}

export async function saveProfile (profile: Profile): Promise<void> {
  const db = await getDB();
  await db.put(STORE_PROFILES, profile as unknown as { id: number; [k: string]: unknown });
}

export async function deleteProfile (id: number): Promise<void> {
  const db = await getDB();
  await db.delete(STORE_PROFILES, id);
}
