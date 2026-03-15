import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

interface SystemCommand {
  trigger: string;
  aliases: string[];
  description: string;
}

interface Command {
  id: number;
  trigger: string;
  aliases: string[];
  responseTemplate: string;
  permissionLevel: number;
  globalCooldownSeconds: number;
  userCooldownSeconds: number;
  isEnabled: boolean;
  useCount: number;
  createdAt: string;
}

const PERMISSION_LABELS: Record<number, string> = {
  0: "Everyone",
  1: "Follower",
  2: "Subscriber",
  3: "Moderator",
  4: "Broadcaster",
};

async function fetchCommands(): Promise<Command[]> {
  const res = await fetch("/api/commands");
  if (!res.ok) throw new Error(`Failed to load commands: ${res.status}`);
  return res.json();
}

export function Commands() {
  const queryClient = useQueryClient();
  const { data: commands, isLoading } = useQuery({ queryKey: ["commands"], queryFn: fetchCommands });
  const { data: systemCommands } = useQuery({
    queryKey: ["systemCommands"],
    queryFn: async () => {
      const res = await fetch("/api/commands/system");
      if (!res.ok) throw new Error("Failed");
      return res.json() as Promise<SystemCommand[]>;
    },
  });
  const [showCreate, setShowCreate] = useState(false);

  const toggleMutation = useMutation({
    mutationFn: async ({ id, isEnabled }: { id: number; isEnabled: boolean }) => {
      await fetch(`/api/commands/${id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ isEnabled }),
      });
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["commands"] }),
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: number) => {
      await fetch(`/api/commands/${id}`, { method: "DELETE" });
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["commands"] }),
  });

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">Commands</h1>
          <p className="mt-1 text-sm text-[var(--color-text-muted)]">
            Manage custom chat commands for your channel.
          </p>
        </div>
        <button
          onClick={() => setShowCreate(!showCreate)}
          className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-[var(--color-text)] hover:bg-[var(--color-brand-hover)] transition-colors"
        >
          {showCreate ? "Cancel" : "+ New Command"}
        </button>
      </div>

      {showCreate && (
        <CreateCommandForm
          onCreated={() => {
            setShowCreate(false);
            queryClient.invalidateQueries({ queryKey: ["commands"] });
          }}
        />
      )}

      {systemCommands && systemCommands.length > 0 && (
        <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
          <div className="bg-[var(--color-surface)] px-4 py-2 border-b border-[var(--color-border)]">
            <h3 className="text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wider">
              System Commands
            </h3>
          </div>
          <table className="w-full text-sm">
            <tbody>
              {systemCommands.map((cmd) => (
                <tr key={cmd.trigger} className="border-b border-[var(--color-border)]">
                  <td className="px-4 py-2.5">
                    <code className="text-[var(--color-brand-text)]">{cmd.trigger}</code>
                    {cmd.aliases.length > 0 && (
                      <span className="ml-2 text-xs text-[var(--color-text-muted)]">
                        ({cmd.aliases.join(", ")})
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-2.5 text-[var(--color-text-secondary)]">{cmd.description}</td>
                  <td className="px-4 py-2.5 text-right">
                    <span className="rounded bg-gray-700/50 px-2 py-0.5 text-[10px] text-[var(--color-text-secondary)]">
                      SYSTEM
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {isLoading ? (
        <p className="text-sm text-[var(--color-text-muted)]">Loading commands…</p>
      ) : !commands || commands.length === 0 ? (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-8 text-center">
          <p className="text-[var(--color-text-secondary)]">No commands yet.</p>
          <p className="mt-1 text-sm text-[var(--color-text-muted)]">
            Create your first command to get started.
          </p>
        </div>
      ) : (
        <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-[var(--color-surface)]">
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-secondary)]">Trigger</th>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-secondary)]">Response</th>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-secondary)]">Permission</th>
                <th className="px-4 py-3 text-center font-medium text-[var(--color-text-secondary)]">Used</th>
                <th className="px-4 py-3 text-center font-medium text-[var(--color-text-secondary)]">Active</th>
                <th className="px-4 py-3 text-right font-medium text-[var(--color-text-secondary)]">Actions</th>
              </tr>
            </thead>
            <tbody>
              {commands.map((cmd) => (
                <tr key={cmd.id} className="border-b border-[var(--color-border)] hover:bg-[var(--color-elevated)]">
                  <td className="px-4 py-3">
                    <code className="text-[var(--color-brand-text)]">{cmd.trigger}</code>
                    {cmd.aliases.length > 0 && (
                      <span className="ml-2 text-xs text-[var(--color-text-muted)]">
                        +{cmd.aliases.length} alias{cmd.aliases.length > 1 ? "es" : ""}
                      </span>
                    )}
                  </td>
                  <td className="max-w-xs truncate px-4 py-3 text-[var(--color-text-secondary)]">
                    {cmd.responseTemplate}
                  </td>
                  <td className="px-4 py-3 text-[var(--color-text-secondary)]">
                    {PERMISSION_LABELS[cmd.permissionLevel] ?? "Unknown"}
                  </td>
                  <td className="px-4 py-3 text-center text-[var(--color-text-secondary)]">{cmd.useCount}</td>
                  <td className="px-4 py-3 text-center">
                    <button
                      onClick={() => toggleMutation.mutate({ id: cmd.id, isEnabled: !cmd.isEnabled })}
                      className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
                        cmd.isEnabled
                          ? "bg-green-500/20 text-green-400 hover:bg-green-500/30"
                          : "bg-gray-700/50 text-[var(--color-text-muted)] hover:bg-gray-700"
                      }`}
                    >
                      {cmd.isEnabled ? "ON" : "OFF"}
                    </button>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => {
                        if (confirm(`Delete command ${cmd.trigger}?`)) {
                          deleteMutation.mutate(cmd.id);
                        }
                      }}
                      className="text-xs text-red-400/60 hover:text-red-400 transition-colors"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function CreateCommandForm({ onCreated }: { onCreated: () => void }) {
  const [trigger, setTrigger] = useState("!");
  const [response, setResponse] = useState("");
  const [permission, setPermission] = useState(0);
  const [globalCooldown, setGlobalCooldown] = useState(5);
  const [userCooldown, setUserCooldown] = useState(10);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const inputClass =
    "w-full appearance-none rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2.5 text-sm text-[var(--color-text)] placeholder-gray-600 focus:border-[var(--color-brand)] focus:outline-none focus:ring-1 focus:ring-[var(--color-brand)]";

  const handleSubmit = async () => {
    setIsSaving(true);
    setError(null);

    try {
      const res = await fetch("/api/commands", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          trigger: trigger.trim().toLowerCase(),
          responseTemplate: response.trim(),
          permissionLevel: permission,
          globalCooldownSeconds: globalCooldown,
          userCooldownSeconds: userCooldown,
        }),
      });

      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        throw new Error(data.error || `Failed (${res.status})`);
      }

      onCreated();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create command.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-5 space-y-4">
      <h3 className="text-sm font-semibold text-[var(--color-text)]">New Command</h3>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Trigger</label>
          <input
            type="text"
            value={trigger}
            onChange={(e) => setTrigger(e.target.value)}
            placeholder="!command"
            className={inputClass}
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
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <div>
          <label className="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Permission</label>
          <select
            value={permission}
            onChange={(e) => setPermission(Number(e.target.value))}
            className={inputClass}
          >
            <option value={0}>Everyone</option>
            <option value={1}>Follower</option>
            <option value={2}>Subscriber</option>
            <option value={3}>Moderator</option>
            <option value={4}>Broadcaster</option>
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

      {error && <p className="text-sm text-red-400">{error}</p>}

      <button
        onClick={handleSubmit}
        disabled={isSaving || trigger.length < 2 || !response.trim()}
        className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-[var(--color-text)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
      >
        {isSaving ? "Creating…" : "Create Command"}
      </button>
    </div>
  );
}
