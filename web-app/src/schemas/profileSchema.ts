import { z } from 'zod';

const keyConfigSchema = z.object({
  index: z.number(),
  type: z.number(),
  modifiers: z.number().default(0),
  key: z.number().default(0),
  text: z.string().optional(),
  function: z.number().default(0),
  action: z.number().default(0),
  value: z.number().default(0),
  profileId: z.number().default(0),
  macroSteps: z.array(z.object({
    stepType: z.number(),
    delayMs: z.number().optional(),
    key: z.number().optional(),
    modifiers: z.number().optional(),
    text: z.string().optional(),
    mediaFunction: z.number().optional()
  })).optional()
});

const encoderActionSchema = z.object({
  type: z.number(),
  value: z.number().optional(),
  key: z.number().optional(),
  modifiers: z.number().optional(),
  function: z.number().optional(),
  action: z.number().optional()
}).passthrough();

const encoderConfigSchema = z.object({
  index: z.number(),
  acceleration: z.boolean().default(true),
  stepsPerDetent: z.number().default(4),
  cwAction: encoderActionSchema.optional(),
  ccwAction: encoderActionSchema.optional(),
  pressAction: encoderActionSchema.optional()
});

export const profileSchema = z.object({
  id: z.number(),
  name: z.string(),
  version: z.number().default(1),
  keys: z.array(keyConfigSchema),
  encoders: z.array(encoderConfigSchema)
}).passthrough();

export type ProfileSchema = z.infer<typeof profileSchema>;

const macroStepSchema = z.object({
  stepType: z.number(),
  delayMs: z.number().optional(),
  key: z.number().optional(),
  modifiers: z.number().optional(),
  text: z.string().optional(),
  mediaFunction: z.number().optional()
}).passthrough();

export const macroAssetSchema = z.object({
  macroId: z.string(),
  name: z.string(),
  tags: z.array(z.string()),
  steps: z.array(macroStepSchema.or(z.any())),
  createdAt: z.string(),
  updatedAt: z.string(),
  version: z.number()
});

export const exportBundleSchema = z.object({
  schemaVersion: z.number(),
  exportedAt: z.string(),
  profiles: z.array(profileSchema),
  macros: z.array(macroAssetSchema)
});

export type ExportBundle = z.infer<typeof exportBundleSchema>;
