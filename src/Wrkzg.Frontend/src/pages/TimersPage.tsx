import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Plus } from "lucide-react";
import { timersApi } from "../api/timers";
import { PageHeader } from "../components/ui/PageHeader";
import { SearchInput } from "../components/ui/SearchInput";
import { TimerForm } from "../components/features/timers/TimerForm";
import { TimerList } from "../components/features/timers/TimerList";
import type { TimedMessage } from "../types/timers";

export function TimersPage() {
  const [showCreate, setShowCreate] = useState(false);
  const [editingTimer, setEditingTimer] = useState<TimedMessage | null>(null);
  const [search, setSearch] = useState("");

  const { data: timers } = useQuery<TimedMessage[]>({
    queryKey: ["timers"],
    queryFn: timersApi.getAll,
  });

  const filteredTimers = (timers ?? []).filter(
    (t) =>
      search === "" ||
      t.name.toLowerCase().includes(search.toLowerCase()) ||
      t.messages.join(" ").toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Timers"
        description="Schedule recurring messages to keep your chat active."
        helpKey="timed-messages"
        actions={
          !showCreate && !editingTimer ? (
            <button
              onClick={() => setShowCreate(true)}
              className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] transition-colors"
            >
              <Plus className="h-4 w-4" /> New Timer
            </button>
          ) : undefined
        }
      />

      {showCreate && (
        <TimerForm onClose={() => setShowCreate(false)} />
      )}

      {editingTimer && (
        <TimerForm
          initial={editingTimer}
          onClose={() => setEditingTimer(null)}
        />
      )}

      {(timers ?? []).length > 3 && (
        <SearchInput
          value={search}
          onChange={setSearch}
          placeholder="Search timers..."
        />
      )}

      <TimerList
        timers={filteredTimers}
        onEdit={(timer) => {
          setShowCreate(false);
          setEditingTimer(timer);
        }}
      />

      {search !== "" && filteredTimers.length === 0 && (
        <p className="text-sm text-[var(--color-text-muted)] text-center py-4">
          No timers matching "{search}"
        </p>
      )}
    </div>
  );
}
