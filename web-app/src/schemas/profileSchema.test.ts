import { describe, it, expect } from 'vitest';
import { exportBundleSchema } from './profileSchema';

describe('exportBundleSchema', () => {
  it('accepts valid export bundle', () => {
    const bundle = {
      schemaVersion: 1,
      exportedAt: new Date().toISOString(),
      profiles: [
        {
          id: 0,
          name: 'Test',
          version: 1,
          keys: [{ index: 0, type: 0, modifiers: 0, key: 0, function: 0, action: 0, value: 0, profileId: 0 }],
          encoders: [{ index: 0, acceleration: false, stepsPerDetent: 1 }]
        }
      ],
      macros: [
        {
          macroId: 'abc',
          name: 'M1',
          tags: [],
          steps: [{ action: 'delay', ms: 100 }],
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          version: 1
        }
      ]
    };
    const result = exportBundleSchema.safeParse(bundle);
    expect(result.success).toBe(true);
  });

  it('accepts bundle with defaults for schemaVersion', () => {
    const bundle = {
      exportedAt: new Date().toISOString(),
      profiles: [],
      macros: []
    };
    const result = exportBundleSchema.safeParse(bundle);
    expect(result.success).toBe(true);
  });
});
