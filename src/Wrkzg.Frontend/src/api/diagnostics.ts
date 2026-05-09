import { api } from "./client";

interface ExportResult {
  path: string;
  fileName: string;
}

export const diagnosticsApi = {
  /**
   * Exports the current diagnostic log file to the user's Downloads folder.
   * Returns the full file path for display in a success notification.
   *
   * Uses a POST endpoint that copies the log server-side because Photino's
   * embedded WebView does not support programmatic Blob URL downloads.
   */
  exportLog: async (): Promise<ExportResult> => {
    return api.post<ExportResult>("/api/diagnostics/log/export");
  },
};
