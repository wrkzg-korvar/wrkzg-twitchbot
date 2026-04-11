import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Plus, X } from "lucide-react";
import { timersApi } from "../../../api/timers";
import { showToast } from "../../../hooks/useToast";
import { inputClass } from "../../../lib/constants";
import { Toggle } from "../../ui/Toggle";
import type { TimedMessage, CreateTimerRequest } from "../../../types/timers";

interface TimerFormProps {
  initial?: TimedMessage;
  onClose: () => void;
}

export function TimerForm({ initial, onClose }: TimerFormProps) {
  const queryClient = useQueryClient();
  const [name, setName] = useState(initial?.name ?? "");
  const [messages, setMessages] = useState<string[]>(initial?.messages ?? [""]);
  const [intervalMinutes, setIntervalMinutes] = useState(initial?.intervalMinutes ?? 5);
  const [minChatLines, setMinChatLines] = useState(initial?.minChatLines ?? 0);
  const [isEnabled, setIsEnabled] = useState(initial?.isEnabled ?? true);
  const [runWhenOnline, setRunWhenOnline] = useState(initial?.runWhenOnline ?? true);
  const [runWhenOffline, setRunWhenOffline] = useState(initial?.runWhenOffline ?? false);
  const [isAnnouncement, setIsAnnouncement] = useState(initial?.isAnnouncement ?? false);
  const [announcementColor, setAnnouncementColor] = useState(initial?.announcementColor || "primary");

  const isEditing = !!initial;

  const buildPayload = (): CreateTimerRequest => ({
    name: name.trim(),
    messages: messages.filter((m) => m.trim()),
    intervalMinutes,
    minChatLines,
    isEnabled,
    runWhenOnline,
    runWhenOffline,
    isAnnouncement,
    announcementColor: isAnnouncement ? announcementColor : undefined,
  });

  const createMutation = useMutation({
    mutationFn: () => timersApi.create(buildPayload()),
    onSuccess: () => {
      showToast("success", `Timer "${name.trim()}" created`);
      queryClient.invalidateQueries({ queryKey: ["timers"] });
      onClose();
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const updateMutation = useMutation({
    mutationFn: () => timersApi.update(initial!.id, buildPayload()),
    onSuccess: () => {
      showToast("success", `Timer "${name.trim()}" updated`);
      queryClient.invalidateQueries({ queryKey: ["timers"] });
      onClose();
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const mutation = isEditing ? updateMutation : createMutation;

  const updateMessage = (idx: number, value: string) => {
    setMessages(messages.map((m, i) => (i === idx ? value : m)));
  };

  const addMessage = () => setMessages([...messages, ""]);

  const removeMessage = (idx: number) => {
    if (messages.length > 1) {
      setMessages(messages.filter((_, i) => i !== idx));
    }
  };

  const canSave = name.trim() && messages.some((m) => m.trim()) && intervalMinutes > 0;

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
      <h2 className="text-sm font-semibold text-[var(--color-text)] mb-4">
        {isEditing ? "Edit Timer" : "Create Timer"}
      </h2>

      <div className="space-y-3">
        <div>
          <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Name</label>
          <input
            type="text"
            placeholder="Timer name..."
            value={name}
            onChange={(e) => setName(e.target.value)}
            className={inputClass}
          />
        </div>

        <div>
          <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Messages</label>
          <div className="space-y-2">
            {messages.map((msg, i) => (
              <div key={i} className="flex items-start gap-2">
                <span className="text-xs text-[var(--color-text-muted)] w-5 mt-2">{i + 1}.</span>
                <div className="flex-1">
                  <textarea
                    value={msg}
                    onChange={(e) => updateMessage(i, e.target.value)}
                    placeholder={`Message ${i + 1}...`}
                    rows={2}
                    className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
                  />
                  <div className="flex justify-end mt-0.5">
                    <span className={`text-xs ${
                      msg.length > 500
                        ? "text-red-500 font-semibold"
                        : msg.length > 400
                          ? "text-yellow-500"
                          : "text-[var(--color-text-muted)]"
                    }`}>
                      {msg.length}/500
                    </span>
                  </div>
                </div>
                {messages.length > 1 && (
                  <button
                    onClick={() => removeMessage(i)}
                    className="mt-2 text-[var(--color-text-muted)] hover:text-red-400 transition-colors"
                  >
                    <X className="h-4 w-4" />
                  </button>
                )}
              </div>
            ))}
            <button
              onClick={addMessage}
              className="flex items-center gap-1 text-xs text-[var(--color-brand)] hover:text-[var(--color-brand-hover)] transition-colors"
            >
              <Plus className="h-3 w-3" /> Add message
            </button>
          </div>
        </div>

        <div className="flex items-center gap-6">
          <div>
            <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Interval (minutes)</label>
            <input
              type="number"
              min={1}
              value={intervalMinutes}
              onChange={(e) => setIntervalMinutes(Math.max(1, parseInt(e.target.value) || 1))}
              className="w-24 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] focus:border-[var(--color-brand)] focus:outline-none"
            />
          </div>
          <div>
            <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Min Chat Lines</label>
            <input
              type="number"
              min={0}
              value={minChatLines}
              onChange={(e) => setMinChatLines(Math.max(0, parseInt(e.target.value) || 0))}
              className="w-24 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] focus:border-[var(--color-brand)] focus:outline-none"
            />
          </div>
        </div>

        <div className="flex items-center gap-6 flex-wrap">
          <Toggle label="Enabled" checked={isEnabled} onChange={setIsEnabled} />
          <Toggle label="Run When Online" checked={runWhenOnline} onChange={setRunWhenOnline} />
          <Toggle label="Run When Offline" checked={runWhenOffline} onChange={setRunWhenOffline} />
          <Toggle label="Announcement" checked={isAnnouncement} onChange={setIsAnnouncement} />
        </div>
        {isAnnouncement && (
          <div className="space-y-2">
            <p className="text-xs text-[var(--color-text-muted)]">
              Messages will appear highlighted in chat. Requires the bot account to be a moderator in your channel.
            </p>
            <div>
              <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Announcement Color</label>
              <select
                value={announcementColor}
                onChange={(e) => setAnnouncementColor(e.target.value)}
                className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] focus:border-[var(--color-brand)] focus:outline-none"
              >
                <option value="primary">Primary</option>
                <option value="blue">Blue</option>
                <option value="green">Green</option>
                <option value="orange">Orange</option>
                <option value="purple">Purple</option>
              </select>
            </div>
          </div>
        )}

        <div className="flex gap-2">
          <button
            onClick={() => mutation.mutate()}
            disabled={!canSave || mutation.isPending}
            className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
          >
            {mutation.isPending ? "Saving..." : "Save"}
          </button>
          <button
            onClick={onClose}
            className="rounded-lg bg-[var(--color-elevated)] px-4 py-2 text-sm font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] transition-colors"
          >
            Cancel
          </button>
        </div>

        {mutation.isError && (
          <p className="text-xs text-red-400">{(mutation.error as Error).message}</p>
        )}
      </div>
    </div>
  );
}
