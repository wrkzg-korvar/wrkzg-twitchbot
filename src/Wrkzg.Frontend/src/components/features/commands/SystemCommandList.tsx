import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { commandsApi } from "../../../api/commands";
import { ConfirmDialog } from "../../../components/ui/ConfirmDialog";
import { DataTable } from "../../../components/ui/DataTable";
import { showToast } from "../../../hooks/useToast";
import { inputClass } from "../../../lib/constants";
import type { SystemCommand } from "../../../types/commands";

interface SystemCommandListProps {
  commands: SystemCommand[];
}

export function SystemCommandList({ commands }: SystemCommandListProps) {
  if (commands.length === 0) {
    return null;
  }

  return (
    <div>
      <div className="rounded-t-lg border border-b-0 border-[var(--color-border)] bg-[var(--color-surface)] px-4 py-2">
        <h3 className="text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wider">
          System Commands
        </h3>
      </div>
      <DataTable minWidth={700} maxHeight={400} className="rounded-b-lg">
          <tbody>
            {commands.map((cmd) => (
              <SystemCommandRow key={cmd.trigger} cmd={cmd} />
            ))}
          </tbody>
      </DataTable>
    </div>
  );
}

function SystemCommandRow({ cmd }: { cmd: SystemCommand }) {
  const queryClient = useQueryClient();
  const [isEditingResponse, setIsEditingResponse] = useState(false);
  const [customResponse, setCustomResponse] = useState(cmd.customResponseTemplate ?? "");
  const [showResetConfirm, setShowResetConfirm] = useState(false);

  const canEdit = cmd.trigger !== "!editcmd";
  const hasOverride = cmd.customResponseTemplate !== null || !cmd.isEnabled;

  const toggleMutation = useMutation({
    mutationFn: () =>
      commandsApi.updateSystem(cmd.trigger, {
        customResponseTemplate: canEdit ? cmd.customResponseTemplate : null,
        isEnabled: !cmd.isEnabled,
      }),
    onSuccess: () => {
      showToast("success", `${cmd.trigger} ${cmd.isEnabled ? "disabled" : "enabled"}`);
      queryClient.invalidateQueries({ queryKey: ["systemCommands"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const saveResponseMutation = useMutation({
    mutationFn: () =>
      commandsApi.updateSystem(cmd.trigger, {
        customResponseTemplate: customResponse.trim() || null,
        isEnabled: cmd.isEnabled,
      }),
    onSuccess: () => {
      showToast("success", `Response for ${cmd.trigger} updated`);
      queryClient.invalidateQueries({ queryKey: ["systemCommands"] });
      setIsEditingResponse(false);
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const resetMutation = useMutation({
    mutationFn: () => commandsApi.resetSystem(cmd.trigger),
    onSuccess: () => {
      showToast("success", `${cmd.trigger} reset to default`);
      queryClient.invalidateQueries({ queryKey: ["systemCommands"] });
      setCustomResponse("");
      setIsEditingResponse(false);
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  return (
    <>
      <tr className="border-b border-[var(--color-border)]">
        <td className="px-4 py-2.5">
          <code className="text-[var(--color-brand-text)]">{cmd.trigger}</code>
          {cmd.aliases.length > 0 && (
            <span className="ml-2 text-xs text-[var(--color-text-muted)]">
              ({cmd.aliases.join(", ")})
            </span>
          )}
        </td>
        <td className="px-4 py-2.5 text-[var(--color-text-secondary)]">{cmd.description}</td>
        <td className="px-4 py-2.5 text-center">
          <button
            onClick={() => toggleMutation.mutate()}
            className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
              cmd.isEnabled
                ? "bg-green-500/20 text-green-400 border border-green-400/30 hover:bg-green-500/30"
                : "bg-red-500/15 text-red-500 border border-red-500/30 hover:bg-red-500/25"
            }`}
          >
            {cmd.isEnabled ? "ON" : "OFF"}
          </button>
        </td>
        <td className="px-4 py-2.5 text-right space-x-2">
          {canEdit && (
            <button
              onClick={() => {
                setIsEditingResponse(!isEditingResponse);
                setCustomResponse(cmd.customResponseTemplate ?? "");
              }}
              className="text-xs text-[var(--color-text-muted)] hover:text-[var(--color-text)] transition-colors"
            >
              {isEditingResponse ? "Cancel" : "Edit"}
            </button>
          )}
          {canEdit && hasOverride && (
            <button
              onClick={() => setShowResetConfirm(true)}
              className="text-xs text-[var(--color-text-muted)] hover:text-orange-400 transition-colors"
            >
              Reset
            </button>
          )}
        </td>
      </tr>
      {isEditingResponse && (
        <tr className="border-b border-[var(--color-border)] bg-[var(--color-elevated)]">
          <td colSpan={4} className="px-4 py-3">
            <div className="flex items-center gap-2">
              <input
                type="text"
                value={customResponse}
                onChange={(e) => setCustomResponse(e.target.value)}
                placeholder={cmd.defaultResponseTemplate ?? "Custom response..."}
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
              Default: {cmd.defaultResponseTemplate}
            </p>
          </td>
        </tr>
      )}

      <ConfirmDialog
        open={showResetConfirm}
        title="Reset to Default"
        message={`Reset ${cmd.trigger} to its default response? Your custom template will be removed.`}
        confirmLabel="Reset"
        variant="warning"
        onConfirm={() => {
          resetMutation.mutate();
          setShowResetConfirm(false);
        }}
        onCancel={() => setShowResetConfirm(false)}
      />
    </>
  );
}
