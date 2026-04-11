import { X, CheckCircle, XCircle, Info, AlertTriangle, Loader2 } from "lucide-react";
import { useNotifications } from "../../hooks/useNotifications";
import { notificationStore, type NotificationType } from "../../lib/notificationStore";

interface NotificationPanelProps {
  open: boolean;
  onClose: () => void;
}

const typeConfig: Record<NotificationType, { icon: typeof Info; borderColor: string; iconColor: string }> = {
  success: { icon: CheckCircle, borderColor: "border-l-green-500", iconColor: "text-green-500" },
  error: { icon: XCircle, borderColor: "border-l-red-500", iconColor: "text-red-500" },
  info: { icon: Info, borderColor: "border-l-blue-500", iconColor: "text-blue-500" },
  warning: { icon: AlertTriangle, borderColor: "border-l-amber-500", iconColor: "text-amber-500" },
  progress: { icon: Loader2, borderColor: "border-l-[var(--color-brand)]", iconColor: "text-[var(--color-brand)]" },
};

export function NotificationPanel({ open, onClose }: NotificationPanelProps) {
  const notifications = useNotifications();

  if (!open) {
    return null;
  }

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 z-40 bg-black/40"
        onClick={onClose}
      />

      {/* Drawer */}
      <div className="fixed right-0 top-0 z-50 flex h-full w-80 flex-col border-l border-[var(--color-border)] bg-[var(--color-bg)] shadow-xl">
        <div className="flex items-center justify-between border-b border-[var(--color-border)] px-4 py-3">
          <h2 className="text-sm font-semibold text-[var(--color-text)]">Notifications</h2>
          <button
            onClick={onClose}
            className="rounded p-1 text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)] hover:text-[var(--color-text)]"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto p-3">
          {notifications.length === 0 ? (
            <p className="py-8 text-center text-sm text-[var(--color-text-muted)]">
              No notifications
            </p>
          ) : (
            <div className="space-y-2">
              {notifications.map((n) => {
                const config = typeConfig[n.type];
                const Icon = config.icon;

                return (
                  <div
                    key={n.id}
                    className={`rounded-lg border border-[var(--color-border)] border-l-4 ${config.borderColor} bg-[var(--color-surface)] p-3`}
                  >
                    <div className="flex items-start gap-2">
                      <Icon className={`h-4 w-4 flex-shrink-0 mt-0.5 ${config.iconColor} ${n.type === "progress" ? "animate-spin" : ""}`} />
                      <div className="min-w-0 flex-1">
                        <p className="text-sm font-medium text-[var(--color-text)]">{n.title}</p>
                        {n.message && (
                          <p className="mt-0.5 text-xs text-[var(--color-text-secondary)]">{n.message}</p>
                        )}
                        {n.type === "progress" && n.progress !== undefined && (
                          <div className="mt-2">
                            <div className="h-1.5 w-full overflow-hidden rounded-full bg-[var(--color-border)]">
                              <div
                                className="h-full rounded-full bg-[var(--color-brand)] transition-all duration-300"
                                style={{ width: `${Math.min(n.progress, 100)}%` }}
                              />
                            </div>
                          </div>
                        )}
                      </div>
                      <button
                        onClick={() => notificationStore.dismiss(n.id)}
                        className="flex-shrink-0 rounded p-0.5 text-[var(--color-text-muted)] hover:text-[var(--color-text)]"
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </>
  );
}
