import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { commandsApi } from "../api/commands";
import { PageHeader } from "../components/ui/PageHeader";
import { SearchInput } from "../components/ui/SearchInput";
import { CommandForm } from "../components/features/commands/CommandForm";
import { CommandTable } from "../components/features/commands/CommandTable";
import { SystemCommandList } from "../components/features/commands/SystemCommandList";
import type { Command, SystemCommand } from "../types/commands";

export function CommandsPage() {
  const [showCreate, setShowCreate] = useState(false);
  const [search, setSearch] = useState("");

  const { data: commands, isLoading, isError } = useQuery<Command[]>({
    queryKey: ["commands"],
    queryFn: commandsApi.getAll,
  });

  const { data: systemCommands } = useQuery<SystemCommand[]>({
    queryKey: ["systemCommands"],
    queryFn: commandsApi.getSystem,
  });

  const filteredCommands = (commands ?? []).filter(
    (cmd) =>
      search === "" ||
      cmd.trigger.toLowerCase().includes(search.toLowerCase()) ||
      cmd.responseTemplate.toLowerCase().includes(search.toLowerCase())
  );

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
      <PageHeader
        title="Commands"
        description="Manage custom chat commands for your channel."
        helpKey="commands"
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
        <>
          {(commands ?? []).length > 3 && (
            <SearchInput
              value={search}
              onChange={setSearch}
              placeholder="Search commands..."
            />
          )}

          <CommandTable commands={filteredCommands} />

          {search !== "" && filteredCommands.length === 0 && (
            <p className="text-sm text-[var(--color-text-muted)] text-center py-4">
              No commands matching "{search}"
            </p>
          )}
        </>
      )}
    </div>
  );
}
