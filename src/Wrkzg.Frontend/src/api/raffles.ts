import { api } from "./client";
import type {
  RaffleDto,
  RaffleHistoryItem,
  RaffleTemplate,
  CreateRaffleRequest,
  RedrawRequest,
} from "../types/raffles";
import type { SettingsUpdate } from "../types/common";

const BASE = "/api/raffles";

export const rafflesApi = {
  getActive: async (): Promise<RaffleDto | null> => {
    const res = await fetch(`${BASE}/active`);
    if (res.status === 404) return null;
    if (!res.ok) throw new Error("Failed to fetch active raffle");
    return res.json();
  },

  getById: (id: number) => api.get<RaffleDto>(`${BASE}/${id}`),

  getHistory: () => api.get<RaffleHistoryItem[]>(`${BASE}/history`),

  getTemplates: () => api.get<RaffleTemplate[]>(`${BASE}/templates`),

  create: (body: CreateRaffleRequest) => api.post<void>(BASE, body),

  draw: () => api.post<void>(`${BASE}/draw`),

  accept: () => api.post<void>(`${BASE}/accept`),

  redraw: (body: RedrawRequest) => api.post<void>(`${BASE}/redraw`, body),

  end: () => api.post<void>(`${BASE}/end`),

  cancel: () => api.post<void>(`${BASE}/cancel`),

  resetTemplate: (key: string) =>
    api.post<void>(`${BASE}/templates/reset/${encodeURIComponent(key)}`),

  saveTemplate: (body: SettingsUpdate) =>
    api.put<void>("/api/settings", body),
};
