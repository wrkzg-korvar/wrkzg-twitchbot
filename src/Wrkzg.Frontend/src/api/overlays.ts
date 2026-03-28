import { api } from "./client";
import type { OverlaySettings, OverlayUrlInfo } from "../types/overlays";

const BASE = "/api/overlays";

export const overlaysApi = {
  getSettings: (type: string) =>
    api.get<OverlaySettings>(`${BASE}/settings/${type}`),

  updateSettings: (type: string, settings: Record<string, string>) =>
    api.put<void>(`${BASE}/settings/${type}`, settings),

  getUrl: (type: string) =>
    api.get<OverlayUrlInfo>(`${BASE}/url/${type}`),
};
