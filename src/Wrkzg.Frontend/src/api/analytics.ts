import { api } from "./client";
import type { AnalyticsSession, AnalyticsSummary, AnalyticsCategory } from "../types/analytics";

export const analyticsApi = {
  getSessions: (limit = 50, offset = 0) =>
    api.get<AnalyticsSession[]>(`/api/analytics/sessions?limit=${limit}&offset=${offset}`),

  getLatestSession: () => api.get<AnalyticsSession>("/api/analytics/sessions/latest"),

  getSession: (id: number) => api.get<AnalyticsSession>(`/api/analytics/sessions/${id}`),

  getSummary: (days = 30) => api.get<AnalyticsSummary>(`/api/analytics/summary?days=${days}`),

  getCategories: (days = 30) => api.get<AnalyticsCategory[]>(`/api/analytics/categories?days=${days}`),
};
