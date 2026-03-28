import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Plus, Minus, RotateCcw, Pencil, Trash2 } from "lucide-react";
import { countersApi } from "../../../api/counters";
import { showToast } from "../../../hooks/useToast";
import { inputClass } from "../../../lib/constants";
import type { Counter } from "../../../types/counters";

interface CounterCardProps {
  counter: Counter;
}

export function CounterCard({ counter }: CounterCardProps) {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [editName, setEditName] = useState(counter.name);
  const [editValue, setEditValue] = useState(counter.value);
  const [editTemplate, setEditTemplate] = useState(counter.responseTemplate);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["counters"] });

  const incrementMut = useMutation({ mutationFn: () => countersApi.increment(counter.id), onSuccess: invalidate });
  const decrementMut = useMutation({ mutationFn: () => countersApi.decrement(counter.id), onSuccess: invalidate });
  const resetMut = useMutation({ mutationFn: () => countersApi.reset(counter.id), onSuccess: invalidate });
  const deleteMut = useMutation({
    mutationFn: () => countersApi.remove(counter.id),
    onSuccess: () => {
      showToast("success", `Counter "${counter.name}" deleted`);
      invalidate();
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const updateMut = useMutation({
    mutationFn: () =>
      countersApi.update(counter.id, {
        name: editName,
        value: editValue,
        responseTemplate: editTemplate,
      }),
    onSuccess: () => {
      setEditing(false);
      showToast("success", "Counter updated");
      invalidate();
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  if (editing) {
    return (
      <div className="rounded-lg border border-[var(--color-brand)] bg-[var(--color-surface)] p-4">
        <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3">Edit Counter</h3>
        <div className="space-y-3">
          <div>
            <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Name</label>
            <input
              type="text"
              value={editName}
              onChange={(e) => setEditName(e.target.value)}
              maxLength={50}
              className={inputClass}
            />
          </div>
          <div>
            <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Value</label>
            <input
              type="number"
              value={editValue}
              onChange={(e) => setEditValue(Number(e.target.value))}
              className="w-32 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] focus:border-[var(--color-brand)] focus:outline-none"
            />
          </div>
          <div>
            <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Response Template</label>
            <input
              type="text"
              value={editTemplate}
              onChange={(e) => setEditTemplate(e.target.value)}
              placeholder="{name}: {value}"
              maxLength={200}
              className={inputClass}
            />
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => updateMut.mutate()}
              disabled={!editName.trim() || updateMut.isPending}
              className="rounded-lg bg-[var(--color-brand)] px-3 py-1.5 text-xs font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
            >
              {updateMut.isPending ? "Saving..." : "Save"}
            </button>
            <button
              onClick={() => {
                setEditing(false);
                setEditName(counter.name);
                setEditValue(counter.value);
                setEditTemplate(counter.responseTemplate);
              }}
              className="rounded-lg bg-[var(--color-elevated)] px-3 py-1.5 text-xs font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] transition-colors"
            >
              Cancel
            </button>
          </div>
          {updateMut.isError && (
            <p className="text-xs text-red-400">{(updateMut.error as Error).message}</p>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4 flex flex-col">
      <div className="flex items-start justify-between mb-4">
        <div>
          <h3 className="text-sm font-semibold text-[var(--color-text)]">{counter.name}</h3>
          <span className="text-xs text-[var(--color-text-muted)] font-mono">{counter.trigger}</span>
        </div>
        <div className="flex gap-1">
          <button
            onClick={() => setEditing(true)}
            className="rounded p-1 text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-elevated)] transition-colors"
            title="Edit counter"
          >
            <Pencil className="h-3.5 w-3.5" />
          </button>
          <button
            onClick={() => deleteMut.mutate()}
            disabled={deleteMut.isPending}
            className="rounded p-1 text-[var(--color-text-muted)] hover:text-red-400 hover:bg-[var(--color-elevated)] transition-colors"
            title="Delete counter"
          >
            <Trash2 className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>

      <div className="flex-1 flex items-center justify-center my-4">
        <span className="text-5xl font-bold text-[var(--color-text)] tabular-nums">{counter.value}</span>
      </div>

      <div className="flex items-center justify-center gap-2">
        <button
          onClick={() => decrementMut.mutate()}
          disabled={decrementMut.isPending}
          className="flex items-center justify-center h-10 w-10 rounded-lg bg-[var(--color-elevated)] text-[var(--color-text)] hover:bg-[var(--color-border)] disabled:opacity-40 transition-colors"
          title="Decrement"
        >
          <Minus className="h-5 w-5" />
        </button>
        <button
          onClick={() => incrementMut.mutate()}
          disabled={incrementMut.isPending}
          className="flex items-center justify-center h-12 w-12 rounded-lg bg-[var(--color-brand)] text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
          title="Increment"
        >
          <Plus className="h-6 w-6" />
        </button>
        <button
          onClick={() => resetMut.mutate()}
          disabled={resetMut.isPending}
          className="flex items-center justify-center h-10 w-10 rounded-lg bg-[var(--color-elevated)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)] disabled:opacity-40 transition-colors"
          title="Reset to zero"
        >
          <RotateCcw className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}
