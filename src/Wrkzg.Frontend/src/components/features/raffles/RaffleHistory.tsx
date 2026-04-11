import { useEffect, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Trophy, ChevronDown, ChevronRight, RotateCcw } from "lucide-react";
import { rafflesApi } from "../../../api/raffles";
import { SmartDataTable } from "../../../components/ui/DataTable";
import { showToast } from "../../../hooks/useToast";
import { DrawHistoryList } from "./RaffleActive";
import type { SmartColumn } from "../../../components/ui/DataTable";
import type { RaffleDto, RaffleHistoryItem, RaffleTemplate } from "../../../types/raffles";

interface RaffleHistoryProps {
  items: RaffleHistoryItem[];
}

export function RaffleHistory({ items }: RaffleHistoryProps) {
  const closedItems = items.filter((r) => !r.isOpen);
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const [expandedRaffle, setExpandedRaffle] = useState<RaffleDto | null>(null);
  const [loadingExpanded, setLoadingExpanded] = useState(false);

  const handleRowClick = async (item: RaffleHistoryItem) => {
    if (expandedId === item.id) {
      setExpandedId(null);
      setExpandedRaffle(null);
      return;
    }

    setExpandedId(item.id);
    setExpandedRaffle(null);
    setLoadingExpanded(true);
    try {
      const raffle = await rafflesApi.getById(item.id);
      setExpandedRaffle(raffle);
    } catch {
      setExpandedRaffle(null);
    } finally {
      setLoadingExpanded(false);
    }
  };

  const columns: SmartColumn<RaffleHistoryItem>[] = [
    {
      key: "id",
      header: "",
      className: "w-6",
      render: (_, row) =>
        expandedId === row.id ? (
          <ChevronDown className="h-3.5 w-3.5 text-[var(--color-text-muted)]" />
        ) : (
          <ChevronRight className="h-3.5 w-3.5 text-[var(--color-text-muted)]" />
        ),
    },
    {
      key: "title",
      header: "Title",
      searchable: true,
      sortable: true,
      className: "text-[var(--color-text)]",
    },
    {
      key: "winnerName",
      header: "Winner",
      searchable: true,
      render: (v) =>
        v ? (
          <span className="flex items-center gap-1 text-[var(--color-text)]">
            <Trophy className="h-3 w-3 text-yellow-500" />
            {v as string}
          </span>
        ) : (
          <span className="text-[var(--color-text-muted)]">No winner</span>
        ),
    },
    {
      key: "entryCount",
      header: "Entries",
      sortable: true,
      className: "text-right text-[var(--color-text)]",
    },
    {
      key: "keyword",
      header: "Keyword",
      render: (v) => (
        <span className="inline-block rounded px-1.5 py-0.5 text-xs bg-[var(--color-elevated)] text-[var(--color-text-secondary)]">
          {(v as string) || "!join"}
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
        <h2 className="text-sm font-semibold text-[var(--color-text)] mb-2">Raffle History</h2>
        <p className="text-sm text-[var(--color-text-muted)]">No raffles yet.</p>
      </div>
    );
  }

  return (
    <div>
      <div className="rounded-t-lg border border-b-0 border-[var(--color-border)] bg-[var(--color-surface)] px-4 py-3">
        <h2 className="text-sm font-semibold text-[var(--color-text)]">Raffle History</h2>
      </div>
      <SmartDataTable<RaffleHistoryItem>
        data={closedItems}
        columns={columns}
        pageSize={25}
        searchPlaceholder="Search raffles..."
        emptyMessage="No raffles yet."
        getRowKey={(row) => row.id}
        onRowClick={handleRowClick}
        rowClassName={(row) =>
          expandedId === row.id ? "bg-[var(--color-elevated)]" : ""
        }
      />

      {expandedId !== null && (
        <div className="border border-t-0 border-[var(--color-border)] rounded-b-lg bg-[var(--color-elevated)] px-4 py-3">
          {loadingExpanded ? (
            <p className="text-xs text-[var(--color-text-muted)]">Loading draw history...</p>
          ) : expandedRaffle && expandedRaffle.draws && expandedRaffle.draws.length > 0 ? (
            <DrawHistoryList draws={expandedRaffle.draws} />
          ) : (
            <p className="text-xs text-[var(--color-text-muted)]">No draw history available.</p>
          )}
        </div>
      )}
    </div>
  );
}

// ---- Announcement Templates ----

export function RaffleAnnouncementTemplates() {
  const queryClient = useQueryClient();
  const [expanded, setExpanded] = useState(false);

  const { data: templates } = useQuery<RaffleTemplate[]>({
    queryKey: ["raffleTemplates"],
    queryFn: rafflesApi.getTemplates,
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
      rafflesApi.saveTemplate({ [key]: value }),
    onSuccess: () => {
      showToast("success", "Template saved");
      queryClient.invalidateQueries({ queryKey: ["raffleTemplates"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const resetMutation = useMutation({
    mutationFn: rafflesApi.resetTemplate,
    onSuccess: (_data, key) => {
      showToast("success", "Template reset to default");
      setEdits((prev) => ({ ...prev, [key]: "" }));
      queryClient.invalidateQueries({ queryKey: ["raffleTemplates"] });
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
