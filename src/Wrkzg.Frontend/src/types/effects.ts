export interface EffectList {
  id: number;
  name: string;
  description: string | null;
  isEnabled: boolean;
  triggerTypeId: string;
  triggerConfig: string;
  conditionsConfig: string;
  effectsConfig: string;
  cooldown: number;
  createdAt: string;
}

export interface EffectTypeInfo {
  id: string;
  displayName: string;
  parameterKeys: string[];
}

export interface EffectTypes {
  triggers: EffectTypeInfo[];
  conditions: EffectTypeInfo[];
  effects: EffectTypeInfo[];
}

export interface ConditionConfig {
  type: string;
  params: Record<string, string>;
}

export interface EffectConfig {
  type: string;
  params: Record<string, string>;
}
