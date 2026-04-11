import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { commandsApi } from "../api/commands";
import { PageHeader } from "../components/ui/PageHeader";
import { SmartDataTable } from "../components/ui/DataTable";
import { ConfirmDialog } from "../components/ui/ConfirmDialog";
import { CommandForm } from "../components/features/commands/CommandForm";
import { SystemCommandList } from "../components/features/commands/SystemCommandList";
import { showToast } from "../hooks/useToast";
import { useModuleLock } from "../hooks/useModuleLock";
import { LockBanner } from "../components/ui/LockBanner";
import { PERMISSION_LABELS } from "../lib/constants";
import type { SmartColumn } from "../components/ui/DataTable";
import type { Command, SystemCommand } from "../types/commands";

export function CommandsPage() {
  const queryClient = useQueryClient();
  const { isLocked, lockReason } = useModuleLock("/commands");
  const [showCreate, setShowCreate] = useState(false);
  const [editCommand, setEditCommand] = useState<Command | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Command | null>(null);

  const { data: commands, isLoading, isError } = useQuery<Command[]>({
    queryKey: ["commands"],
    queryFn: commandsApi.getAll,
  });

  const { data: systemCommands } = useQuery<SystemCommand[]>({
    queryKey: ["systemCommands"],
    queryFn: commandsApi.getSystem,
  });

  const toggleMutation = useMutation({
    mutationFn: ({ id, isEnabled }: { id: number; isEnabled: boolean }) =>
      commandsApi.update(id, { isEnabled }),
    onSuccess: (_data, { isEnabled }) => {
      showToast("success", `Command ${isEnabled ? "enabled" : "disabled"}`);
      queryClient.invalidateQueries({ queryKey: ["commands"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const deleteMutation = useMutation({
    mutationFn: commandsApi.remove,
    onSuccess: () => {
      showToast("success", "Command deleted");
      queryClient.invalidateQueries({ queryKey: ["commands"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const columns: SmartColumn<Command>[] = [
    {
      key: "trigger",
      header: "Trigger",
      sortable: true,
      searchable: true,
      render: (_, row) => (
        <span>
          <code className="text-[var(--color-brand-text)]">{row.trigger}</code>
          {row.aliases.length > 0 && (
            <span className="ml-2 inline-flex flex-wrap gap-1">
              {row.aliases.map((a) => (
                <span key={a} className="rounded bg-[var(--color-elevated)] px-1.5 py-0.5 text-[10px] text-[var(--color-text-muted)] border border-[var(--color-border)]">{a}</span>
              ))}
            </span>
          )}
        </span>
      ),
    },
    {
      key: "responseTemplate",
      header: "Response",
      searchable: true,
      className: "max-w-[400px] truncate text-[var(--color-text-secondary)]",
    },
    {
      key: "permissionLevel",
      header: "Permission",
      sortable: true,
      render: (v) => <span className="text-[var(--color-text-secondary)]">{PERMISSION_LABELS[v as number] ?? "Unknown"}</span>,
    },
    {
      key: "useCount",
      header: "Uses",
      sortable: true,
      className: "text-right font-mono text-[var(--color-text-secondary)]",
    },
    {
      key: "isEnabled",
      header: "Active",
      sortable: true,
      render: (_, row) => (
        <button
          onClick={(e) => { e.stopPropagation(); toggleMutation.mutate({ id: row.id, isEnabled: !row.isEnabled }); }}
          disabled={isLocked}
          className={`rounded-full px-3 py-1 text-xs font-medium transition-colors disabled:opacity-40 ${
            row.isEnabled
              ? "bg-green-500/20 text-green-400 border border-green-400/30 hover:bg-green-500/30"
              : "bg-red-500/15 text-red-500 border border-red-500/30 hover:bg-red-500/25"
          }`}
        >
          {row.isEnabled ? "ON" : "OFF"}
        </button>
      ),
    },
    {
      key: "actions",
      header: "",
      className: "w-24 text-right",
      render: (_, row) => (
        <div className="space-x-2">
          <button onClick={(e) => { e.stopPropagation(); setEditCommand(row); setShowCreate(true); }} disabled={isLocked} className="text-xs text-[var(--color-text-muted)] hover:text-[var(--color-text)] transition-colors disabled:opacity-40">Edit</button>
          <button onClick={(e) => { e.stopPropagation(); setDeleteTarget(row); }} disabled={isLocked} className="text-xs text-red-400/60 hover:text-red-400 transition-colors disabled:opacity-40">Delete</button>
        </div>
      ),
    },
  ];

  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-[var(--color-text-muted)]">
        <p className="text-lg font-medium">Failed to load data</p>
        <p className="mt-1 text-sm">Please check your connection and try again.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6 p-6">
      {lockReason && <LockBanner message={lockReason} />}
      <PageHeader
        title="Commands"
        description="Manage custom chat commands for your channel."
        helpKey="commands"
        actions={
          <button
            onClick={() => {
              setEditCommand(null);
              setShowCreate(!showCreate);
            }}
            disabled={isLocked}
            className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] transition-colors disabled:opacity-40"
          >
            {showCreate && !editCommand ? "Cancel" : "+ New Command"}
          </button>
        }
      />

      {showCreate && (
        <CommandForm
          initial={editCommand}
          onClose={() => {
            setShowCreate(false);
            setEditCommand(null);
          }}
        />
      )}

      {systemCommands && systemCommands.length > 0 && (
        <SystemCommandList commands={systemCommands} />
      )}

      <SmartDataTable
        data={commands ?? []}
        columns={columns}
        pageSize={50}
        searchPlaceholder="Search commands..."
        emptyMessage="No commands yet. Create your first command!"
        isLoading={isLoading}
        getRowKey={(row) => row.id}
      />

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete Command"
        message={`Are you sure you want to delete "${deleteTarget?.trigger}"? This action cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={() => {
          if (deleteTarget) {
            deleteMutation.mutate(deleteTarget.id);
          }
          setDeleteTarget(null);
        }}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  );
}
