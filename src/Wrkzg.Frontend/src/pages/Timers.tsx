import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Plus, X, Pencil, Trash2 } from "lucide-react";

// ─── Types ───────────────────────────────────────────────

interface TimedMessage {
  id: number;
  name: string;
  messages: string[];
  nextMessageIndex: number;
  intervalMinutes: number;
  minChatLines: number;
  isEnabled: boolean;
  runWhenOnline: boolean;
  runWhenOffline: boolean;
  lastFiredAt: string | null;
  createdAt: string;
}

interface TimerFormData {
  name: string;
  messages: string[];
  intervalMinutes: number;
  minChatLines: number;
  isEnabled: boolean;
  runWhenOnline: boolean;
  runWhenOffline: boolean;
}

// ─── API ─────────────────────────────────────────────────

async function fetchTimers(): Promise<TimedMessage[]> {
  const res = await fetch("/api/timers");
  if (!res.ok) throw new Error("Failed to fetch timers");
  return res.json();
}

// ─── Component ───────────────────────────────────────────

export function Timers() {
  const queryClient = useQueryClient();

  const { data: timers } = useQuery<TimedMessage[]>({
    queryKey: ["timers"],
    queryFn: fetchTimers,
  });

  const [showCreate, setShowCreate] = useState(false);
  const [editingTimer, setEditingTimer] = useState<TimedMessage | null>(null);

  const createMutation = useMutation({
    mutationFn: async (data: TimerFormData) => {
      const res = await fetch("/api/timers", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });
      if (!res.ok) {
        const err = await res.json();
        throw new Error(err.error || "Failed to create timer");
      }
    },
    onSuccess: () => {
      setShowCreate(false);
      queryClient.invalidateQueries({ queryKey: ["timers"] });
    },
  });

  const updateMutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: Partial<TimerFormData> }) => {
      const res = await fetch(`/api/timers/${id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });
      if (!res.ok) {
        const err = await res.json();
        throw new Error(err.error || "Failed to update timer");
      }
    },
    onSuccess: () => {
      setEditingTimer(null);
      queryClient.invalidateQueries({ queryKey: ["timers"] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: number) => {
      const res = await fetch(`/api/timers/${id}`, { method: "DELETE" });
      if (!res.ok) throw new Error("Failed to delete timer");
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["timers"] });
    },
  });

  const toggleEnabled = (timer: TimedMessage) => {
    updateMutation.mutate({ id: timer.id, data: { isEnabled: !timer.isEnabled } });
  };

  return (
    <div className="flex h-full flex-col gap-6 overflow-y-auto p-6">
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">Timers</h1>
        <p className="mt-1 text-sm text-[var(--color-text-muted)]">
          Schedule recurring messages to keep your chat active.
        </p>
      </div>

      {!showCreate && !editingTimer && (
        <button
          onClick={() => setShowCreate(true)}
          className="flex w-fit items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] transition-colors"
        >
          <Plus className="h-4 w-4" /> New Timer
        </button>
      )}

      {showCreate && (
        <TimerForm
          onSave={(data) => createMutation.mutate(data)}
          onCancel={() => setShowCreate(false)}
          isPending={createMutation.isPending}
          error={createMutation.isError ? (createMutation.error as Error).message : null}
        />
      )}

      {editingTimer && (
        <TimerForm
          initial={editingTimer}
          onSave={(data) => updateMutation.mutate({ id: editingTimer.id, data })}
          onCancel={() => setEditingTimer(null)}
          isPending={updateMutation.isPending}
          error={updateMutation.isError ? (updateMutation.error as Error).message : null}
        />
      )}

      <TimerTable
        timers={timers ?? []}
        onEdit={(timer) => {
          setShowCreate(false);
          setEditingTimer(timer);
        }}
        onDelete={(id) => deleteMutation.mutate(id)}
        onToggleEnabled={toggleEnabled}
      />
    </div>
  );
}

// ─── Timer Form ──────────────────────────────────────────

function TimerForm({
  initial,
  onSave,
  onCancel,
  isPending,
  error,
}: {
  initial?: TimedMessage;
  onSave: (data: TimerFormData) => void;
  onCancel: () => void;
  isPending: boolean;
  error: string | null;
}) {
  const [name, setName] = useState(initial?.name ?? "");
  const [messages, setMessages] = useState<string[]>(initial?.messages ?? [""]);
  const [intervalMinutes, setIntervalMinutes] = useState(initial?.intervalMinutes ?? 5);
  const [minChatLines, setMinChatLines] = useState(initial?.minChatLines ?? 0);
  const [isEnabled, setIsEnabled] = useState(initial?.isEnabled ?? true);
  const [runWhenOnline, setRunWhenOnline] = useState(initial?.runWhenOnline ?? true);
  const [runWhenOffline, setRunWhenOffline] = useState(initial?.runWhenOffline ?? false);

  const updateMessage = (idx: number, value: string) => {
    setMessages(messages.map((m, i) => (i === idx ? value : m)));
  };

  const addMessage = () => {
    setMessages([...messages, ""]);
  };

  const removeMessage = (idx: number) => {
    if (messages.length > 1) {
      setMessages(messages.filter((_, i) => i !== idx));
    }
  };

  const canSave = name.trim() && messages.some((m) => m.trim()) && intervalMinutes > 0;

  const handleSave = () => {
    onSave({
      name: name.trim(),
      messages: messages.filter((m) => m.trim()),
      intervalMinutes,
      minChatLines,
      isEnabled,
      runWhenOnline,
      runWhenOffline,
    });
  };

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
      <h2 className="text-sm font-semibold text-[var(--color-text)] mb-4">
        {initial ? "Edit Timer" : "Create Timer"}
      </h2>

      <div className="space-y-3">
        <div>
          <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Name</label>
          <input
            type="text"
            placeholder="Timer name..."
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
          />
        </div>

        <div>
          <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Messages</label>
          <div className="space-y-2">
            {messages.map((msg, i) => (
              <div key={i} className="flex items-start gap-2">
                <span className="text-xs text-[var(--color-text-muted)] w-5 mt-2">{i + 1}.</span>
                <textarea
                  value={msg}
                  onChange={(e) => updateMessage(i, e.target.value)}
                  placeholder={`Message ${i + 1}...`}
                  rows={2}
                  className="flex-1 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
                />
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

        <div className="flex items-center gap-6">
          <Toggle label="Enabled" checked={isEnabled} onChange={setIsEnabled} />
          <Toggle label="Run When Online" checked={runWhenOnline} onChange={setRunWhenOnline} />
          <Toggle label="Run When Offline" checked={runWhenOffline} onChange={setRunWhenOffline} />
        </div>

        <div className="flex gap-2">
          <button
            onClick={handleSave}
            disabled={!canSave || isPending}
            className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
          >
            {isPending ? "Saving..." : "Save"}
          </button>
          <button
            onClick={onCancel}
            className="rounded-lg bg-[var(--color-elevated)] px-4 py-2 text-sm font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] transition-colors"
          >
            Cancel
          </button>
        </div>

        {error && <p className="text-xs text-red-400">{error}</p>}
      </div>
    </div>
  );
}

// ─── Toggle ──────────────────────────────────────────────

function Toggle({
  label,
  checked,
  onChange,
}: {
  label: string;
  checked: boolean;
  onChange: (value: boolean) => void;
}) {
  return (
    <label className="flex items-center gap-2 cursor-pointer">
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        onClick={() => onChange(!checked)}
        className={`relative inline-flex h-5 w-9 items-center rounded-full transition-colors ${
          checked ? "bg-[var(--color-brand)]" : "bg-[var(--color-elevated)]"
        }`}
      >
        <span
          className={`inline-block h-3.5 w-3.5 rounded-full bg-white transition-transform ${
            checked ? "translate-x-4" : "translate-x-0.5"
          }`}
        />
      </button>
      <span className="text-xs text-[var(--color-text-muted)]">{label}</span>
    </label>
  );
}

// ─── Timer Table ─────────────────────────────────────────

function TimerTable({
  timers,
  onEdit,
  onDelete,
  onToggleEnabled,
}: {
  timers: TimedMessage[];
  onEdit: (timer: TimedMessage) => void;
  onDelete: (id: number) => void;
  onToggleEnabled: (timer: TimedMessage) => void;
}) {
  if (timers.length === 0) {
    return (
      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
        <h2 className="text-sm font-semibold text-[var(--color-text)] mb-2">Timers</h2>
        <p className="text-sm text-[var(--color-text-muted)]">No timers yet.</p>
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)]">
      <div className="border-b border-[var(--color-border)] px-4 py-3">
        <h2 className="text-sm font-semibold text-[var(--color-text)]">Timers</h2>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-[var(--color-border)] text-left text-xs text-[var(--color-text-muted)]">
              <th className="px-4 py-2 font-medium">Name</th>
              <th className="px-4 py-2 font-medium text-right">Messages</th>
              <th className="px-4 py-2 font-medium text-right">Interval</th>
              <th className="px-4 py-2 font-medium text-right">Min Lines</th>
              <th className="px-4 py-2 font-medium">When</th>
              <th className="px-4 py-2 font-medium">Enabled</th>
              <th className="px-4 py-2 font-medium">Actions</th>
            </tr>
          </thead>
          <tbody>
            {timers.map((timer) => (
              <tr
                key={timer.id}
                className="border-b border-[var(--color-border)] last:border-0 hover:bg-[var(--color-elevated)] transition-colors"
              >
                <td className="px-4 py-2.5 text-[var(--color-text)]">{timer.name}</td>
                <td className="px-4 py-2.5 text-right text-[var(--color-text)]">
                  {timer.messages.length}
                </td>
                <td className="px-4 py-2.5 text-right text-[var(--color-text)]">
                  {timer.intervalMinutes}m
                </td>
                <td className="px-4 py-2.5 text-right text-[var(--color-text)]">
                  {timer.minChatLines}
                </td>
                <td className="px-4 py-2.5">
                  <div className="flex gap-1">
                    {timer.runWhenOnline && (
                      <span className="inline-block rounded px-1.5 py-0.5 text-xs bg-green-500/10 text-green-400">
                        Online
                      </span>
                    )}
                    {timer.runWhenOffline && (
                      <span className="inline-block rounded px-1.5 py-0.5 text-xs bg-blue-500/10 text-blue-400">
                        Offline
                      </span>
                    )}
                  </div>
                </td>
                <td className="px-4 py-2.5">
                  <button
                    type="button"
                    role="switch"
                    aria-checked={timer.isEnabled}
                    onClick={() => onToggleEnabled(timer)}
                    className={`relative inline-flex h-5 w-9 items-center rounded-full transition-colors ${
                      timer.isEnabled ? "bg-[var(--color-brand)]" : "bg-[var(--color-elevated)]"
                    }`}
                  >
                    <span
                      className={`inline-block h-3.5 w-3.5 rounded-full bg-white transition-transform ${
                        timer.isEnabled ? "translate-x-4" : "translate-x-0.5"
                      }`}
                    />
                  </button>
                </td>
                <td className="px-4 py-2.5">
                  <div className="flex items-center gap-1.5">
                    <button
                      onClick={() => onEdit(timer)}
                      className="rounded p-1 text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)] hover:text-[var(--color-text)] transition-colors"
                      title="Edit"
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </button>
                    <button
                      onClick={() => onDelete(timer.id)}
                      className="rounded p-1 text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)] hover:text-red-400 transition-colors"
                      title="Delete"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
