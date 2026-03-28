import { api } from "./client";
import type { NotificationSettings, UpdateNotificationSettingRequest } from "../types/notifications";

const BASE = "/api/notifications";

export const notificationsApi = {
  getSettings: () => api.get<NotificationSettings>(`${BASE}/settings`),

  updateSetting: (type: string, body: UpdateNotificationSettingRequest) =>
    api.put<void>(`${BASE}/settings/${type}`, body),

  test: (type: string) =>
    api.post<{ message: string }>(`${BASE}/test/${type}`),
};
