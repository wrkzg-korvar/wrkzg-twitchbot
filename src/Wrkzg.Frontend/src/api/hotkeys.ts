import { api } from "./client";
import type { HotkeyBinding } from "../types/hotkeys";

export const hotkeysApi = {
  getAll: () => api.get<HotkeyBinding[]>("/api/hotkeys"),

  create: (body: {
    keyCombination: string;
    actionType: string;
    actionPayload?: string;
    description?: string;
  }) => api.post<HotkeyBinding>("/api/hotkeys", body),

  update: (id: number, body: {
    keyCombination?: string;
    actionType?: string;
    actionPayload?: string;
    description?: string;
    isEnabled?: boolean;
  }) => api.put<HotkeyBinding>(`/api/hotkeys/${id}`, body),

  delete: (id: number) => api.del(`/api/hotkeys/${id}`),

  trigger: (id: number) => api.post<{ triggered: boolean; action: string; payload: string }>(`/api/hotkeys/${id}/trigger`),

  getPermission: () => api.get<{ globalHotkeySupported: boolean; hasPermission: boolean; platform: string }>("/api/hotkeys/permission"),
  requestPermission: () => api.post<{ hasPermission: boolean }>("/api/hotkeys/permission/request"),
};
