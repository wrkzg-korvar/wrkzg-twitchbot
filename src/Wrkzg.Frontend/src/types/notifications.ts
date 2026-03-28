export interface NotificationSetting {
  enabled: boolean;
  template: string;
  variables: string[];
  autoShoutout?: boolean;
}

export type NotificationSettings = Record<string, NotificationSetting>;

export interface UpdateNotificationSettingRequest {
  enabled?: boolean;
  template?: string;
  autoShoutout?: boolean;
}
