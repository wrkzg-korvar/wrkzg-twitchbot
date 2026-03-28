import { api } from "./client";
import type { Quote, CreateQuoteRequest } from "../types/quotes";

const BASE = "/api/quotes";

export const quotesApi = {
  getAll: () => api.get<Quote[]>(BASE),

  create: (body: CreateQuoteRequest) => api.post<void>(BASE, body),

  remove: (id: number) => api.del(`${BASE}/${id}`),
};
