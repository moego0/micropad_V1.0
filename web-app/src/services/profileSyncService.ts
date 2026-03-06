import type { Profile } from '../models/types';
import type { DeviceCaps } from '../models/types';
import type { ProtocolHandler } from '../device/protocolHandler';
import * as profileStorage from '../storage/profileStorage';

export class ProfileSyncService {
  constructor (
    private getConnected: () => boolean,
    private getProtocol: () => ProtocolHandler | null
  ) {}

  async pushProfile (profile: Profile): Promise<boolean> {
    if (!this.getConnected()) throw new Error('Not connected. Open the Devices page and connect first.');
    if (!this.getProtocol()) throw new Error('Protocol not ready.');
    return this.getProtocol()!.setProfile(profile);
  }

  async pullProfile (profileId: number): Promise<Profile | null> {
    if (!this.getConnected()) throw new Error('Not connected. Open the Devices page and connect first.');
    if (!this.getProtocol()) throw new Error('Protocol not ready.');
    return this.getProtocol()!.getProfile(profileId);
  }

  async listProfiles (): Promise<Profile[]> {
    if (!this.getConnected() || !this.getProtocol()) return [];
    return this.getProtocol()!.listProfiles();
  }

  async getActiveProfileId (): Promise<number | null> {
    if (!this.getConnected() || !this.getProtocol()) return null;
    return this.getProtocol()!.getActiveProfile();
  }

  async setActiveProfile (profileId: number): Promise<boolean> {
    if (!this.getConnected()) throw new Error('Not connected. Open the Devices page and connect first.');
    if (!this.getProtocol()) throw new Error('Protocol not ready.');
    return this.getProtocol()!.setActiveProfile(profileId);
  }

  async getCaps (): Promise<DeviceCaps | null> {
    if (!this.getConnected() || !this.getProtocol()) return null;
    const payload = await this.getProtocol()!.getCaps();
    return payload as unknown as DeviceCaps | null;
  }

  async deleteProfileFromDevice (profileId: number): Promise<boolean> {
    if (!this.getConnected()) throw new Error('Not connected. Open the Devices page and connect first.');
    if (!this.getProtocol()) throw new Error('Protocol not ready.');
    return this.getProtocol()!.deleteProfile(profileId);
  }

  async saveProfileLocally (profile: Profile): Promise<void> {
    await profileStorage.saveProfile(profile);
  }

  async deleteProfileLocally (profileId: number): Promise<void> {
    await profileStorage.deleteProfile(profileId);
  }

  async getLocalProfiles (): Promise<Profile[]> {
    return profileStorage.getAllProfiles();
  }
}
