import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { commandsApi } from "../../../api/commands";
import { ConfirmDialog } from "../../../components/ui/ConfirmDialog";
import { SmartDataTable } from "../../../components/ui/DataTable";
import { showToast } from "../../../hooks/useToast";
import { inputClass } from "../../../lib/constants";
import type { SmartColumn } from "../../../components/ui/DataTable";
import type { SystemCommand } from "../../../types/commands";

interface SystemCommandListProps {
  commands: SystemCommand[];
}

export function SystemCommandList({ commands }: SystemCommandListProps) {
  const queryClient = useQueryClient();
  const [editingCmd, setEditingCmd] = useState<SystemCommand | null>(null);
  const [customResponse, setCustomResponse] = useState("");
  const [showResetConfirm, setShowResetConfirm] = useState(false);
  const [resetTarget, setResetTarget] = useState<SystemCommand | null>(null);

  const toggleMutation = useMutation({
    mutationFn: (cmd: SystemCommand) => {
      const canEdit = cmd.trigger !== "!editcmd";
      return commandsApi.updateSystem(cmd.trigger, {
        customResponseTemplate: canEdit ? cmd.customResponseTemplate : null,
        isEnabled: !cmd.isEnabled,
      });
    },
    onSuccess: (_data, cmd) => {
      showToast("success", `${cmd.trigger} ${cmd.isEnabled ? "disabled" : "enabled"}`);
      queryClient.invalidateQueries({ queryKey: ["systemCommands"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const saveResponseMutation = useMutation({
    mutationFn: () => {
      if (!editingCmd) return Promise.resolve();
      return commandsApi.updateSystem(editingCmd.trigger, {
        customResponseTemplate: customResponse.trim() || null,
        isEnabled: editingCmd.isEnabled,
      });
    },
    onSuccess: () => {
      showToast("success", `Response for ${editingCmd?.trigger} updated`);
      queryClient.invalidateQueries({ queryKey: ["systemCommands"] });
      setEditingCmd(null);
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const resetMutation = useMutation({
    mutationFn: () => {
      if (!resetTarget) return Promise.resolve();
      return commandsApi.resetSystem(resetTarget.trigger);
    },
    onSuccess: () => {
      showToast("success", `${resetTarget?.trigger} reset to default`);
      queryClient.invalidateQueries({ queryKey: ["systemCommands"] });
      setCustomResponse("");
      setEditingCmd(null);
      setResetTarget(null);
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  if (commands.length === 0) {
    return null;
  }

  const columns: SmartColumn<SystemCommand>[] = [
    {
      key: "trigger",
      header: "Trigger",
      searchable: true,
      sortable: true,
      render: (_, row) => (
        <span>
          <code className="text-[var(--color-brand-text)]">{row.trigger}</code>
          {row.aliases.length > 0 && (
            <span className="ml-2 text-xs text-[var(--color-text-muted)]">
              ({row.aliases.join(", ")})
            </span>
          )}
        </span>
      ),
    },
    {
      key: "description",
      header: "Description",
      searchable: true,
      className: "text-[var(--color-text-secondary)]",
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
            toggleMutation.mutate(row);
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
      render: (_, row) => {
        const canEdit = row.trigger !== "!editcmd";
        const hasOverride = row.customResponseTemplate !== null || !row.isEnabled;
        return (
          <span className="space-x-2">
            {canEdit && (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  if (editingCmd?.trigger === row.trigger) {
                    setEditingCmd(null);
                  } else {
                    setEditingCmd(row);
                    setCustomResponse(row.customResponseTemplate ?? "");
                  }
                }}
                className="text-xs text-[var(--color-text-muted)] hover:text-[var(--color-text)] transition-colors"
              >
                {editingCmd?.trigger === row.trigger ? "Cancel" : "Edit"}
              </button>
            )}
            {canEdit && hasOverride && (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  setResetTarget(row);
                  setShowResetConfirm(true);
                }}
                className="text-xs text-[var(--color-text-muted)] hover:text-orange-400 transition-colors"
              >
                Reset
              </button>
            )}
          </span>
        );
      },
    },
  ];

  return (
    <>
      <div>
        <div className="rounded-t-lg border border-b-0 border-[var(--color-border)] bg-[var(--color-surface)] px-4 py-2">
          <h3 className="text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wider">
            System Commands
          </h3>
        </div>
        <SmartDataTable<SystemCommand>
          data={commands}
          columns={columns}
          pageSize={0}
          searchPlaceholder="Search system commands..."
          getRowKey={(row) => row.trigger}
          maxHeight="400px"
        />

        {editingCmd && (
          <div className="border border-t-0 border-[var(--color-border)] rounded-b-lg bg-[var(--color-elevated)] px-4 py-3">
            <div className="flex items-center gap-2">
              <span className="text-xs font-medium text-[var(--color-text)] shrink-0">
                {editingCmd.trigger}:
              </span>
              <input
                type="text"
                value={customResponse}
                onChange={(e) => setCustomResponse(e.target.value)}
                placeholder={editingCmd.defaultResponseTemplate ?? "Custom response..."}
                className={inputClass + " flex-1"}
              />
              <button
                onClick={() => saveResponseMutation.mutate()}
                className="rounded-lg bg-[var(--color-brand)] px-3 py-2 text-xs font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)]"
              >
                Save
              </button>
            </div>
            <p className="mt-1 text-xs text-[var(--color-text-muted)]">
              Default: {editingCmd.defaultResponseTemplate}
            </p>
          </div>
        )}
      </div>

      <ConfirmDialog
        open={showResetConfirm}
        title="Reset to Default"
        message={`Reset ${resetTarget?.trigger} to its default response? Your custom template will be removed.`}
        confirmLabel="Reset"
        variant="warning"
        onConfirm={() => {
          resetMutation.mutate();
          setShowResetConfirm(false);
        }}
        onCancel={() => {
          setShowResetConfirm(false);
          setResetTarget(null);
        }}
      />
    </>
  );
}
