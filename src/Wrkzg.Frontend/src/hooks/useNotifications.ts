import { useSyncExternalStore } from "react";
import { notificationStore } from "../lib/notificationStore";
import type { Notification } from "../lib/notificationStore";

export function useNotifications(): Notification[] {
  return useSyncExternalStore(
    (cb) => notificationStore.subscribe(cb),
    notificationStore.getSnapshot,
  );
}

export function useUnreadCount(): number {
  return useSyncExternalStore(
    (cb) => notificationStore.subscribe(cb),
    notificationStore.getCountSnapshot,
  );
}
