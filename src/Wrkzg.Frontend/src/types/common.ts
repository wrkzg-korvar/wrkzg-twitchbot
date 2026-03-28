/** Generic API error response body from the backend. */
export interface ApiErrorBody {
  error?: string;
  message?: string;
}

/** Settings key-value pair used by PUT /api/settings. */
export type SettingsUpdate = Record<string, string>;
