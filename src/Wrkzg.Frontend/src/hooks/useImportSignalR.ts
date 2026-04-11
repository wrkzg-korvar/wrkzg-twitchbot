import { useEffect, useRef } from "react";
import { notificationStore, notify } from "../lib/notificationStore";
import type { ImportResult } from "../api/import";

/**
 * Listens for import-related SignalR events and routes them to the notification center.
 * Should be mounted once in a component that has an active SignalR connection.
 */
export function useImportSignalR(
  signalRConnected: boolean,
  on: <T>(method: string, handler: (data: T) => void) => void,
  off: (method: string) => void,
) {
  const importNotificationIdRef = useRef<string | null>(null);

  useEffect(() => {
    if (!signalRConnected) {
      return;
    }

    on<{ jobId: string; status: string; progressPercent: number }>(
      "ImportProgress",
      (data) => {
        if (!importNotificationIdRef.current) {
          importNotificationIdRef.current = notificationStore.notify(
            "progress",
            "Import running...",
            `${data.progressPercent.toFixed(0)}%`,
            true,
          );
        } else {
          notificationStore.updateProgress(
            importNotificationIdRef.current,
            data.progressPercent,
            `${data.progressPercent.toFixed(0)}%`,
          );
        }
      },
    );

    on<{ jobId: string; result: ImportResult }>(
      "ImportComplete",
      (data) => {
        if (importNotificationIdRef.current) {
          notificationStore.dismiss(importNotificationIdRef.current);
        }
        notify("success", "Import complete", data.result.summary);
        importNotificationIdRef.current = null;
      },
    );

    on<{ jobId: string; errorMessage: string }>(
      "ImportError",
      (data) => {
        if (importNotificationIdRef.current) {
          notificationStore.dismiss(importNotificationIdRef.current);
        }
        notify("error", "Import failed", data.errorMessage);
        importNotificationIdRef.current = null;
      },
    );

    return () => {
      off("ImportProgress");
      off("ImportComplete");
      off("ImportError");
    };
  }, [signalRConnected, on, off]);
}
