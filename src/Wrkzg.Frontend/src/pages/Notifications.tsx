import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Send } from "lucide-react";

// ─── Types ───────────────────────────────────────────────

interface NotificationSetting {
  enabled: boolean;
  template: string;
  variables: string[];
  autoShoutout?: boolean;
}

type NotificationSettings = Record<string, NotificationSetting>;

// ─── API ─────────────────────────────────────────────────

async function fetchSettings(): Promise<NotificationSettings> {
  const res = await fetch("/api/notifications/settings");
  if (!res.ok) throw new Error("Failed to fetch notification settings");
  return res.json();
}

async function updateSetting(
  type: string,
  body: { enabled?: boolean; template?: string; autoShoutout?: boolean }
): Promise<void> {
  const res = await fetch(`/api/notifications/settings/${type}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error("Failed to save");
}

async function testNotification(type: string): Promise<string> {
  const res = await fetch(`/api/notifications/test/${type}`, { method: "POST" });
  if (!res.ok) throw new Error("Failed to send test");
  const data = await res.json();
  return data.message;
}

// ─── Labels ──────────────────────────────────────────────

const EVENT_LABELS: Record<string, { icon: string; label: string }> = {
  follow: { icon: "🎉", label: "Follow Notifications" },
  subscribe: { icon: "⭐", label: "Subscribe Notifications" },
  gift: { icon: "🎁", label: "Gift Sub Notifications" },
  resub: { icon: "⭐", label: "Resub Notifications" },
  raid: { icon: "🚀", label: "Raid Notifications" },
};

const inputClass =
  "w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2.5 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none focus:ring-1 focus:ring-[var(--color-brand)]";

// ─── Component ───────────────────────────────────────────

export function Notifications() {
  const { data: settings, isLoading } = useQuery<NotificationSettings>({
    queryKey: ["notificationSettings"],
    queryFn: fetchSettings,
  });

  if (isLoading) {
    return <div className="p-6 text-sm text-[var(--color-text-muted)]">Loading notifications...</div>;
  }

  return (
    <div className="flex h-full flex-col gap-6 overflow-y-auto p-6">
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">Event Notifications</h1>
        <p className="mt-1 text-sm text-[var(--color-text-muted)]">
          Configure chat messages for follows, subs, raids, and more.
        </p>
      </div>

      <div className="rounded-lg border border-blue-500/20 bg-blue-500/5 p-4 text-sm text-blue-300">
        <p>
          EventSub connects automatically when your Broadcaster account is linked.
          If notifications are not working, try reconnecting your Broadcaster account in{" "}
          <strong>Settings</strong> to grant updated permissions.
        </p>
      </div>

      <div className="space-y-4">
        {settings &&
          Object.entries(settings).map(([type, setting]) => (
            <NotificationCard key={type} type={type} setting={setting} />
          ))}
      </div>
    </div>
  );
}

// ─── Notification Card ───────────────────────────────────

function NotificationCard({ type, setting }: { type: string; setting: NotificationSetting }) {
  const queryClient = useQueryClient();
  const [template, setTemplate] = useState(setting.template);
  const [autoShoutout, setAutoShoutout] = useState(setting.autoShoutout ?? false);
  const [testResult, setTestResult] = useState<string | null>(null);
  const label = EVENT_LABELS[type] ?? { icon: "📢", label: type };

  useEffect(() => {
    setTemplate(setting.template);
    setAutoShoutout(setting.autoShoutout ?? false);
  }, [setting]);

  const toggleMut = useMutation({
    mutationFn: () => updateSetting(type, { enabled: !setting.enabled }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notificationSettings"] }),
  });

  const saveMut = useMutation({
    mutationFn: () =>
      updateSetting(type, {
        template,
        ...(type === "raid" ? { autoShoutout } : {}),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notificationSettings"] }),
  });

  const testMut = useMutation({
    mutationFn: () => testNotification(type),
    onSuccess: (msg) => {
      setTestResult(msg);
      setTimeout(() => setTestResult(null), 3000);
    },
  });

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
      <div className="flex items-center justify-between mb-3">
        <h3 className="text-sm font-semibold text-[var(--color-text)]">
          {label.icon} {label.label}
        </h3>
        <button
          onClick={() => toggleMut.mutate()}
          disabled={toggleMut.isPending}
          className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
            setting.enabled
              ? "bg-green-500/20 text-green-400 border border-green-400/30 hover:bg-green-500/30"
              : "bg-red-500/15 text-red-500 border border-red-500/30 hover:bg-red-500/25"
          }`}
        >
          {setting.enabled ? "ON" : "OFF"}
        </button>
      </div>

      <div className="space-y-3">
        <div>
          <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Template</label>
          <input
            type="text"
            value={template}
            onChange={(e) => setTemplate(e.target.value)}
            className={inputClass}
            maxLength={500}
          />
          <span className="text-[10px] text-[var(--color-text-muted)] mt-1 block">
            Variables: {setting.variables.map((v) => `{${v}}`).join(", ")}
          </span>
        </div>

        {type === "raid" && (
          <label className="flex items-center gap-2 text-sm text-[var(--color-text-secondary)]">
            <input
              type="checkbox"
              checked={autoShoutout}
              onChange={(e) => setAutoShoutout(e.target.checked)}
              className="rounded border-[var(--color-border)]"
            />
            Auto-shoutout raider (sends native Twitch shoutout)
          </label>
        )}

        <div className="flex items-center gap-2">
          <button
            onClick={() => saveMut.mutate()}
            disabled={saveMut.isPending}
            className="rounded-lg bg-[var(--color-brand)] px-3 py-1.5 text-xs font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
          >
            {saveMut.isPending ? "Saving..." : "Save"}
          </button>
          <button
            onClick={() => testMut.mutate()}
            disabled={testMut.isPending || !setting.enabled}
            className="flex items-center gap-1 rounded-lg bg-[var(--color-elevated)] px-3 py-1.5 text-xs font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] disabled:opacity-40 transition-colors"
          >
            <Send className="h-3 w-3" /> Test
          </button>
          {testResult && (
            <span className="text-xs text-green-400">Sent!</span>
          )}
          {saveMut.isSuccess && !testResult && (
            <span className="text-xs text-green-400">Saved!</span>
          )}
        </div>
      </div>
    </div>
  );
}
