import { useEffect, useState } from "react";
import { notify } from "../lib/notificationStore";

export type ToastType = "success" | "error" | "info";

export interface Toast {
  id: number;
  type: ToastType;
  message: string;
}

type ToastListener = (toast: Toast) => void;

const listeners = new Set<ToastListener>();
let nextId = 0;

/**
 * Shows a toast notification. Internally routes through the notification center
 * so all messages appear in the NotificationPanel drawer.
 */
export function showToast(type: ToastType, message: string): void {
  // Route through new notification center
  notify(type, message);

  // Also fire legacy listeners so ToastContainer still works during migration
  const toast: Toast = { id: nextId++, type, message };
  listeners.forEach((listener) => listener(toast));
}

export function useToast(): Toast[] {
  const [toasts, setToasts] = useState<Toast[]>([]);

  useEffect(() => {
    const listener: ToastListener = (toast) => {
      setToasts((prev) => [...prev, toast]);
      setTimeout(() => {
        setToasts((prev) => prev.filter((t) => t.id !== toast.id));
      }, 4000);
    };

    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  }, []);

  return toasts;
}
