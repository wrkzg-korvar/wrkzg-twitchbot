import { useEffect, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Clock } from "lucide-react";
import { pollsApi } from "../../../api/polls";
import { showToast } from "../../../hooks/useToast";
import type { PollResults } from "../../../types/polls";

const BAR_COLORS = ["bg-blue-500", "bg-green-500", "bg-yellow-500", "bg-purple-500", "bg-red-500"];

interface PollActiveProps {
  poll: PollResults;
}

export function PollActive({ poll }: PollActiveProps) {
  const queryClient = useQueryClient();
  const { remaining, display } = useCountdown(poll.endsAt);

  const endMutation = useMutation({
    mutationFn: pollsApi.end,
    onSuccess: () => {
      showToast("success", "Poll ended");
      queryClient.invalidateQueries({ queryKey: ["pollActive"] });
      queryClient.invalidateQueries({ queryKey: ["pollHistory"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const cancelMutation = useMutation({
    mutationFn: pollsApi.cancel,
    onSuccess: () => {
      showToast("info", "Poll cancelled");
      queryClient.invalidateQueries({ queryKey: ["pollActive"] });
      queryClient.invalidateQueries({ queryKey: ["pollHistory"] });
    },
    onError: (err: unknown) => {
      const msg = err instanceof Error ? err.message : "Failed to cancel poll";
      showToast("error", msg.replace("API Error 400: Bad Request", "No active poll to cancel"));
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
