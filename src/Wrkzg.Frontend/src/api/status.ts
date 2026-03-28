import { api } from "./client";
import type { StatusResponse, ChatMsg, SendChatRequest } from "../types/status";

export const statusApi = {
  get: () => api.get<StatusResponse>("/api/status"),

  getRecentChat: () => api.get<ChatMsg[]>("/api/chat/recent"),

  getRecentChatForUser: (twitchId: string) =>
    api.get<ChatMsg[]>(`/api/chat/recent?userId=${encodeURIComponent(twitchId)}`),

  sendChat: (body: SendChatRequest) => api.post<void>("/api/chat/send", body),
};
