import { api } from "./client";
import type { EffectList, EffectTypes } from "../types/effects";

export const effectsApi = {
  getAll: () => api.get<EffectList[]>("/api/effects"),

  getById: (id: number) => api.get<EffectList>(`/api/effects/${id}`),

  create: (body: {
    name: string;
    description?: string;
    triggerTypeId: string;
    triggerConfig?: string;
    conditionsConfig?: string;
    effectsConfig?: string;
    cooldown?: number;
  }) => api.post<EffectList>("/api/effects", body),

  update: (id: number, body: {
    name?: string;
    description?: string;
    triggerTypeId?: string;
    triggerConfig?: string;
    conditionsConfig?: string;
    effectsConfig?: string;
    cooldown?: number;
    isEnabled?: boolean;
  }) => api.put<EffectList>(`/api/effects/${id}`, body),

  delete: (id: number) => api.del(`/api/effects/${id}`),

  getTypes: () => api.get<EffectTypes>("/api/effects/types"),

  test: (id: number) => api.post<{ tested: boolean; name: string }>(`/api/effects/${id}/test`),
};
