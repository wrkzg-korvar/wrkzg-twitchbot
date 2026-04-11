import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { commandsApi } from "../../../api/commands";
import { ConfirmDialog } from "../../../components/ui/ConfirmDialog";
import { SmartDataTable } from "../../../components/ui/DataTable";
import { showToast } from "../../../hooks/useToast";
import { inputClass, PERMISSION_LABELS } from "../../../lib/constants";
import type { SmartColumn } from "../../../components/ui/DataTable";
import type { Command } from "../../../types/commands";

interface CommandTableProps {
  commands: Command[];
}

export function CommandTable({ commands }: CommandTableProps) {
  const queryClient = useQueryClient();
  const [editingCmd, setEditingCmd] = useState<Command | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Command | null>(null);

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
              {row.aliases.map((alias) => (
                <span
                  key={alias}
                  className="rounded bg-[var(--color-elevated)] px-1.5 py-0.5 text-[10px] text-[var(--color-text-muted)] border border-[var(--color-border)]"
                >
                  {alias}
                </span>
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
      className: "max-w-xs truncate text-[var(--color-text-secondary)]",
    },
    {
      key: "permissionLevel",
      header: "Permission",
      sortable: true,
      render: (v) => (
        <span className="text-[var(--color-text-secondary)]">
          {PERMISSION_LABELS[v as number] ?? "Unknown"}
        </span>
      ),
    },
    {
      key: "useCount",
      header: "Used",
      sortable: true,
      className: "text-center text-[var(--color-text-secondary)]",
    },
    {
      key: "isEnabled",
      header: "Active",
      sortable: true,
      className: "text-center",
      render: (_, row) => (
        <button
          onClick={(e) => {
            e.stopPropagation();
            toggleMutation.mutate({ id: row.id, isEnabled: !row.isEnabled });
          }}
          className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
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
      className: "text-right",
      render: (_, row) => (
        <div className="space-x-2">
          <button
            onClick={(e) => {
              e.stopPropagation();
              setEditingCmd(editingCmd?.id === row.id ? null : row);
            }}
            className="text-xs text-[var(--color-text-muted)] hover:text-[var(--color-text)] transition-colors"
          >
            {editingCmd?.id === row.id ? "Cancel" : "Edit"}
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              setDeleteTarget(row);
            }}
            className="text-xs text-red-400/60 hover:text-red-400 transition-colors"
          >
            Delete
          </button>
        </div>
      ),
    },
  ];

  if (commands.length === 0) {
    return (
      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-8 text-center">
        <p className="text-[var(--color-text-secondary)]">No commands yet.</p>
        <p className="mt-1 text-sm text-[var(--color-text-muted)]">
          Create your first command to get started.
        </p>
      </div>
    );
  }

  return (
    <>
      <SmartDataTable<Command>
        data={commands}
        columns={columns}
        pageSize={50}
        searchPlaceholder="Search commands..."
        emptyMessage="No commands yet."
        getRowKey={(row) => row.id}
      />

      {editingCmd && <EditCommandPanel cmd={editingCmd} onDone={() => setEditingCmd(null)} />}

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
    </>
  );
}

function EditCommandPanel({ cmd, onDone }: { cmd: Command; onDone: () => void }) {
  const queryClient = useQueryClient();
  const [response, setResponse] = useState(cmd.responseTemplate);
  const [aliases, setAliases] = useState(cmd.aliases.join(", "));
  const [permission, setPermission] = useState(cmd.permissionLevel);
  const [globalCooldown, setGlobalCooldown] = useState(cmd.globalCooldownSeconds);
  const [userCooldown, setUserCooldown] = useState(cmd.userCooldownSeconds);

  const saveMutation = useMutation({
    mutationFn: () => {
      const parsedAliases = aliases
        .split(",")
        .map((a) => a.trim().toLowerCase())
        .filter((a) => a.length > 0);

      return commandsApi.update(cmd.id, {
        responseTemplate: response.trim(),
        aliases: parsedAliases,
        permissionLevel: permission,
        globalCooldownSeconds: globalCooldown,
        userCooldownSeconds: userCooldown,
      });
    },
    onSuccess: () => {
      showToast("success", `Command "${cmd.trigger}" updated`);
      queryClient.invalidateQueries({ queryKey: ["commands"] });
      onDone();
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-elevated)] p-4 space-y-3">
      <h4 className="text-xs font-semibold text-[var(--color-text-secondary)]">
        Editing: {cmd.trigger}
      </h4>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Trigger</label>
          <input type="text" value={cmd.trigger} disabled className={inputClass + " opacity-50 cursor-not-allowed"} />
        </div>
        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Response</label>
          <input type="text" value={response} onChange={(e) => setResponse(e.target.value)} className={inputClass} />
        </div>
      </div>
      <div>
        <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Aliases</label>
        <input type="text" value={aliases} onChange={(e) => setAliases(e.target.value)} placeholder="!dc, !disc" className={inputClass} />
        <span className="text-[10px] text-[var(--color-text-muted)] mt-1 block">Comma-separated alternative triggers.</span>
      </div>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Permission</label>
          <select value={permission} onChange={(e) => setPermission(Number(e.target.value))} className={inputClass}>
            {Object.entries(PERMISSION_LABELS).map(([val, label]) => (
              <option key={val} value={val}>{label}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Global Cooldown (s)</label>
          <input type="number" min={0} value={globalCooldown} onChange={(e) => setGlobalCooldown(Number(e.target.value))} className={inputClass} />
        </div>
        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">User Cooldown (s)</label>
          <input type="number" min={0} value={userCooldown} onChange={(e) => setUserCooldown(Number(e.target.value))} className={inputClass} />
        </div>
      </div>
      <div className="flex gap-2">
        <button
          onClick={() => saveMutation.mutate()}
          disabled={saveMutation.isPending || !response.trim()}
          className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40"
        >
          {saveMutation.isPending ? "Saving..." : "Save"}
        </button>
        <button onClick={onDone} className="rounded-lg px-4 py-2 text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text)]">
          Cancel
        </button>
      </div>
    </div>
  );
}
