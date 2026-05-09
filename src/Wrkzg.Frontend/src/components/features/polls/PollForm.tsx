import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Plus, X } from "lucide-react";
import { pollsApi } from "../../../api/polls";
import { showToast } from "../../../hooks/useToast";

export function PollForm() {
  const queryClient = useQueryClient();
  const [question, setQuestion] = useState("");
  const [options, setOptions] = useState(["", ""]);
  const [duration, setDuration] = useState(60);

  const createMutation = useMutation({
    mutationFn: () =>
      pollsApi.create({
        question,
        options: options.filter((o) => o.trim()),
        durationSeconds: duration,
        createdBy: "Dashboard",
      }),
    onSuccess: () => {
      showToast("success", "Poll started");
      setQuestion("");
      setOptions(["", ""]);
      setDuration(60);
      queryClient.invalidateQueries({ queryKey: ["pollActive"] });
    },
    onError: (err: Error) => showToast("error", err.message),
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

  const inputClass =
    "rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none";

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
      <h2 className="text-sm font-semibold text-[var(--color-text)] mb-4">Create Poll</h2>

      <div className="space-y-4">
        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1.5">
            Question <span className="text-red-400">*</span>
          </label>
          <input
            type="text"
            placeholder="e.g. Which game next?"
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
            maxLength={200}
            className={inputClass + " w-full"}
          />
        </div>

        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1.5">
            Options <span className="text-red-400">*</span>
          </label>
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
                  className={inputClass + " flex-1 py-1.5"}
                />
                {options.length > 2 && (
                  <button
                    onClick={() => removeOption(i)}
                    className="text-[var(--color-text-muted)] hover:text-red-400 transition-colors"
                    title="Remove option"
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
          <p className="text-[11px] text-[var(--color-text-muted)] mt-1">
            Between 2 and 5 options.
          </p>
        </div>

        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1.5">
            Duration
          </label>
          <div className="flex gap-1.5">
            {[30, 60, 120, 300].map((d) => (
              <button
                key={d}
                onClick={() => setDuration(d)}
                className={`rounded px-2.5 py-1.5 text-xs transition-colors ${
                  duration === d
                    ? "bg-[var(--color-brand)] text-[var(--color-bg)]"
                    : "bg-[var(--color-elevated)] text-[var(--color-text-secondary)] hover:bg-[var(--color-border)]"
                }`}
              >
                {d >= 60 ? `${d / 60} min` : `${d}s`}
              </button>
            ))}
          </div>
        </div>

        <div>
          <button
            onClick={() => createMutation.mutate()}
            disabled={!canCreate || createMutation.isPending}
            className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
          >
            {createMutation.isPending ? "Creating..." : "Start Bot Poll"}
          </button>
          <p className="text-[11px] text-[var(--color-text-muted)] mt-1.5">
            Twitch-native polls require Affiliate or Partner status.
          </p>
        </div>

        {createMutation.isError && (
          <p className="text-xs text-red-400">{(createMutation.error as Error).message}</p>
        )}
      </div>
    </div>
  );
}
