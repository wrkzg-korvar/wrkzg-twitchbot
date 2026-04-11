import { useEffect, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Trophy, ChevronDown, ChevronRight, RotateCcw } from "lucide-react";
import { pollsApi } from "../../../api/polls";
import { SmartDataTable } from "../../../components/ui/DataTable";
import { showToast } from "../../../hooks/useToast";
import type { SmartColumn } from "../../../components/ui/DataTable";
import type { PollHistoryItem, PollTemplate } from "../../../types/polls";

interface PollHistoryProps {
  items: PollHistoryItem[];
}

export function PollHistory({ items }: PollHistoryProps) {
  const closedItems = items.filter((p) => !p.isActive);

  const columns: SmartColumn<PollHistoryItem>[] = [
    {
      key: "question",
      header: "Question",
      searchable: true,
      sortable: true,
      className: "max-w-[200px] truncate text-[var(--color-text)]",
    },
    {
      key: "winnerIndex",
      header: "Winner",
      className: "max-w-[150px] truncate",
      render: (_, row) =>
        row.winnerIndex !== null && row.options[row.winnerIndex] ? (
          <span className="flex items-center gap-1 text-[var(--color-text)]">
            <Trophy className="h-3 w-3 text-yellow-500 shrink-0" />
            <span className="truncate">{row.options[row.winnerIndex]}</span>
          </span>
        ) : (
          <span className="text-[var(--color-text-muted)]">No votes</span>
        ),
    },
    {
      key: "totalVotes",
      header: "Votes",
      sortable: true,
      className: "text-right text-[var(--color-text)]",
    },
    {
      key: "source",
      header: "Source",
      sortable: true,
      render: (v) => (
        <span
          className={`inline-block rounded px-1.5 py-0.5 text-xs ${
            v === "BotNative"
              ? "bg-blue-500/10 text-blue-400"
              : "bg-purple-500/10 text-purple-400"
          }`}
        >
          {v === "BotNative" ? "Bot" : "Twitch"}
        </span>
      ),
    },
    {
      key: "endReason",
      header: "Status",
      className: "text-xs text-[var(--color-text-muted)]",
    },
    {
      key: "createdAt",
      header: "Created",
      sortable: true,
      className: "text-xs text-[var(--color-text-muted)]",
      render: (v) => new Date(v as string).toLocaleString(),
    },
  ];

  if (closedItems.length === 0) {
    return (
      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
        <h2 className="text-sm font-semibold text-[var(--color-text)] mb-2">Poll History</h2>
        <p className="text-sm text-[var(--color-text-muted)]">No polls yet.</p>
      </div>
    );
  }

  return (
    <div>
      <div className="rounded-t-lg border border-b-0 border-[var(--color-border)] bg-[var(--color-surface)] px-4 py-3">
        <h2 className="text-sm font-semibold text-[var(--color-text)]">Poll History</h2>
      </div>
      <SmartDataTable<PollHistoryItem>
        data={closedItems}
        columns={columns}
        pageSize={25}
        searchPlaceholder="Search polls..."
        emptyMessage="No polls yet."
        getRowKey={(row) => row.id}
      />
    </div>
  );
}

export function PollAnnouncementTemplates() {
  const queryClient = useQueryClient();
  const [expanded, setExpanded] = useState(false);

  const { data: templates } = useQuery<PollTemplate[]>({
    queryKey: ["pollTemplates"],
    queryFn: pollsApi.getTemplates,
    enabled: expanded,
  });

  const [edits, setEdits] = useState<Record<string, string>>({});

  useEffect(() => {
    if (templates) {
      const initial: Record<string, string> = {};
      for (const t of templates) {
        initial[t.key] = t.current ?? "";
      }
      setEdits(initial);
    }
  }, [templates]);

  const saveMutation = useMutation({
    mutationFn: ({ key, value }: { key: string; value: string }) =>
      pollsApi.saveTemplate({ [key]: value }),
    onSuccess: () => {
      showToast("success", "Template saved");
      queryClient.invalidateQueries({ queryKey: ["pollTemplates"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const resetMutation = useMutation({
    mutationFn: pollsApi.resetTemplate,
    onSuccess: (_data, key) => {
      showToast("success", "Template reset to default");
      setEdits((prev) => ({ ...prev, [key]: "" }));
      queryClient.invalidateQueries({ queryKey: ["pollTemplates"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)]">
      <button
        onClick={() => setExpanded(!expanded)}
        className="flex w-full items-center justify-between px-4 py-3 text-left"
      >
        <h2 className="text-sm font-semibold text-[var(--color-text)]">Announcement Templates</h2>
        {expanded ? (
          <ChevronDown className="h-4 w-4 text-[var(--color-text-muted)]" />
        ) : (
          <ChevronRight className="h-4 w-4 text-[var(--color-text-muted)]" />
        )}
      </button>

      {expanded && (
        <div className="border-t border-[var(--color-border)] p-4 space-y-4">
          {!templates ? (
            <p className="text-sm text-[var(--color-text-muted)]">Loading...</p>
          ) : (
            templates.map((t) => {
              const editValue = edits[t.key] ?? "";
              const isCustom = t.current !== null && t.current.length > 0;
              const hasUnsavedChanges = editValue !== (t.current ?? "");

              return (
                <div key={t.key} className="space-y-1.5">
                  <div className="flex items-center justify-between">
                    <label className="text-xs font-medium text-[var(--color-text)]">
                      {t.description}
                      {isCustom && (
                        <span className="ml-2 inline-block rounded px-1.5 py-0.5 text-[10px] bg-[var(--color-brand-subtle)] text-[var(--color-brand-text)]">
                          custom
                        </span>
                      )}
                    </label>
                    <code className="text-[10px] text-[var(--color-text-muted)]">{t.key}</code>
                  </div>

                  <textarea
                    value={editValue}
                    onChange={(e) => setEdits((prev) => ({ ...prev, [t.key]: e.target.value }))}
                    placeholder={t.default}
                    rows={2}
                    className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none font-mono"
                  />

                  <div className="flex items-center justify-between">
                    <span className="text-[10px] text-[var(--color-text-muted)]">
                      Variables: {t.variables.map((v) => `{${v}}`).join(", ")}
                    </span>
                    <div className="flex gap-1.5">
                      {isCustom && (
                        <button
                          onClick={() => resetMutation.mutate(t.key)}
                          disabled={resetMutation.isPending}
                          className="flex items-center gap-1 rounded px-2 py-1 text-xs text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)] transition-colors"
                          title="Reset to default"
                        >
                          <RotateCcw className="h-3 w-3" /> Reset
                        </button>
                      )}
                      <button
                        onClick={() => saveMutation.mutate({ key: t.key, value: editValue })}
                        disabled={!hasUnsavedChanges || !editValue.trim() || saveMutation.isPending}
                        className="rounded bg-[var(--color-brand)] px-2 py-1 text-xs font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
                      >
                        Save
                      </button>
                    </div>
                  </div>
                </div>
              );
            })
          )}
        </div>
      )}
    </div>
  );
}
