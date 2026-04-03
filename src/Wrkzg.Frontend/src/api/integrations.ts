import { api } from "./client";

export interface DiscordStatus {
  configured: boolean;
  webhookUrlSet: boolean;
}

export const integrationsApi = {
  getDiscord: () => api.get<DiscordStatus>("/api/integrations/discord"),
  setDiscord: (webhookUrl: string) => api.put<{ configured: boolean }>("/api/integrations/discord", { webhookUrl }),
  removeDiscord: () => api.del("/api/integrations/discord"),
  testDiscord: () => api.post<{ success: boolean; message: string }>("/api/integrations/discord/test"),
};
