import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Plus } from "lucide-react";
import { rafflesApi } from "../../../api/raffles";
import { showToast } from "../../../hooks/useToast";

const DURATION_PRESETS = [
  { label: "None", value: 0 },
  { label: "1 min", value: 60 },
  { label: "2 min", value: 120 },
  { label: "5 min", value: 300 },
];

export function RaffleForm() {
  const queryClient = useQueryClient();
  const [title, setTitle] = useState("");
  const [keyword, setKeyword] = useState("");
  const [duration, setDuration] = useState<number>(0);
  const [maxEntries, setMaxEntries] = useState<number | "">("");

  const createMutation = useMutation({
    mutationFn: () =>
      rafflesApi.create({
        title,
        keyword: keyword.trim() || undefined,
        durationSeconds: duration > 0 ? duration : undefined,
        maxEntries: maxEntries || undefined,
      }),
    onSuccess: () => {
      showToast("success", "Raffle started");
      setTitle("");
      setKeyword("");
      setDuration(0);
      setMaxEntries("");
      queryClient.invalidateQueries({ queryKey: ["raffleActive"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const canCreate = title.trim().length > 0;

  const inputClass =
    "w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none";

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
      <h2 className="text-sm font-semibold text-[var(--color-text)] mb-4">Create Raffle</h2>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="sm:col-span-2">
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1.5">
            Title <span className="text-red-400">*</span>
          </label>
          <input
            type="text"
            placeholder="e.g. Steam Key Giveaway"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            maxLength={200}
            className={inputClass}
          />
        </div>

        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1.5">
            Keyword
          </label>
          <input
            type="text"
            placeholder="!join"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
            maxLength={50}
            className={inputClass}
          />
          <p className="text-[11px] text-[var(--color-text-muted)] mt-1">
            Leave empty for the default keyword !join
          </p>
        </div>

        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1.5">
            Max Entries
          </label>
          <input
            type="number"
            placeholder="Unlimited"
            value={maxEntries}
            onChange={(e) => setMaxEntries(e.target.value ? Number(e.target.value) : "")}
            min={0}
            className={inputClass + " w-32"}
          />
          <p className="text-[11px] text-[var(--color-text-muted)] mt-1">
            0 or empty = unlimited
          </p>
        </div>

        <div className="sm:col-span-2">
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1.5">
            Duration
          </label>
          <div className="flex flex-wrap gap-1.5">
            {DURATION_PRESETS.map((d) => (
              <button
                key={d.value}
                onClick={() => setDuration(d.value)}
                className={`rounded px-2.5 py-1.5 text-xs transition-colors ${
                  duration === d.value
                    ? "bg-[var(--color-brand)] text-[var(--color-bg)]"
                    : "bg-[var(--color-elevated)] text-[var(--color-text-secondary)] hover:bg-[var(--color-border)]"
                }`}
              >
                {d.label}
              </button>
            ))}
          </div>
          <p className="text-[11px] text-[var(--color-text-muted)] mt-1">
            "None" lets you draw manually whenever you're ready.
          </p>
        </div>
      </div>

      <div className="mt-4 flex gap-2">
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
        <p className="mt-2 text-xs text-red-400">{(createMutation.error as Error).message}</p>
      )}
    </div>
  );
}
