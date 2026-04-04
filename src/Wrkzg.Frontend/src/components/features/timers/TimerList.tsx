import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Pencil, Trash2 } from "lucide-react";
import { timersApi } from "../../../api/timers";
import { ConfirmDialog } from "../../../components/ui/ConfirmDialog";
import { DataTable } from "../../../components/ui/DataTable";
import { showToast } from "../../../hooks/useToast";
import type { TimedMessage } from "../../../types/timers";

interface TimerListProps {
  timers: TimedMessage[];
  onEdit: (timer: TimedMessage) => void;
}

export function TimerList({ timers, onEdit }: TimerListProps) {
  const queryClient = useQueryClient();
  const [deleteId, setDeleteId] = useState<number | null>(null);

  const toggleMutation = useMutation({
    mutationFn: (timer: TimedMessage) =>
      timersApi.update(timer.id, { isEnabled: !timer.isEnabled }),
    onSuccess: (_data, timer) => {
      showToast("success", `Timer "${timer.name}" ${timer.isEnabled ? "disabled" : "enabled"}`);
      queryClient.invalidateQueries({ queryKey: ["timers"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => timersApi.remove(id),
    onSuccess: () => {
      showToast("success", "Timer deleted");
      queryClient.invalidateQueries({ queryKey: ["timers"] });
      setDeleteId(null);
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  if (timers.length === 0) {
    return (
      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
        <h2 className="text-sm font-semibold text-[var(--color-text)] mb-2">Timers</h2>
        <p className="text-sm text-[var(--color-text-muted)]">No timers yet.</p>
      </div>
    );
  }

  return (
    <>
    <div>
      <div className="rounded-t-lg border border-b-0 border-[var(--color-border)] bg-[var(--color-surface)] px-4 py-3">
        <h2 className="text-sm font-semibold text-[var(--color-text)]">Timers</h2>
      </div>
      <DataTable minWidth={640} className="rounded-b-lg">
          <thead>
            <tr className="border-b border-[var(--color-border)] text-left text-xs text-[var(--color-text-muted)]">
              <th className="px-4 py-2 font-medium">Name</th>
              <th className="px-4 py-2 font-medium text-right">Messages</th>
              <th className="px-4 py-2 font-medium text-right">Interval</th>
              <th className="px-4 py-2 font-medium text-right">Min Lines</th>
              <th className="px-4 py-2 font-medium">When</th>
              <th className="px-4 py-2 font-medium">Enabled</th>
              <th className="px-4 py-2 font-medium">Actions</th>
            </tr>
          </thead>
          <tbody>
            {timers.map((timer) => (
              <tr
                key={timer.id}
                className="border-b border-[var(--color-border)] last:border-0 hover:bg-[var(--color-elevated)] transition-colors"
              >
                <td className="px-4 py-2.5 text-[var(--color-text)]">{timer.name}</td>
                <td className="px-4 py-2.5 text-right text-[var(--color-text)]">
                  {timer.messages.length}
                </td>
                <td className="px-4 py-2.5 text-right text-[var(--color-text)]">
                  {timer.intervalMinutes}m
                </td>
                <td className="px-4 py-2.5 text-right text-[var(--color-text)]">
                  {timer.minChatLines}
                </td>
                <td className="px-4 py-2.5">
                  <div className="flex gap-1">
                    {timer.runWhenOnline && (
                      <span className="inline-block rounded px-1.5 py-0.5 text-xs bg-green-500/10 text-green-700 dark:text-green-400">
                        Online
                      </span>
                    )}
                    {timer.runWhenOffline && (
                      <span className="inline-block rounded px-1.5 py-0.5 text-xs bg-blue-500/10 text-blue-700 dark:text-blue-400">
                        Offline
                      </span>
                    )}
                  </div>
                </td>
                <td className="px-4 py-2.5">
                  <button
                    type="button"
                    role="switch"
                    aria-checked={timer.isEnabled}
                    onClick={() => toggleMutation.mutate(timer)}
                    className={`relative inline-flex h-5 w-9 items-center rounded-full transition-colors ${
                      timer.isEnabled ? "bg-[var(--color-brand)]" : "bg-[var(--color-elevated)]"
                    }`}
                  >
                    <span
                      className={`inline-block h-3.5 w-3.5 rounded-full bg-white transition-transform ${
                        timer.isEnabled ? "translate-x-4" : "translate-x-0.5"
                      }`}
                    />
                  </button>
                </td>
                <td className="px-4 py-2.5">
                  <div className="flex items-center gap-1.5">
                    <button
                      onClick={() => onEdit(timer)}
                      className="rounded p-1 text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)] hover:text-[var(--color-text)] transition-colors"
                      title="Edit"
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </button>
                    <button
                      onClick={() => setDeleteId(timer.id)}
                      className="rounded p-1 text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)] hover:text-red-400 transition-colors"
                      title="Delete"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
      </DataTable>
    </div>

    <ConfirmDialog
      open={deleteId !== null}
      title="Delete Timer"
      message="Are you sure you want to delete this timer? This action cannot be undone."
      confirmLabel="Delete"
      onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
      onCancel={() => setDeleteId(null)}
    />
    </>
  );
}
