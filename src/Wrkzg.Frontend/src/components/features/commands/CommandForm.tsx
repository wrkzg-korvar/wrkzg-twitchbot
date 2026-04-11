import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { commandsApi } from "../../../api/commands";
import { showToast } from "../../../hooks/useToast";
import { inputClass, PERMISSION_LABELS } from "../../../lib/constants";
import type { Command } from "../../../types/commands";

interface CommandFormProps {
  onClose: () => void;
  initial?: Command | null;
}

export function CommandForm({ onClose, initial }: CommandFormProps) {
  const queryClient = useQueryClient();
  const isEdit = initial != null;
  const [trigger, setTrigger] = useState(initial?.trigger ?? "!");
  const [aliases, setAliases] = useState(initial?.aliases.join(", ") ?? "");
  const [response, setResponse] = useState(initial?.responseTemplate ?? "");
  const [permission, setPermission] = useState(initial?.permissionLevel ?? 0);
  const [globalCooldown, setGlobalCooldown] = useState(initial?.globalCooldownSeconds ?? 5);
  const [userCooldown, setUserCooldown] = useState(initial?.userCooldownSeconds ?? 10);

  const saveMutation = useMutation({
    mutationFn: () => {
      const parsedAliases = aliases
        .split(",")
        .map((a) => a.trim().toLowerCase())
        .filter((a) => a.length > 0);

      if (isEdit) {
        return commandsApi.update(initial.id, {
          responseTemplate: response.trim(),
          aliases: parsedAliases,
          permissionLevel: permission,
          globalCooldownSeconds: globalCooldown,
          userCooldownSeconds: userCooldown,
        });
      }

      return commandsApi.create({
        trigger: trigger.trim().toLowerCase(),
        aliases: parsedAliases.length > 0 ? parsedAliases : undefined,
        responseTemplate: response.trim(),
        permissionLevel: permission,
        globalCooldownSeconds: globalCooldown,
        userCooldownSeconds: userCooldown,
      });
    },
    onSuccess: () => {
      showToast("success", `Command "${trigger}" ${isEdit ? "updated" : "created"}`);
      queryClient.invalidateQueries({ queryKey: ["commands"] });
      onClose();
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const canSave = trigger.length >= 2 && response.trim().length > 0;

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-5 space-y-4">
      <h3 className="text-sm font-semibold text-[var(--color-text)]">{isEdit ? `Edit "${initial.trigger}"` : "New Command"}</h3>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Trigger</label>
          <input
            type="text"
            value={trigger}
            onChange={(e) => setTrigger(e.target.value)}
            placeholder="!command"
            disabled={isEdit}
            className={inputClass + (isEdit ? " opacity-50 cursor-not-allowed" : "")}
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Response</label>
          <input
            type="text"
            value={response}
            onChange={(e) => setResponse(e.target.value)}
            placeholder="Hello {user}! Welcome to the stream."
            className={inputClass}
          />
          <div className="flex justify-end mt-1">
            <span className={`text-xs ${
              response.length > 500
                ? "text-red-500 font-semibold"
                : response.length > 400
                  ? "text-yellow-500"
                  : "text-[var(--color-text-muted)]"
            }`}>
              {response.length}/500
            </span>
          </div>
        </div>
      </div>

      <div>
        <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Aliases</label>
        <input
          type="text"
          value={aliases}
          onChange={(e) => setAliases(e.target.value)}
          placeholder="!dc, !disc"
          className={inputClass}
        />
        <span className="text-[10px] text-[var(--color-text-muted)] mt-1 block">
          Comma-separated alternative triggers for this command.
        </span>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Permission</label>
          <select
            value={permission}
            onChange={(e) => setPermission(Number(e.target.value))}
            className={inputClass}
          >
            {Object.entries(PERMISSION_LABELS).map(([val, label]) => (
              <option key={val} value={val}>{label}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Global Cooldown (seconds)</label>
          <input
            type="number"
            min={0}
            value={globalCooldown}
            onChange={(e) => setGlobalCooldown(Number(e.target.value))}
            className={inputClass}
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">User Cooldown (seconds)</label>
          <input
            type="number"
            min={0}
            value={userCooldown}
            onChange={(e) => setUserCooldown(Number(e.target.value))}
            className={inputClass}
          />
        </div>
      </div>

      <p className="text-xs text-[var(--color-text-muted)]">
        Available variables: {"{user}"}, {"{count}"}, {"{points}"}, {"{watchtime}"}, {"{random:min:max}"}
      </p>

      <div className="flex gap-2">
        <button
          onClick={() => saveMutation.mutate()}
          disabled={!canSave || saveMutation.isPending}
          className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
        >
          {saveMutation.isPending ? (isEdit ? "Saving..." : "Creating...") : (isEdit ? "Save Changes" : "Create Command")}
        </button>
        <button
          onClick={onClose}
          className="rounded-lg px-4 py-2 text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text)]"
        >
          Cancel
        </button>
      </div>
    </div>
  );
}
