export type NotificationType = "success" | "error" | "info" | "warning" | "progress";

export interface Notification {
  id: string;
  type: NotificationType;
  title: string;
  message?: string;
  progress?: number;
  persistent?: boolean;
  createdAt: number;
  dismissedAt?: number;
}

type Listener = (notifications: Notification[]) => void;

class NotificationStore {
  private notifications: Notification[] = [];
  private listeners = new Set<Listener>();
  private nextId = 0;

  // Cached snapshots — only recreated when data actually changes
  private _activeCache: Notification[] = [];
  private _countCache: number = 0;

  subscribe(listener: Listener) {
    this.listeners.add(listener);
    return () => {
      this.listeners.delete(listener);
    };
  }

  // Stable references for useSyncExternalStore — NO computation, just return cache
  getSnapshot = (): Notification[] => {
    return this._activeCache;
  };

  getCountSnapshot = (): number => {
    return this._countCache;
  };

  private emit() {
    this._activeCache = this.notifications.filter((n) => !n.dismissedAt);
    this._countCache = this._activeCache.length;
    this.listeners.forEach((l) => l(this._activeCache));
  }

  notify(type: NotificationType, title: string, message?: string, persistent?: boolean): string {
    const id = `n-${this.nextId++}`;
    this.notifications.push({ id, type, title, message, persistent, createdAt: Date.now() });
    this.emit();

    if (!persistent && type !== "progress") {
      setTimeout(() => this.dismiss(id), 5000);
    }

    return id;
  }

  updateProgress(id: string, progress: number, message?: string) {
    const n = this.notifications.find((x) => x.id === id);
    if (n) {
      n.progress = progress;
      if (message) {
        n.message = message;
      }
      this.emit();
    }
  }

  dismiss(id: string) {
    const n = this.notifications.find((x) => x.id === id);
    if (n && !n.dismissedAt) {
      n.dismissedAt = Date.now();
      this.emit();
    }
  }

  getActive(): Notification[] {
    return this._activeCache;
  }

  getUnreadCount(): number {
    return this._countCache;
  }
}

export const notificationStore = new NotificationStore();

/** Drop-in replacement for showToast — routes through the notification center. */
export function notify(type: NotificationType, title: string, message?: string): void {
  notificationStore.notify(type, title, message);
}
