import { z } from 'zod';

const keyActionSchema = z.object({
  type: z.number(),
  modifiers: z.number().optional(),
  key: z.number().optional(),
  text: z.string().optional(),
  function: z.number().optional(),
  action: z.number().optional(),
  value: z.number().optional(),
  profileId: z.number().optional(),
  path: z.string().optional(),
  url: z.string().optional(),
  macroId: z.string().optional()
});

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
  AppPath: z.string().optional(),
  url: z.string().optional(),
  macroId: z.string().optional(),
  macroSnapshot: z.array(z.any()).optional(),
  tapAction: keyActionSchema.optional(),
  holdAction: keyActionSchema.optional(),
  doubleTapAction: keyActionSchema.optional()
});

const encoderActionSchema = z.object({
  type: z.string(),
  value: z.number().optional(),
  key: z.number().optional(),
  modifiers: z.number().optional(),
  mediaFunction: z.number().optional()
});

const encoderConfigSchema = z.object({
  index: z.number(),
  acceleration: z.boolean().default(false),
  stepsPerDetent: z.number().default(1),
  stepSize: z.number().optional(),
  accelerationCurve: z.string().optional(),
  smoothing: z.boolean().optional(),
  mode: z.number().optional(),
  cwAction: encoderActionSchema.optional(),
  ccwAction: encoderActionSchema.optional(),
  pressAction: encoderActionSchema.optional(),
  holdAction: encoderActionSchema.optional(),
  pressRotateCwAction: encoderActionSchema.optional(),
  pressRotateCcwAction: encoderActionSchema.optional(),
  holdRotateCwAction: encoderActionSchema.optional(),
  holdRotateCcwAction: encoderActionSchema.optional()
});

const comboAssignmentSchema = z.object({
  key1: z.number(),
  key2: z.number(),
  action: keyActionSchema.optional()
});

export const profileSchema = z.object({
  schemaVersion: z.number().default(1),
  deviceModel: z.string().optional().default('micropad'),
  id: z.number(),
  name: z.string(),
  version: z.number(),
  keys: z.array(keyConfigSchema),
  encoders: z.array(encoderConfigSchema),
  layer1Keys: z.array(keyConfigSchema).optional(),
  layer2Keys: z.array(keyConfigSchema).optional(),
  combos: z.array(comboAssignmentSchema).optional()
});

export type ProfileSchema = z.infer<typeof profileSchema>;

export const macroStepSchema = z.object({
  action: z.string(),
  key: z.string().optional(),
  ms: z.number().optional(),
  text: z.string().optional(),
  value: z.number().optional(),
  vkCode: z.number().optional(),
  mediaFunction: z.number().optional()
});

export const macroAssetSchema = z.object({
  macroId: z.string(),
  name: z.string(),
  tags: z.array(z.string()),
  steps: z.array(macroStepSchema),
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
