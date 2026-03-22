import { useEffect, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useSignalR } from "../hooks/useSignalR";
import { Plus, X, Clock, Trophy, ChevronDown, ChevronRight, RotateCcw } from "lucide-react";

// ─── Types ───────────────────────────────────────────────

interface PollOptionResult {
  index: number;
  label: string;
  votes: number;
  percentage: number;
}

interface PollResults {
  id: number;
  question: string;
  isActive: boolean;
  source: string;
  createdBy: string;
  createdAt: string;
  endsAt: string;
  endReason: string;
  totalVotes: number;
  options: PollOptionResult[];
  winnerIndex: number | null;
}

interface PollHistoryItem {
  id: number;
  question: string;
  options: string[];
  isActive: boolean;
  source: string;
  createdBy: string;
  createdAt: string;
  endsAt: string;
  durationSeconds: number;
  endReason: string;
  totalVotes: number;
  winnerIndex: number | null;
}

// ─── API ─────────────────────────────────────────────────

async function fetchActivePoll(): Promise<PollResults | null> {
  const res = await fetch("/api/polls/active");
  if (res.status === 404) return null;
  if (!res.ok) throw new Error("Failed to fetch active poll");
  return res.json();
}

async function fetchPollHistory(): Promise<PollHistoryItem[]> {
  const res = await fetch("/api/polls/history");
  if (!res.ok) throw new Error("Failed to fetch poll history");
  return res.json();
}

interface PollTemplate {
  key: string;
  default: string;
  description: string;
  variables: string[];
  current: string | null;
}

async function fetchPollTemplates(): Promise<PollTemplate[]> {
  const res = await fetch("/api/polls/templates");
  if (!res.ok) throw new Error("Failed to fetch templates");
  return res.json();
}

// ─── Countdown Hook ──────────────────────────────────────

function useCountdown(endsAt: string | null) {
  const [remaining, setRemaining] = useState(0);

  useEffect(() => {
    if (!endsAt) return;

    const update = () => {
      const diff = Math.max(0, Math.floor((new Date(endsAt).getTime() - Date.now()) / 1000));
      setRemaining(diff);
    };

    update();
    const interval = setInterval(update, 1000);
    return () => clearInterval(interval);
  }, [endsAt]);

  const minutes = Math.floor(remaining / 60);
  const seconds = remaining % 60;
  return { remaining, display: `${minutes}:${seconds.toString().padStart(2, "0")}` };
}

// ─── Component ───────────────────────────────────────────

export function Polls() {
  const queryClient = useQueryClient();
  const { isConnected: signalRConnected, on, off } = useSignalR("/hubs/chat");

  const { data: activePoll } = useQuery<PollResults | null>({
    queryKey: ["pollActive"],
    queryFn: fetchActivePoll,
    refetchInterval: 10_000,
  });

  const { data: history } = useQuery<PollHistoryItem[]>({
    queryKey: ["pollHistory"],
    queryFn: fetchPollHistory,
  });

  // Live poll state for SignalR updates
  const [livePoll, setLivePoll] = useState<PollResults | null>(null);

  // Sync REST data into live state
  useEffect(() => {
    if (activePoll) {
      setLivePoll(activePoll);
    } else {
      setLivePoll(null);
    }
  }, [activePoll]);

  // SignalR events
  useEffect(() => {
    if (!signalRConnected) return;

    on("PollCreated", () => {
      queryClient.invalidateQueries({ queryKey: ["pollActive"] });
    });

    on<{ pollId: number; optionIndex: number }>("PollVote", ({ pollId, optionIndex }) => {
      setLivePoll((prev) => {
        if (!prev || prev.id !== pollId) return prev;
        const totalVotes = prev.totalVotes + 1;
        const options = prev.options.map((opt, i) => {
          const votes = i === optionIndex ? opt.votes + 1 : opt.votes;
          const percentage = totalVotes > 0 ? Math.round((votes / totalVotes) * 1000) / 10 : 0;
          return { ...opt, votes, percentage };
        });
        // Recalculate all percentages with new total
        const recalculated = options.map((opt) => ({
          ...opt,
          percentage: totalVotes > 0 ? Math.round((opt.votes / totalVotes) * 1000) / 10 : 0,
        }));
        return { ...prev, totalVotes, options: recalculated };
      });
    });

    on("PollEnded", () => {
      setLivePoll(null);
      queryClient.invalidateQueries({ queryKey: ["pollActive"] });
      queryClient.invalidateQueries({ queryKey: ["pollHistory"] });
    });

    return () => {
      off("PollCreated");
      off("PollVote");
      off("PollEnded");
    };
  }, [signalRConnected, on, off, queryClient]);

  return (
    <div className="flex h-full flex-col gap-6 overflow-y-auto p-6">
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">Polls</h1>
        <p className="mt-1 text-sm text-[var(--color-text-muted)]">
          Create polls, view live results, and browse history.
        </p>
      </div>

      <CreatePollForm />

      {livePoll && <ActivePoll poll={livePoll} />}

      <AnnouncementTemplates />

      <PollHistory items={history ?? []} />
    </div>
  );
}

