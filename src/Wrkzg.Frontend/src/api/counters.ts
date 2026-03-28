import { api } from "./client";
import type { Counter, CreateCounterRequest, UpdateCounterRequest } from "../types/counters";

const BASE = "/api/counters";

export const countersApi = {
  getAll: () => api.get<Counter[]>(BASE),

  create: (body: CreateCounterRequest) => api.post<void>(BASE, body),

  update: (id: number, body: UpdateCounterRequest) =>
    api.put<void>(`${BASE}/${id}`, body),

  remove: (id: number) => api.del(`${BASE}/${id}`),

  increment: (id: number) => api.post<void>(`${BASE}/${id}/increment`),

  decrement: (id: number) => api.post<void>(`${BASE}/${id}/decrement`),

  reset: (id: number) => api.post<void>(`${BASE}/${id}/reset`),
};
