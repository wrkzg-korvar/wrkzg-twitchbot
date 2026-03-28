import { useState, useEffect } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Send } from "lucide-react";
import { notificationsApi } from "../../../api/notifications";
import { showToast } from "../../../hooks/useToast";
import { inputClass } from "../../../lib/constants";
import type { NotificationSetting } from "../../../types/notifications";

const EVENT_LABELS: Record<string, { icon: string; label: string }> = {
  follow: { icon: "\u{1F389}", label: "Follow Notifications" },
  subscribe: { icon: "\u2B50", label: "Subscribe Notifications" },
  gift: { icon: "\u{1F381}", label: "Gift Sub Notifications" },
  resub: { icon: "\u2B50", label: "Resub Notifications" },
  raid: { icon: "\u{1F680}", label: "Raid Notifications" },
};

interface NotificationCardProps {
  type: string;
  setting: NotificationSetting;
}

export function NotificationCard({ type, setting }: NotificationCardProps) {
  const queryClient = useQueryClient();
  const [template, setTemplate] = useState(setting.template);
  const [autoShoutout, setAutoShoutout] = useState(setting.autoShoutout ?? false);
  const [testResult, setTestResult] = useState<string | null>(null);
  const label = EVENT_LABELS[type] ?? { icon: "\u{1F4E2}", label: type };

  useEffect(() => {
    setTemplate(setting.template);
    setAutoShoutout(setting.autoShoutout ?? false);
  }, [setting]);

  const toggleMut = useMutation({
    mutationFn: () => notificationsApi.updateSetting(type, { enabled: !setting.enabled }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notificationSettings"] }),
    onError: (err: Error) => showToast("error", err.message),
  });

  const saveMut = useMutation({
    mutationFn: () =>
      notificationsApi.updateSetting(type, {
        template,
        ...(type === "raid" ? { autoShoutout } : {}),
      }),
    onSuccess: () => {
      showToast("success", `${label.label} template saved`);
      queryClient.invalidateQueries({ queryKey: ["notificationSettings"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const testMut = useMutation({
    mutationFn: () => notificationsApi.test(type),
    onSuccess: (data) => {
      setTestResult(data.message);
      showToast("success", "Test notification sent");
      setTimeout(() => setTestResult(null), 3000);
    },
    onError: (err: Error) => showToast("error", err.message),
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