// ─── Create Poll Form ────────────────────────────────────

function CreatePollForm() {
  const queryClient = useQueryClient();
  const [question, setQuestion] = useState("");
  const [options, setOptions] = useState(["", ""]);
  const [duration, setDuration] = useState(60);

  const createMutation = useMutation({
    mutationFn: async () => {
      const res = await fetch("/api/polls", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          question,
          options: options.filter((o) => o.trim()),
          durationSeconds: duration,
          createdBy: "Dashboard",
        }),
      });
      if (!res.ok) {
        const data = await res.json();
        throw new Error(data.error || "Failed to create poll");
      }
    },
    onSuccess: () => {
      setQuestion("");
      setOptions(["", ""]);
      setDuration(60);
      queryClient.invalidateQueries({ queryKey: ["pollActive"] });
    },
  });

  const addOption = () => {
    if (options.length < 5) {
      setOptions([...options, ""]);
    }
  };

  const removeOption = (idx: number) => {
    if (options.length > 2) {
      setOptions(options.filter((_, i) => i !== idx));
    }
  };

  const updateOption = (idx: number, value: string) => {
    setOptions(options.map((o, i) => (i === idx ? value : o)));
  };

  const validOptions = options.filter((o) => o.trim()).length >= 2;
  const canCreate = question.trim() && validOptions;

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
      <h2 className="text-sm font-semibold text-[var(--color-text)] mb-4">Create Poll</h2>

      <div className="space-y-3">
        <input
          type="text"
          placeholder="Poll question..."
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          maxLength={200}
          className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
        />

        <div className="space-y-2">
          {options.map((opt, i) => (
            <div key={i} className="flex items-center gap-2">
              <span className="text-xs text-[var(--color-text-muted)] w-5">{i + 1}.</span>
              <input
                type="text"
                placeholder={`Option ${i + 1}`}
                value={opt}
                onChange={(e) => updateOption(i, e.target.value)}
                maxLength={100}
                className="flex-1 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-1.5 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
              />
              {options.length > 2 && (
                <button
                  onClick={() => removeOption(i)}
                  className="text-[var(--color-text-muted)] hover:text-red-400 transition-colors"
                >
                  <X className="h-4 w-4" />
                </button>
              )}
            </div>
          ))}
          {options.length < 5 && (
            <button
              onClick={addOption}
              className="flex items-center gap-1 text-xs text-[var(--color-brand)] hover:text-[var(--color-brand-hover)] transition-colors"
            >
              <Plus className="h-3 w-3" /> Add option
            </button>
          )}
        </div>

        <div className="flex items-center gap-3">
          <label className="text-xs text-[var(--color-text-muted)]">Duration:</label>
          <div className="flex gap-1">
            {[30, 60, 120, 300].map((d) => (
              <button
                key={d}
                onClick={() => setDuration(d)}
                className={`rounded px-2 py-1 text-xs transition-colors ${
                  duration === d
                    ? "bg-[var(--color-brand)] text-[var(--color-bg)]"
                    : "bg-[var(--color-elevated)] text-[var(--color-text-secondary)] hover:bg-[var(--color-border)]"
                }`}
              >
                {d >= 60 ? `${d / 60}m` : `${d}s`}
              </button>
            ))}
          </div>
        </div>

        <div className="flex gap-2">
          <button
            onClick={() => createMutation.mutate()}
            disabled={!canCreate || createMutation.isPending}
            className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
          >
            {createMutation.isPending ? "Creating..." : "Start Bot Poll"}
          </button>
          <button
            disabled
            title="Requires Twitch Affiliate/Partner"
            className="rounded-lg bg-[var(--color-elevated)] px-4 py-2 text-sm font-medium text-[var(--color-text-muted)] opacity-40 cursor-not-allowed"
          >
            Start Twitch Poll
          </button>
        </div>

        {createMutation.isError && (
          <p className="text-xs text-red-400">{(createMutation.error as Error).message}</p>
        )}
      </div>
    </div>
  );
}

// ─── Announcement Templates ──────────────────────────────

