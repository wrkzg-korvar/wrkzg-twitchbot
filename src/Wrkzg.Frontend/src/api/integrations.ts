import { api } from "./client";

export interface DiscordStatus {
  configured: boolean;
  webhookUrlSet: boolean;
}

export interface ObsStatus {
  isConnected: boolean;
  obsVersion: string | null;
  currentScene: string | null;
  isConfigured: boolean;
}

export const integrationsApi = {
  getDiscord: () => api.get<DiscordStatus>("/api/integrations/discord"),
  setDiscord: (webhookUrl: string) => api.put<{ configured: boolean }>("/api/integrations/discord", { webhookUrl }),
  removeDiscord: () => api.del("/api/integrations/discord"),
  testDiscord: () => api.post<{ success: boolean; message: string }>("/api/integrations/discord/test"),

  getObs: () => api.get<ObsStatus>("/api/integrations/obs"),
  setObs: (host: string, port: number, password?: string) =>
    api.put<{ configured: boolean }>("/api/integrations/obs", { host, port, password }),
  removeObs: () => api.del("/api/integrations/obs"),
  connectObs: () => api.post<{ connected: boolean }>("/api/integrations/obs/connect"),
  disconnectObs: () => api.post<{ disconnected: boolean }>("/api/integrations/obs/disconnect"),
  getObsScenes: () => api.get<string[]>("/api/integrations/obs/scenes"),
};
