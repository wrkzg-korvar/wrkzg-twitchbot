import { useState, useEffect } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Hash } from "lucide-react";
import { useSignalR } from "../hooks/useSignalR";
import { countersApi } from "../api/counters";
import { PageHeader } from "../components/ui/PageHeader";
import { CounterCard } from "../components/features/counters/CounterCard";
import { CounterForm } from "../components/features/counters/CounterForm";
import type { Counter } from "../types/counters";

export function CountersPage() {
  const queryClient = useQueryClient();
  const { isConnected: signalRConnected, on, off } = useSignalR("/hubs/chat");
  const [showCreate, setShowCreate] = useState(false);

  const { data: counters } = useQuery<Counter[]>({
    queryKey: ["counters"],
    queryFn: countersApi.getAll,
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
    <div className="space-y-6 p-6">
      <PageHeader
        title="Counters"
        description="Track deaths, wins, and more with chat commands."
        actions={
          !showCreate ? (
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
