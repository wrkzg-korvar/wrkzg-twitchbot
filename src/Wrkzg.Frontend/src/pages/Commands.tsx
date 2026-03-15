import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

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
          <h1 className="text-2xl font-bold text-white">Commands</h1>
          <p className="mt-1 text-sm text-gray-500">
            Manage custom chat commands for your channel.
          </p>
        </div>
        <button
          onClick={() => setShowCreate(!showCreate)}
          className="rounded-lg bg-purple-600 px-4 py-2 text-sm font-semibold text-white hover:bg-purple-700 transition-colors"
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

      {isLoading ? (
        <p className="text-sm text-gray-500">Loading commands…</p>
      ) : !commands || commands.length === 0 ? (
        <div className="rounded-lg border border-gray-800 bg-gray-900/50 p-8 text-center">
          <p className="text-gray-400">No commands yet.</p>
          <p className="mt-1 text-sm text-gray-500">
            Create your first command to get started.
          </p>
        </div>
      ) : (
        <div className="rounded-lg border border-gray-800 overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-800 bg-gray-900/80">
                <th className="px-4 py-3 text-left font-medium text-gray-400">Trigger</th>
                <th className="px-4 py-3 text-left font-medium text-gray-400">Response</th>
                <th className="px-4 py-3 text-left font-medium text-gray-400">Permission</th>
                <th className="px-4 py-3 text-center font-medium text-gray-400">Used</th>
                <th className="px-4 py-3 text-center font-medium text-gray-400">Active</th>
                <th className="px-4 py-3 text-right font-medium text-gray-400">Actions</th>
              </tr>
            </thead>
            <tbody>
              {commands.map((cmd) => (
                <tr key={cmd.id} className="border-b border-gray-800/50 hover:bg-gray-900/40">
                  <td className="px-4 py-3">
                    <code className="text-purple-400">{cmd.trigger}</code>
                    {cmd.aliases.length > 0 && (
                      <span className="ml-2 text-xs text-gray-600">
                        +{cmd.aliases.length} alias{cmd.aliases.length > 1 ? "es" : ""}
                      </span>
                    )}
                  </td>
                  <td className="max-w-xs truncate px-4 py-3 text-gray-400">
                    {cmd.responseTemplate}
                  </td>
                  <td className="px-4 py-3 text-gray-400">
                    {PERMISSION_LABELS[cmd.permissionLevel] ?? "Unknown"}
                  </td>
                  <td className="px-4 py-3 text-center text-gray-400">{cmd.useCount}</td>
                  <td className="px-4 py-3 text-center">
                    <button
                      onClick={() => toggleMutation.mutate({ id: cmd.id, isEnabled: !cmd.isEnabled })}
                      className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
                        cmd.isEnabled
                          ? "bg-green-500/20 text-green-400 hover:bg-green-500/30"
                          : "bg-gray-700/50 text-gray-500 hover:bg-gray-700"
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
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

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
          permissionLevel: 0,
          globalCooldownSeconds: 5,
          userCooldownSeconds: 10,
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
    <div className="rounded-lg border border-gray-800 bg-gray-900/50 p-5 space-y-4">
      <h3 className="text-sm font-semibold text-gray-200">New Command</h3>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div>
          <label className="block text-xs font-medium text-gray-400 mb-1">Trigger</label>
          <input
            type="text"
            value={trigger}
            onChange={(e) => setTrigger(e.target.value)}
            placeholder="!command"
            className="w-full rounded-lg border border-gray-700 bg-gray-900 px-3 py-2 text-sm text-gray-200 placeholder-gray-600 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500"
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-400 mb-1">Response</label>
          <input
            type="text"
            value={response}
            onChange={(e) => setResponse(e.target.value)}
            placeholder="Hello {user}! Welcome to the stream."
            className="w-full rounded-lg border border-gray-700 bg-gray-900 px-3 py-2 text-sm text-gray-200 placeholder-gray-600 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500"
          />
        </div>
      </div>

      <p className="text-xs text-gray-500">
        Available variables: {"{user}"}, {"{count}"}, {"{points}"}, {"{watchtime}"}, {"{random:min:max}"}
      </p>

      {error && <p className="text-sm text-red-400">{error}</p>}

      <button
        onClick={handleSubmit}
        disabled={isSaving || trigger.length < 2 || !response.trim()}
        className="rounded-lg bg-purple-600 px-4 py-2 text-sm font-semibold text-white hover:bg-purple-700 disabled:opacity-40 transition-colors"
      >
        {isSaving ? "Creating…" : "Create Command"}
      </button>
    </div>
  );
}
