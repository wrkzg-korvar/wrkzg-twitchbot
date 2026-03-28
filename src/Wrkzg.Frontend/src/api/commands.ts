import { api } from "./client";
import type {
  Command,
  SystemCommand,
  CreateCommandRequest,
  UpdateCommandRequest,
  UpdateSystemCommandRequest,
} from "../types/commands";

const BASE = "/api/commands";

export const commandsApi = {
  getAll: () => api.get<Command[]>(BASE),

  create: (body: CreateCommandRequest) => api.post<void>(BASE, body),

  update: (id: number, body: UpdateCommandRequest) =>
    api.put<void>(`${BASE}/${id}`, body),

  remove: (id: number) => api.del(`${BASE}/${id}`),

  getSystem: () => api.get<SystemCommand[]>(`${BASE}/system`),

  updateSystem: (trigger: string, body: UpdateSystemCommandRequest) =>
    api.put<void>(`${BASE}/system/${encodeURIComponent(trigger)}`, body),

  resetSystem: (trigger: string) =>
    api.post<void>(`${BASE}/system/${encodeURIComponent(trigger)}/reset`),
};