function AnnouncementTemplates() {
  const queryClient = useQueryClient();
  const [expanded, setExpanded] = useState(false);

  const { data: templates } = useQuery<PollTemplate[]>({
    queryKey: ["pollTemplates"],
    queryFn: fetchPollTemplates,
    enabled: expanded,
  });

  const [edits, setEdits] = useState<Record<string, string>>({});

  // Sync edits when templates load
  useEffect(() => {
    if (templates) {
      const initial: Record<string, string> = {};
      for (const t of templates) {
        initial[t.key] = t.current ?? "";
      }
      setEdits(initial);
    }
  }, [templates]);

  const saveMutation = useMutation({
    mutationFn: async ({ key, value }: { key: string; value: string }) => {
      const res = await fetch("/api/settings", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ [key]: value }),
      });
      if (!res.ok) throw new Error("Failed to save template");
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["pollTemplates"] });
    },
  });

  const resetMutation = useMutation({
    mutationFn: async (key: string) => {
      const res = await fetch(`/api/polls/templates/reset/${encodeURIComponent(key)}`, {
        method: "POST",
      });
      if (!res.ok) throw new Error("Failed to reset template");
    },
    onSuccess: (_data, key) => {
      setEdits((prev) => ({ ...prev, [key]: "" }));
      queryClient.invalidateQueries({ queryKey: ["pollTemplates"] });
    },
  });

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)]">
      <button
        onClick={() => setExpanded(!expanded)}
        className="flex w-full items-center justify-between px-4 py-3 text-left"
      >
        <h2 className="text-sm font-semibold text-[var(--color-text)]">Announcement Templates</h2>
        {expanded ? (
          <ChevronDown className="h-4 w-4 text-[var(--color-text-muted)]" />
        ) : (
          <ChevronRight className="h-4 w-4 text-[var(--color-text-muted)]" />
        )}
      </button>

      {expanded && (
        <div className="border-t border-[var(--color-border)] p-4 space-y-4">
          {!templates ? (
            <p className="text-sm text-[var(--color-text-muted)]">Loading...</p>
          ) : (
            templates.map((t) => {
              const editValue = edits[t.key] ?? "";
              const isCustom = t.current !== null && t.current.length > 0;
              const hasUnsavedChanges = editValue !== (t.current ?? "");

              return (
                <div key={t.key} className="space-y-1.5">
                  <div className="flex items-center justify-between">
                    <label className="text-xs font-medium text-[var(--color-text)]">
                      {t.description}
                      {isCustom && (
                        <span className="ml-2 inline-block rounded px-1.5 py-0.5 text-[10px] bg-[var(--color-brand-subtle)] text-[var(--color-brand-text)]">
                          custom
                        </span>
                      )}
                    </label>
                    <code className="text-[10px] text-[var(--color-text-muted)]">{t.key}</code>
                  </div>

                  <textarea
                    value={editValue}
                    onChange={(e) => setEdits((prev) => ({ ...prev, [t.key]: e.target.value }))}
                    placeholder={t.default}
                    rows={2}
                    className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none font-mono"
                  />

                  <div className="flex items-center justify-between">
                    <span className="text-[10px] text-[var(--color-text-muted)]">
                      Variables: {t.variables.map((v) => `{${v}}`).join(", ")}
                    </span>
                    <div className="flex gap-1.5">
                      {isCustom && (
                        <button
                          onClick={() => resetMutation.mutate(t.key)}
                          disabled={resetMutation.isPending}
                          className="flex items-center gap-1 rounded px-2 py-1 text-xs text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)] transition-colors"
                          title="Reset to default"
                        >
                          <RotateCcw className="h-3 w-3" /> Reset
                        </button>
                      )}
                      <button
                        onClick={() => saveMutation.mutate({ key: t.key, value: editValue })}
                        disabled={!hasUnsavedChanges || !editValue.trim() || saveMutation.isPending}
                        className="rounded bg-[var(--color-brand)] px-2 py-1 text-xs font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
                      >
                        Save
                      </button>
                    </div>
                  </div>
                </div>
              );
            })
          )}
        </div>
      )}
    </div>
  );
}

// ─── Active Poll ─────────────────────────────────────────

