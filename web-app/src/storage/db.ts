import { openDB, type IDBPDatabase } from 'idb';

const DB_NAME = 'micropad-db';
const DB_VERSION = 1;

export const STORE_PROFILES = 'profiles';
export const STORE_MACROS = 'macros';
export const STORE_SETTINGS = 'settings';

export interface DBSchema {
  [STORE_PROFILES]: { key: number; value: unknown };
  [STORE_MACROS]: { key: string; value: unknown };
  [STORE_SETTINGS]: { key: string; value: unknown };
}

let dbInstance: IDBPDatabase<DBSchema> | null = null;

export async function getDB (): Promise<IDBPDatabase<DBSchema>> {
  if (dbInstance) return dbInstance;
  dbInstance = await openDB<DBSchema>(DB_NAME, DB_VERSION, {
    upgrade (db) {
      if (!db.objectStoreNames.contains(STORE_PROFILES)) {
        db.createObjectStore(STORE_PROFILES, { keyPath: 'id' });
      }
      if (!db.objectStoreNames.contains(STORE_MACROS)) {
        db.createObjectStore(STORE_MACROS, { keyPath: 'macroId' });
      }
      if (!db.objectStoreNames.contains(STORE_SETTINGS)) {
        db.createObjectStore(STORE_SETTINGS);
      }
    }
  });
  return dbInstance;
}
