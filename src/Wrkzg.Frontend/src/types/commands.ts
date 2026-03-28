export interface SystemCommand {
  trigger: string;
  aliases: string[];
  description: string;
  defaultResponseTemplate: string | null;
  customResponseTemplate: string | null;
  isEnabled: boolean;
}

export interface Command {
  id: number;
  trigger: string;
  aliases: string[];
  responseTemplate: string;
  permissionLevel: number;
  globalCooldownSeconds: number;
  userCooldownSeconds: number;
  isEnabled: boolean;
  useCount: number;
  createdAt: string;
}

export interface CreateCommandRequest {
  trigger: string;
  aliases?: string[];
  responseTemplate: string;
  permissionLevel?: number;
  globalCooldownSeconds?: number;
  userCooldownSeconds?: number;
}

export interface UpdateCommandRequest {
  trigger?: string;
  aliases?: string[];
  responseTemplate?: string;
  permissionLevel?: number;
  globalCooldownSeconds?: number;
  userCooldownSeconds?: number;
  isEnabled?: boolean;
}

export interface UpdateSystemCommandRequest {
  customResponseTemplate: string | null;
  isEnabled: boolean;
}
