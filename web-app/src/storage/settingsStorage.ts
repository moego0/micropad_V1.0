import { getDB, STORE_SETTINGS } from './db';

const KEY_LAST_DEVICE = 'lastDeviceId';
const KEY_GETTING_STARTED_DISMISSED = 'gettingStartedDismissed';

export interface AppSettings {
  lastDeviceId?: string;
  gettingStartedDismissed?: boolean;
}

export async function loadSettings (): Promise<AppSettings> {
  const db = await getDB();
  const last = await db.get(STORE_SETTINGS, KEY_LAST_DEVICE);
  const dismissed = await db.get(STORE_SETTINGS, KEY_GETTING_STARTED_DISMISSED);
  return {
    lastDeviceId: last as string | undefined,
    gettingStartedDismissed: dismissed as boolean | undefined
  };
}

export async function saveSettings (s: AppSettings): Promise<void> {
  const db = await getDB();
  if (s.lastDeviceId !== undefined) await db.put(STORE_SETTINGS, s.lastDeviceId, KEY_LAST_DEVICE);
  if (s.gettingStartedDismissed !== undefined) await db.put(STORE_SETTINGS, s.gettingStartedDismissed, KEY_GETTING_STARTED_DISMISSED);
}
