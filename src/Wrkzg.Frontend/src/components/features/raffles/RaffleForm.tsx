import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Plus } from "lucide-react";
import { rafflesApi } from "../../../api/raffles";
import { showToast } from "../../../hooks/useToast";

export function RaffleForm() {
  const queryClient = useQueryClient();
  const [title, setTitle] = useState("");
  const [keyword, setKeyword] = useState("");
  const [duration, setDuration] = useState<number | "">("");
  const [maxEntries, setMaxEntries] = useState<number | "">("");

  const createMutation = useMutation({
    mutationFn: () =>
      rafflesApi.create({
        title,
        keyword: keyword.trim() || undefined,
        durationSeconds: duration || undefined,
        maxEntries: maxEntries || undefined,
      }),
    onSuccess: () => {
      showToast("success", "Raffle started");
      setTitle("");
      setKeyword("");
      setDuration("");
      setMaxEntries("");
      queryClient.invalidateQueries({ queryKey: ["raffleActive"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const canCreate = title.trim().length > 0;

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
      <h2 className="text-sm font-semibold text-[var(--color-text)] mb-4">Create Raffle</h2>

      <div className="space-y-3">
        <input
          type="text"
          placeholder="Raffle title..."
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          maxLength={200}
          className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
        />

        <input
          type="text"
          placeholder="Leave empty for !join"
          value={keyword}
          onChange={(e) => setKeyword(e.target.value)}
          maxLength={50}
          className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
        />

        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <label className="text-xs text-[var(--color-text-muted)]">Duration (s):</label>
            <input
              type="number"
              placeholder="Optional"
              value={duration}
              onChange={(e) => setDuration(e.target.value ? Number(e.target.value) : "")}
              min={0}
              className="w-24 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-1.5 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
            />
          </div>
          <div className="flex items-center gap-2">
            <label className="text-xs text-[var(--color-text-muted)]">Max entries:</label>
            <input
              type="number"
              placeholder="Optional"
              value={maxEntries}
              onChange={(e) => setMaxEntries(e.target.value ? Number(e.target.value) : "")}
              min={0}
              className="w-24 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-1.5 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
            />
          </div>
        </div>

        <div className="flex gap-2">
          <button
            onClick={() => createMutation.mutate()}
            disabled={!canCreate || createMutation.isPending}
            className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
          >
            <Plus className="h-4 w-4" />
            {createMutation.isPending ? "Creating..." : "Start Raffle"}
          </button>
        </div>

        {createMutation.isError && (
          <p className="text-xs text-red-400">{(createMutation.error as Error).message}</p>
        )}
      </div>
    </div>
  );
}
