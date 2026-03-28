import { api } from "./client";
import type { PollResults, PollHistoryItem, PollTemplate, CreatePollRequest } from "../types/polls";
import type { SettingsUpdate } from "../types/common";

const BASE = "/api/polls";

export const pollsApi = {
  getActive: async (): Promise<PollResults | null> => {
    const res = await fetch(`${BASE}/active`);
    if (res.status === 404) return null;
    if (!res.ok) throw new Error("Failed to fetch active poll");
    return res.json();
  },

  getHistory: () => api.get<PollHistoryItem[]>(`${BASE}/history`),

  getTemplates: () => api.get<PollTemplate[]>(`${BASE}/templates`),

  create: (body: CreatePollRequest) => api.post<void>(BASE, body),

  end: () => api.post<void>(`${BASE}/end`),

  cancel: () => api.post<void>(`${BASE}/cancel`),

  resetTemplate: (key: string) =>
    api.post<void>(`${BASE}/templates/reset/${encodeURIComponent(key)}`),

  saveTemplate: (body: SettingsUpdate) =>
    api.put<void>("/api/settings", body),
};
