import { useEffect, useState } from "react";

export type ToastType = "success" | "error" | "info";

export interface Toast {
  id: number;
  type: ToastType;
  message: string;
}

type ToastListener = (toast: Toast) => void;

const listeners = new Set<ToastListener>();
let nextId = 0;

export function showToast(type: ToastType, message: string): void {
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
