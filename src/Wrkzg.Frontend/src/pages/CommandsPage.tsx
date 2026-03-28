import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { commandsApi } from "../api/commands";
import { PageHeader } from "../components/ui/PageHeader";
import { CommandForm } from "../components/features/commands/CommandForm";
import { CommandTable } from "../components/features/commands/CommandTable";
import { SystemCommandList } from "../components/features/commands/SystemCommandList";
import type { Command, SystemCommand } from "../types/commands";

export function CommandsPage() {
  const [showCreate, setShowCreate] = useState(false);

  const { data: commands, isLoading } = useQuery<Command[]>({
    queryKey: ["commands"],
    queryFn: commandsApi.getAll,
  });

  const { data: systemCommands } = useQuery<SystemCommand[]>({
    queryKey: ["systemCommands"],
    queryFn: commandsApi.getSystem,
  });

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Commands"
        description="Manage custom chat commands for your channel."
        actions={
          <button
            onClick={() => setShowCreate(!showCreate)}
            className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] transition-colors"
          >
            {showCreate ? "Cancel" : "+ New Command"}
          </button>
        }
      />

      {showCreate && <CommandForm onClose={() => setShowCreate(false)} />}

      {systemCommands && systemCommands.length > 0 && (
        <SystemCommandList commands={systemCommands} />
      )}

      {isLoading ? (
        <p className="text-sm text-[var(--color-text-muted)]">Loading commands...</p>
      ) : (
        <CommandTable commands={commands ?? []} />
      )}
    </div>
  );
}