function ActivePoll({ poll }: { poll: PollResults }) {
  const queryClient = useQueryClient();
  const { remaining, display } = useCountdown(poll.endsAt);

  const endMutation = useMutation({
    mutationFn: async () => {
      await fetch("/api/polls/end", { method: "POST" });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["pollActive"] });
      queryClient.invalidateQueries({ queryKey: ["pollHistory"] });
    },
  });

  const cancelMutation = useMutation({
    mutationFn: async () => {
      await fetch("/api/polls/cancel", { method: "POST" });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["pollActive"] });
      queryClient.invalidateQueries({ queryKey: ["pollHistory"] });
    },
  });

  return (
    <div className="rounded-lg border border-[var(--color-brand)] bg-[var(--color-surface)] p-4">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <span className="h-2 w-2 rounded-full bg-red-500 animate-pulse" />
          <h2 className="text-sm font-semibold text-[var(--color-text)]">Active Poll</h2>
        </div>
        <div className="flex items-center gap-2 text-xs text-[var(--color-text-muted)]">
          <Clock className="h-3.5 w-3.5" />
          <span className={remaining <= 10 ? "text-red-400 font-bold" : ""}>{display}</span>
          <span className="ml-2">{poll.totalVotes} votes</span>
        </div>
      </div>

      <h3 className="text-lg font-semibold text-[var(--color-text)] mb-4">{poll.question}</h3>

      <div className="space-y-3 mb-4">
        {poll.options.map((opt, i) => (
          <PollBar key={i} label={opt.label} votes={opt.votes} totalVotes={poll.totalVotes} index={i} />
        ))}
      </div>

      <div className="flex gap-2">
        <button
          onClick={() => endMutation.mutate()}
          disabled={endMutation.isPending}
          className="rounded-lg bg-[var(--color-brand)] px-3 py-1.5 text-xs font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
        >
          End Poll
        </button>
        <button
          onClick={() => cancelMutation.mutate()}
          disabled={cancelMutation.isPending}
          className="rounded-lg bg-[var(--color-elevated)] px-3 py-1.5 text-xs font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] disabled:opacity-40 transition-colors"
        >
          Cancel Poll
        </button>
      </div>
    </div>
  );
}

// ─── Poll Bar ────────────────────────────────────────────

const BAR_COLORS = ["bg-blue-500", "bg-green-500", "bg-yellow-500", "bg-purple-500", "bg-red-500"];

function PollBar({ label, votes, totalVotes, index }: { label: string; votes: number; totalVotes: number; index: number }) {
  const percentage = totalVotes > 0 ? (votes / totalVotes) * 100 : 0;

  return (
    <div className="space-y-1">
      <div className="flex justify-between text-sm">
        <span className="text-[var(--color-text)]">[{index + 1}] {label}</span>
        <span className="text-[var(--color-text-muted)]">{votes} ({percentage.toFixed(1)}%)</span>
      </div>
      <div className="h-6 w-full rounded-full bg-[var(--color-elevated)] overflow-hidden">
        <div
          className={`h-full rounded-full ${BAR_COLORS[index % BAR_COLORS.length]} transition-all duration-300`}
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
}

// ─── Poll History ────────────────────────────────────────

function PollHistory({ items }: { items: PollHistoryItem[] }) {
  const closedItems = items.filter((p) => !p.isActive);

  if (closedItems.length === 0) {
    return (
      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
        <h2 className="text-sm font-semibold text-[var(--color-text)] mb-2">Poll History</h2>
        <p className="text-sm text-[var(--color-text-muted)]">No polls yet.</p>
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)]">
      <div className="border-b border-[var(--color-border)] px-4 py-3">
        <h2 className="text-sm font-semibold text-[var(--color-text)]">Poll History</h2>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-[var(--color-border)] text-left text-xs text-[var(--color-text-muted)]">
              <th className="px-4 py-2 font-medium">Question</th>
              <th className="px-4 py-2 font-medium">Winner</th>
              <th className="px-4 py-2 font-medium text-right">Votes</th>
              <th className="px-4 py-2 font-medium">Source</th>
              <th className="px-4 py-2 font-medium">Status</th>
              <th className="px-4 py-2 font-medium">Created</th>
            </tr>
          </thead>
          <tbody>
            {closedItems.map((poll) => (
              <tr key={poll.id} className="border-b border-[var(--color-border)] last:border-0 hover:bg-[var(--color-elevated)] transition-colors">
                <td className="px-4 py-2.5 text-[var(--color-text)]">{poll.question}</td>
                <td className="px-4 py-2.5">
                  {poll.winnerIndex !== null && poll.options[poll.winnerIndex] ? (
                    <span className="flex items-center gap-1 text-[var(--color-text)]">
                      <Trophy className="h-3 w-3 text-yellow-500" />
                      {poll.options[poll.winnerIndex]}
                    </span>
                  ) : (
                    <span className="text-[var(--color-text-muted)]">No votes</span>
                  )}
                </td>
                <td className="px-4 py-2.5 text-right text-[var(--color-text)]">{poll.totalVotes}</td>
                <td className="px-4 py-2.5">
                  <span className={`inline-block rounded px-1.5 py-0.5 text-xs ${
                    poll.source === "BotNative"
                      ? "bg-blue-500/10 text-blue-400"
                      : "bg-purple-500/10 text-purple-400"
                  }`}>
                    {poll.source === "BotNative" ? "Bot" : "Twitch"}
                  </span>
                </td>
                <td className="px-4 py-2.5 text-xs text-[var(--color-text-muted)]">{poll.endReason}</td>
                <td className="px-4 py-2.5 text-xs text-[var(--color-text-muted)]">
                  {new Date(poll.createdAt).toLocaleString()}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
