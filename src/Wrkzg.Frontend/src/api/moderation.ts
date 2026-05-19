import { api } from "./client";
import type { ModerationEvent, LiveViewer, TimeoutRequest, BanRequest, ShoutoutRequest } from "../types/moderation";

export const moderationApi = {
  timeout: (request: TimeoutRequest) =>
    api.post<{ success: boolean; eventId: number }>("/api/moderation/timeout", request),

  ban: (request: BanRequest) =>
    api.post<{ success: boolean; eventId: number }>("/api/moderation/ban", request),

  unban: (twitchUserId: string) =>
    api.delete<{ success: boolean; eventId: number }>(`/api/moderation/ban/${twitchUserId}`),

  shoutout: (request: ShoutoutRequest) =>
    api.post<{ success: boolean; eventId: number }>("/api/moderation/shoutout", request),

  getLog: (limit = 100, days?: number) => {
    const params = new URLSearchParams({ limit: limit.toString() });
    if (days) params.set("days", days.toString());
    return api.get<ModerationEvent[]>(`/api/moderation/log?${params.toString()}`);
  },

  getUserLog: (twitchUserId: string, limit = 100, days?: number) => {
    const params = new URLSearchParams({ limit: limit.toString() });
    if (days) params.set("days", days.toString());
    return api.get<ModerationEvent[]>(`/api/moderation/log/${twitchUserId}?${params.toString()}`);
  },

  cleanupLog: () =>
    api.delete<{ deleted: number; cutoff: string }>("/api/moderation/log/cleanup"),

  getLiveViewers: () =>
    api.get<LiveViewer[]>("/api/moderation/viewers"),
};
