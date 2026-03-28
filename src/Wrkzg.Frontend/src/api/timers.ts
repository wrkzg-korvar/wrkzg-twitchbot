import { api } from "./client";
import type { TimedMessage, CreateTimerRequest, UpdateTimerRequest } from "../types/timers";

const BASE = "/api/timers";

export const timersApi = {
  getAll: () => api.get<TimedMessage[]>(BASE),

  create: (body: CreateTimerRequest) => api.post<void>(BASE, body),

  update: (id: number, body: UpdateTimerRequest) =>
    api.put<void>(`${BASE}/${id}`, body),

  remove: (id: number) => api.del(`${BASE}/${id}`),
};
