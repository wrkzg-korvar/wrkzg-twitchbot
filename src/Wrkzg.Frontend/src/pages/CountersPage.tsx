import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Plus, Minus, RotateCcw, Pencil, Trash2 } from "lucide-react";
import { useSignalR } from "../hooks/useSignalR";
import { countersApi } from "../api/counters";
import { PageHeader } from "../components/ui/PageHeader";
import { SmartDataTable } from "../components/ui/DataTable";
import type { SmartColumn } from "../components/ui/DataTable";
import { ConfirmDialog } from "../components/ui/ConfirmDialog";
import { CounterForm } from "../components/features/counters/CounterForm";
import { showToast } from "../hooks/useToast";
import { inputClass } from "../lib/constants";
import type { Counter } from "../types/counters";

export function CountersPage() {
  const queryClient = useQueryClient();
  const { isConnected: signalRConnected, on, off } = useSignalR("/hubs/chat");
  const [showCreate, setShowCreate] = useState(false);
  const [editingCounter, setEditingCounter] = useState<Counter | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Counter | null>(null);

  const { data: counters, isLoading } = useQuery<Counter[]>({
    queryKey: ["counters"],
    queryFn: countersApi.getAll,
    refetchOnMount: "always",
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

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["counters"] });

  const incrementMut = useMutation({ mutationFn: (id: number) => countersApi.increment(id), onSuccess: invalidate });
  const decrementMut = useMutation({ mutationFn: (id: number) => countersApi.decrement(id), onSuccess: invalidate });
  const resetMut = useMutation({ mutationFn: (id: number) => countersApi.reset(id), onSuccess: invalidate });
  const deleteMut = useMutation({
    mutationFn: (id: number) => countersApi.remove(id),
    onSuccess: () => { showToast("success", "Counter deleted"); invalidate(); setDeleteTarget(null); },
    onError: (err: Error) => showToast("error", err.message),
  });

  const columns: SmartColumn<Counter>[] = [
    {
      key: "name",
      header: "Name",
      sortable: true,
      searchable: true,
      render: (v) => <span className="font-medium text-[var(--color-text)]">{v as string}</span>,
    },
    {
      key: "trigger",
      header: "Trigger",
      searchable: true,
      render: (v) => <code className="text-[var(--color-brand-text)]">{v as string}</code>,
    },
    {
      key: "value",
      header: "Value",
      sortable: true,
      className: "text-right",
      render: (v) => <span className="text-2xl font-bold tabular-nums text-[var(--color-text)]">{v as number}</span>,
    },
    {
      key: "responseTemplate",
      header: "Response",
      searchable: true,
      className: "max-w-[300px] truncate text-[var(--color-text-secondary)]",
    },
    {
      key: "actions",
      header: "",
      className: "w-44",
      render: (_, row) => (
        <div className="flex items-center justify-end gap-1">
          <button
            onClick={(e) => { e.stopPropagation(); decrementMut.mutate(row.id); }}
            disabled={decrementMut.isPending}
            className="flex items-center justify-center h-8 w-8 rounded-lg bg-[var(--color-elevated)] text-[var(--color-text)] hover:bg-[var(--color-border)] disabled:opacity-40 transition-colors"
            title="Decrement"
          >
            <Minus className="h-4 w-4" />
          </button>
          <button
            onClick={(e) => { e.stopPropagation(); incrementMut.mutate(row.id); }}
            disabled={incrementMut.isPending}
            className="flex items-center justify-center h-8 w-8 rounded-lg bg-[var(--color-brand)] text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
            title="Increment"
          >
            <Plus className="h-4 w-4" />
          </button>
          <button
            onClick={(e) => { e.stopPropagation(); resetMut.mutate(row.id); }}
            disabled={resetMut.isPending}
            className="flex items-center justify-center h-8 w-8 rounded-lg bg-[var(--color-elevated)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)] disabled:opacity-40 transition-colors"
            title="Reset to zero"
          >
            <RotateCcw className="h-3.5 w-3.5" />
          </button>
          <button
            onClick={(e) => { e.stopPropagation(); setEditingCounter(row); }}
            className="rounded p-1.5 text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-elevated)] transition-colors"
            title="Edit"
          >
            <Pencil className="h-3.5 w-3.5" />
          </button>
          <button
            onClick={(e) => { e.stopPropagation(); setDeleteTarget(row); }}
            disabled={deleteMut.isPending}
            className="rounded p-1.5 text-[var(--color-text-muted)] hover:text-red-400 hover:bg-[var(--color-elevated)] transition-colors"
            title="Delete"
          >
            <Trash2 className="h-3.5 w-3.5" />
          </button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Counters"
        description="Track deaths, wins, and more with chat commands."
        helpKey="counters"
        actions={
          !showCreate && !editingCounter ? (
            <button
              onClick={() => setShowCreate(true)}
              className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-3 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] transition-colors"
            >
              <Plus className="h-4 w-4" /> New Counter
            </button>
          ) : undefined
        }
      />

      {showCreate && (
        <CounterForm
          onClose={() => setShowCreate(false)}
          onCreated={() => {
            setShowCreate(false);
            invalidate();
          }}
        />
      )}

      {editingCounter && (
        <EditCounterForm
          counter={editingCounter}
          onClose={() => setEditingCounter(null)}
          onSaved={() => {
            setEditingCounter(null);
            invalidate();
          }}
        />
      )}

      <SmartDataTable
        data={counters ?? []}
        columns={columns}
        pageSize={25}
        searchPlaceholder="Search counters..."
        emptyMessage="No counters yet. Create one to get started."
        isLoading={isLoading}
        getRowKey={(row) => row.id}
      />

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete Counter"
        message={`Are you sure you want to delete "${deleteTarget?.name}"? This action cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={() => { if (deleteTarget) { deleteMut.mutate(deleteTarget.id); } }}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  );
}

function EditCounterForm({ counter, onClose, onSaved }: { counter: Counter; onClose: () => void; onSaved: () => void }) {
  const [name, setName] = useState(counter.name);
  const [value, setValue] = useState(counter.value);
  const [responseTemplate, setResponseTemplate] = useState(counter.responseTemplate);

  const updateMut = useMutation({
    mutationFn: () => countersApi.update(counter.id, { name, value, responseTemplate }),
    onSuccess: () => {
      showToast("success", "Counter updated");
      onSaved();
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  return (
    <div className="rounded-lg border border-[var(--color-brand)] bg-[var(--color-surface)] p-4">
      <h2 className="text-sm font-semibold text-[var(--color-text)] mb-4">Edit Counter: {counter.name}</h2>
      <div className="space-y-3">
        <div>
          <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Name</label>
          <input type="text" value={name} onChange={(e) => setName(e.target.value)} maxLength={50} className={inputClass} />
        </div>
        <div>
          <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Value</label>
          <input type="number" value={value} onChange={(e) => setValue(Number(e.target.value))} className="w-32 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] focus:border-[var(--color-brand)] focus:outline-none" />
        </div>
        <div>
          <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Response Template</label>
          <input type="text" value={responseTemplate} onChange={(e) => setResponseTemplate(e.target.value)} placeholder="{name}: {value}" maxLength={200} className={inputClass} />
        </div>
        <div className="flex gap-2">
          <button onClick={() => updateMut.mutate()} disabled={!name.trim() || updateMut.isPending} className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors">
            {updateMut.isPending ? "Saving..." : "Save"}
          </button>
          <button onClick={onClose} className="rounded-lg bg-[var(--color-elevated)] px-4 py-2 text-sm font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] transition-colors">
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
}
