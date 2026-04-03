import { api } from "./client";
import type { SongRequest } from "../types/songRequests";

export interface SongRequestStatus {
  queueOpen: boolean;
  queueCount: number;
  maxPerUser: number;
  maxDuration: number;
  pointsCost: number;
}

export interface SongRequestMessages {
  name: string;
  messages: Record<string, string>;
  defaults: Record<string, string>;
}

export const songRequestsApi = {
  getQueue: () => api.get<SongRequest[]>("/api/song-requests/queue"),
  getCurrent: () => api.get<SongRequest | null>("/api/song-requests/current"),
  getHistory: () => api.get<SongRequest[]>("/api/song-requests/history"),
  skip: () => api.post<{ message: string }>("/api/song-requests/skip"),
  playNext: () => api.post<SongRequest | null>("/api/song-requests/next"),
  remove: (id: number) => api.del(`/api/song-requests/${id}`),
  clearQueue: () => api.post<{ message: string }>("/api/song-requests/clear"),
  getStatus: () => api.get<SongRequestStatus>("/api/song-requests/status"),
  toggle: () => api.post<{ queueOpen: boolean }>("/api/song-requests/toggle"),

  getMessages: () => api.get<SongRequestMessages>("/api/song-requests/messages"),
  updateMessages: (messages: Record<string, string>) =>
    api.put("/api/song-requests/messages", { messages }),
  resetMessage: (messageKey: string) =>
    api.post<{ key: string; value: string }>(`/api/song-requests/messages/${messageKey}/reset`),

  updateSettings: (body: { maxDuration?: number; maxPerUser?: number; pointsCost?: number }) =>
    api.put("/api/song-requests/settings", body),
};
