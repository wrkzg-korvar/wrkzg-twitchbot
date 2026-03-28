import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { countersApi } from "../../../api/counters";
import { showToast } from "../../../hooks/useToast";
import { inputClass } from "../../../lib/constants";

interface CounterFormProps {
  onClose: () => void;
  onCreated: () => void;
}

export function CounterForm({ onClose, onCreated }: CounterFormProps) {
  const [name, setName] = useState("");
  const [startValue, setStartValue] = useState(0);
  const [responseTemplate, setResponseTemplate] = useState("");

  const createMutation = useMutation({
    mutationFn: () =>
      countersApi.create({
        name,
        value: startValue,
        responseTemplate: responseTemplate.trim() || undefined,
      }),
    onSuccess: () => {
      showToast("success", `Counter "${name}" created`);
      onCreated();
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const canCreate = name.trim().length > 0;

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
      <h2 className="text-sm font-semibold text-[var(--color-text)] mb-4">New Counter</h2>

      <div className="space-y-3">
        <div>
          <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Name</label>
          <input
            type="text"
            placeholder="e.g. Deaths"
            value={name}
            onChange={(e) => setName(e.target.value)}
            maxLength={50}
            className={inputClass}
          />
        </div>

        <div>
          <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Starting Value</label>
          <input
            type="number"
            value={startValue}
            onChange={(e) => setStartValue(Number(e.target.value))}
            className="w-32 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] focus:border-[var(--color-brand)] focus:outline-none"
          />
        </div>

        <div>
          <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Response Template</label>
          <input
            type="text"
            placeholder="{name}: {value}"
            value={responseTemplate}
            onChange={(e) => setResponseTemplate(e.target.value)}
            maxLength={200}
            className={inputClass}
          />
          <span className="text-[10px] text-[var(--color-text-muted)] mt-1 block">
            Use {"{name}"} and {"{value}"} as placeholders.
          </span>
        </div>

        <div className="flex gap-2">
          <button
            onClick={() => createMutation.mutate()}
            disabled={!canCreate || createMutation.isPending}
            className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
          >
            {createMutation.isPending ? "Creating..." : "Create Counter"}
          </button>
          <button
            onClick={onClose}
            className="rounded-lg bg-[var(--color-elevated)] px-4 py-2 text-sm font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] transition-colors"
          >
            Cancel
          </button>
        </div>

        {createMutation.isError && (
          <p className="text-xs text-red-400">{(createMutation.error as Error).message}</p>
        )}
      </div>
    </div>
  );
}
