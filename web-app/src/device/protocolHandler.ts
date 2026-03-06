/**
 * JSON protocol handler: request/response correlation and chunk reassembly.
 * Compatible with firmware protocol (getDeviceInfo, listProfiles, getProfile, setProfile, etc.).
 */

import type { Profile } from '../models/types';

export interface ProtocolMessage {
  v?: number;
  type: string;
  id: number;
  ts?: number;
  cmd?: string;
  event?: string;
  payload?: Record<string, unknown>;
  profileId?: number;
  profile?: Record<string, unknown>;
}

export interface ProtocolCallbacks {
  onEvent: (msg: ProtocolMessage) => void;
}

export class ProtocolHandler {
  private nextId = 1;
  private pending = new Map<number, { resolve: (m: ProtocolMessage | null) => void; reject: (e: Error) => void }>();
  private chunkBuffer: string[] = [];
  private chunkTotal = -1;
  private callbacks: ProtocolCallbacks;

  constructor (
    private sendRaw: (json: string) => Promise<void>,
    callbacks: ProtocolCallbacks
  ) {
    this.callbacks = callbacks;
  }

  handleMessage (json: string): void {
    try {
      if (json.includes('"chunk":') && json.includes('"total":')) {
        this.processChunk(json);
        return;
      }
      this.processMessage(json);
    } catch {
      // ignore malformed
    }
  }

  private processChunk (chunkJson: string): void {
    let obj: { chunk?: number; total?: number; dataB64?: string; data?: string };
    try {
      obj = JSON.parse(chunkJson) as { chunk?: number; total?: number; dataB64?: string; data?: string };
    } catch {
      return;
    }

    const chunkIndex = obj.chunk ?? 0;
    const total = obj.total ?? 0;
    if (total <= 0) return;

    let payload: string;
    if (obj.dataB64) {
      try {
        payload = atob(obj.dataB64);
      } catch {
        return;
      }
    } else {
      payload = (obj.data ?? '').replace(/\\"/g, '"').replace(/\\\\/g, '\\');
    }

    if (chunkIndex === 0) {
      this.chunkBuffer = [];
      this.chunkTotal = total;
    }
    if (this.chunkTotal !== total) return;
    while (this.chunkBuffer.length <= chunkIndex) this.chunkBuffer.push('');
    this.chunkBuffer[chunkIndex] = payload;

    if (this.chunkBuffer.length !== this.chunkTotal) return;
    const full = this.chunkBuffer.join('');
    this.chunkBuffer = [];
    this.chunkTotal = -1;
    this.processMessage(full);
  }

  private processMessage (json: string): void {
    const message = JSON.parse(json) as ProtocolMessage;
    if (message.type === 'response' || message.type === 'RESP') {
      const p = this.pending.get(message.id);
      if (p) {
        this.pending.delete(message.id);
        p.resolve(message);
      }
    } else if (message.type === 'event') {
      this.callbacks.onEvent(message);
    }
  }

  private nextRequestId (): number {
    return this.nextId++;
  }

  private async sendRequest (message: Omit<ProtocolMessage, 'id' | 'type'>): Promise<ProtocolMessage | null> {
    const id = this.nextRequestId();
    const full: ProtocolMessage = { ...message, id, v: 1, type: 'request' };

    return new Promise<ProtocolMessage | null>((resolve, reject) => {
      const timeout = setTimeout(() => {
        if (this.pending.has(id)) {
          this.pending.delete(id);
          resolve(null);
        }
      }, 8000);

      this.pending.set(id, {
        resolve: (m) => {
          clearTimeout(timeout);
          resolve(m);
        },
        reject: (e) => {
          clearTimeout(timeout);
          reject(e);
        }
      });

      this.sendRaw(JSON.stringify(full)).catch((e) => {
        if (this.pending.has(id)) {
          this.pending.delete(id);
          reject(e);
        }
      });
    });
  }

  async getDeviceInfo (): Promise<Record<string, unknown> | null> {
    const res = await this.sendRequest({ cmd: 'getDeviceInfo', payload: {} });
    return (res?.payload ?? null) as Record<string, unknown> | null;
  }

  async getCaps (): Promise<Record<string, unknown> | null> {
    const res = await this.sendRequest({ cmd: 'getCaps', payload: {} });
    return (res?.payload ?? null) as Record<string, unknown> | null;
  }

  async listProfiles (): Promise<Profile[]> {
    const res = await this.sendRequest({ cmd: 'listProfiles', payload: {} });
    const arr = (res?.payload as { profiles?: Array<{ id: number; name: string; size?: number }> })?.profiles;
    if (!Array.isArray(arr)) return [];
    return arr.map((p) => ({ id: p.id, name: p.name ?? 'Unknown', version: 1, keys: [], encoders: [] }));
  }

  async getProfile (profileId: number): Promise<Profile | null> {
    const res = await this.sendRequest({ cmd: 'getProfile', profileId });
    if (!res?.payload) return null;
    return res.payload as unknown as Profile;
  }

  async setProfile (profile: Profile): Promise<boolean> {
    const res = await this.sendRequest({
      cmd: 'setProfile',
      profile: profile as unknown as Record<string, unknown>
    });
    return (res?.payload as { success?: boolean })?.success === true;
  }

  async setActiveProfile (profileId: number): Promise<boolean> {
    const res = await this.sendRequest({ cmd: 'setActiveProfile', profileId });
    return (res?.payload as { success?: boolean })?.success === true;
  }

  async getActiveProfile (): Promise<number | null> {
    const res = await this.sendRequest({ cmd: 'getActiveProfile', payload: {} });
    const id = (res?.payload as { profileId?: number })?.profileId;
    return id != null ? id : null;
  }

  async deleteProfile (profileId: number): Promise<boolean> {
    const res = await this.sendRequest({ cmd: 'deleteProfile', profileId });
    return (res?.payload as { success?: boolean })?.success === true;
  }

  async getStats (): Promise<Record<string, unknown> | null> {
    const res = await this.sendRequest({ cmd: 'getStats', payload: {} });
    return (res?.payload ?? null) as Record<string, unknown> | null;
  }
}
