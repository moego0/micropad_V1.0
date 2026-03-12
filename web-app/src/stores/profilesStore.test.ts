import { describe, expect, it } from 'vitest';
import type { DeviceCaps, Profile } from '../models/types';
import { ActionType } from '../models/types';
import { getNextAvailableProfileId, getProfileValidationError, getPushErrorMessage } from './profilesStore';

function makeProfile(id: number): Profile {
  return {
    id,
    name: `Profile ${id + 1}`,
    version: 1,
    keys: Array.from({ length: 12 }, (_, index) => ({
      index,
      type: ActionType.None,
      modifiers: 0,
      key: 0,
      function: 0,
      action: 0,
      value: 0,
      profileId: 0
    })),
    encoders: Array.from({ length: 2 }, (_, index) => ({
      index,
      acceleration: true,
      stepsPerDetent: 4,
      cwAction: { type: ActionType.None },
      ccwAction: { type: ActionType.None },
      pressAction: { type: ActionType.None }
    }))
  };
}

const caps: DeviceCaps = {
  maxProfiles: 8,
  freeBytes: 4096,
  supportsLayers: false,
  supportsMacros: true,
  supportsEncoders: true,
  maxKeys: 12,
  maxEncoders: 2,
  supportedActions: [0, 1, 2, 3, 4, 5, 7]
};

describe('profilesStore helpers', () => {
  it('finds the next free profile slot within device capacity', () => {
    expect(getNextAvailableProfileId([makeProfile(0), makeProfile(2)], 4)).toBe(1);
  });

  it('returns null when all device slots are used', () => {
    expect(getNextAvailableProfileId(Array.from({ length: 8 }, (_, id) => makeProfile(id)), 8)).toBeNull();
  });

  it('rejects profiles outside device limits', () => {
    const profile = makeProfile(8);
    expect(getProfileValidationError(profile, caps)).toContain('max 8 profiles');
  });

  it('maps disconnect and capacity errors to user-friendly messages', () => {
    expect(getPushErrorMessage(new Error('Not connected'), 8)).toBe('Lost connection while saving. Reconnect and try again.');
    expect(getPushErrorMessage(new Error('Profile ID exceeds device limit'), 8)).toBe("This profile ID exceeds your device's capacity (max 8 profiles).");
  });
});
