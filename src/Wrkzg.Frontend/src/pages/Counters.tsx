import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useSignalR } from "../hooks/useSignalR";
import { Plus, Minus, RotateCcw, Pencil, Trash2, Hash } from "lucide-react";

// ─── Types ───────────────────────────────────────────────

interface Counter {
  id: number;
  name: string;
  value: number;
  trigger: string;
  responseTemplate: string;
  createdAt: string;
}

// ─── API ─────────────────────────────────────────────────

async function fetchCounters(): Promise<Counter[]> {
  const res = await fetch("/api/counters");
  if (!res.ok) throw new Error("Failed to fetch counters");
  return res.json();
}

async function createCounter(body: { name: string; value?: number; responseTemplate?: string }): Promise<void> {
  const res = await fetch("/api/counters", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const data = await res.json();
    throw new Error(data.error || "Failed to create counter");
  }
}

async function updateCounter(id: number, body: { name?: string; value?: number; responseTemplate?: string }): Promise<void> {
  const res = await fetch(`/api/counters/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const data = await res.json();
    throw new Error(data.error || "Failed to update counter");
  }
}

async function deleteCounter(id: number): Promise<void> {
  const res = await fetch(`/api/counters/${id}`, { method: "DELETE" });
  if (!res.ok) throw new Error("Failed to delete counter");
}

async function incrementCounter(id: number): Promise<void> {
  const res = await fetch(`/api/counters/${id}/increment`, { method: "POST" });
  if (!res.ok) throw new Error("Failed to increment counter");
}

async function decrementCounter(id: number): Promise<void> {
  const res = await fetch(`/api/counters/${id}/decrement`, { method: "POST" });
  if (!res.ok) throw new Error("Failed to decrement counter");
}

async function resetCounter(id: number): Promise<void> {
  const res = await fetch(`/api/counters/${id}/reset`, { method: "POST" });
  if (!res.ok) throw new Error("Failed to reset counter");
}

// ─── Component ───────────────────────────────────────────

export function Counters() {
  const queryClient = useQueryClient();
  const { isConnected: signalRConnected, on, off } = useSignalR("/hubs/chat");
  const [showCreate, setShowCreate] = useState(false);

  const { data: counters } = useQuery<Counter[]>({
    queryKey: ["counters"],
    queryFn: fetchCounters,
  });

  // Live counter updates via SignalR
  useEffect(() => {
    if (!signalRConnected) return;

    on<{ counterId: number; name: string; value: number }>("CounterUpdated", ({ counterId, value }) => {
      queryClient.setQueryData<Counter[]>(["counters"], (old) =>
        old?.map((c) => (c.id === counterId ? { ...c, value } : c)) ?? []
      );
    });

    return () => {
      off("CounterUpdated");
    };
  }, [signalRConnected, on, off, queryClient]);

  return (
    <div className="flex h-full flex-col gap-6 overflow-y-auto p-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">Counters</h1>
          <p className="mt-1 text-sm text-[var(--color-text-muted)]">
            Track deaths, wins, and more with chat commands.
          </p>
        </div>
        {!showCreate && (
          <button
            onClick={() => setShowCreate(true)}
            className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-3 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] transition-colors"
          >
            <Plus className="h-4 w-4" /> New Counter
          </button>
        )}
      </div>

      {showCreate && (
        <CreateCounterForm
          onClose={() => setShowCreate(false)}
          onCreated={() => {
            setShowCreate(false);
            queryClient.invalidateQueries({ queryKey: ["counters"] });
          }}
        />
      )}

      {counters && counters.length === 0 && !showCreate && (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-8 text-center">
          <Hash className="mx-auto h-10 w-10 text-[var(--color-text-muted)] mb-3" />
          <p className="text-sm text-[var(--color-text-muted)]">No counters yet. Create one to get started.</p>
        </div>
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {(counters ?? []).map((counter) => (
          <CounterCard key={counter.id} counter={counter} />
        ))}
      </div>
    </div>
  );
}

// ─── Create Counter Form ─────────────────────────────────

function CreateCounterForm({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [name, setName] = useState("");
  const [startValue, setStartValue] = useState(0);
  const [responseTemplate, setResponseTemplate] = useState("");

  const createMutation = useMutation({
    mutationFn: () =>
      createCounter({
        name,
        value: startValue,
        responseTemplate: responseTemplate.trim() || undefined,
      }),
    onSuccess: onCreated,
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
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
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
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
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

// ─── Counter Card ────────────────────────────────────────

function CounterCard({ counter }: { counter: Counter }) {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [editName, setEditName] = useState(counter.name);
  const [editValue, setEditValue] = useState(counter.value);
  const [editTemplate, setEditTemplate] = useState(counter.responseTemplate);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["counters"] });

  const incrementMut = useMutation({ mutationFn: () => incrementCounter(counter.id), onSuccess: invalidate });
  const decrementMut = useMutation({ mutationFn: () => decrementCounter(counter.id), onSuccess: invalidate });
  const resetMut = useMutation({ mutationFn: () => resetCounter(counter.id), onSuccess: invalidate });
  const deleteMut = useMutation({ mutationFn: () => deleteCounter(counter.id), onSuccess: invalidate });

  const updateMut = useMutation({
    mutationFn: () =>
      updateCounter(counter.id, {
        name: editName,
        value: editValue,
        responseTemplate: editTemplate,
      }),
    onSuccess: () => {
      setEditing(false);
      invalidate();
    },
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
              className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] focus:border-[var(--color-brand)] focus:outline-none"
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
              className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
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
