import { useState } from "react";
import { useQuery, useQueryClient, useMutation } from "@tanstack/react-query";
import { Plus, Trash2 } from "lucide-react";
import { quotesApi } from "../api/quotes";
import { PageHeader } from "../components/ui/PageHeader";
import { SmartDataTable } from "../components/ui/DataTable";
import type { SmartColumn } from "../components/ui/DataTable";
import { ConfirmDialog } from "../components/ui/ConfirmDialog";
import { QuoteForm } from "../components/features/quotes/QuoteForm";
import { showToast } from "../hooks/useToast";
import { useModuleLock } from "../hooks/useModuleLock";
import { LockBanner } from "../components/ui/LockBanner";
import type { Quote } from "../types/quotes";

const columns: SmartColumn<Quote>[] = [
  {
    key: "number",
    header: "#",
    sortable: true,
    className: "w-16",
    render: (v) => <span className="font-mono text-[var(--color-text-muted)]">{v as number}</span>,
  },
  {
    key: "text",
    header: "Text",
    searchable: true,
    render: (v) => <span>&ldquo;{v as string}&rdquo;</span>,
  },
  {
    key: "quotedUser",
    header: "Said by",
    searchable: true,
    sortable: true,
    className: "w-32 text-[var(--color-text-secondary)]",
  },
  {
    key: "gameName",
    header: "Game",
    className: "w-40 text-[var(--color-text-muted)] text-xs",
    render: (v) => (v as string) || "\u2014",
  },
  {
    key: "createdAt",
    header: "Date",
    sortable: true,
    className: "w-28 text-[var(--color-text-muted)] text-xs",
    render: (v) => new Date(v as string).toLocaleDateString(),
  },
];

export function QuotesPage() {
  const queryClient = useQueryClient();
  const { isLocked, lockReason } = useModuleLock("/quotes");
  const [showCreate, setShowCreate] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<Quote | null>(null);

  const { data: quotes, isLoading, isError } = useQuery<Quote[]>({
    queryKey: ["quotes"],
    queryFn: quotesApi.getAll,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => quotesApi.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["quotes"] });
      showToast("success", "Quote deleted");
      setDeleteTarget(null);
    },
    onError: () => {
      showToast("error", "Failed to delete quote");
      setDeleteTarget(null);
    },
  });

  const allColumns: SmartColumn<Quote>[] = [
    ...columns,
    {
      key: "_actions",
      header: "",
      className: "w-20 text-right",
      render: (_v, row) => (
        <button
          onClick={(e) => {
            e.stopPropagation();
            setDeleteTarget(row);
          }}
          disabled={isLocked}
          className="rounded p-1 text-[var(--color-text-muted)] hover:text-red-400 hover:bg-red-500/10 transition-colors disabled:opacity-40"
          title="Delete quote"
        >
          <Trash2 className="h-4 w-4" />
        </button>
      ),
    },
  ];

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
      {lockReason && <LockBanner message={lockReason} />}
      <PageHeader
        title="Quotes"
        description="Save memorable chat moments."
        helpKey="quotes"
        badge={
          quotes && quotes.length > 0 ? (
            <span className="rounded-full bg-[var(--color-elevated)] px-2.5 py-0.5 text-xs font-medium text-[var(--color-text-secondary)] border border-[var(--color-border)]">
              {quotes.length}
            </span>
          ) : undefined
        }
        actions={
          !showCreate ? (
            <button
              onClick={() => setShowCreate(true)}
              disabled={isLocked}
              className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-3 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] transition-colors disabled:opacity-40"
            >
              <Plus className="h-4 w-4" /> Add Quote
            </button>
          ) : undefined
        }
      />

      {showCreate && (
        <QuoteForm
          onClose={() => setShowCreate(false)}
          onCreated={() => {
            setShowCreate(false);
            queryClient.invalidateQueries({ queryKey: ["quotes"] });
          }}
        />
      )}

      <SmartDataTable
        data={quotes ?? []}
        columns={allColumns}
        pageSize={50}
        searchPlaceholder="Search quotes..."
        emptyMessage="No quotes saved yet."
        isLoading={isLoading}
        getRowKey={(row) => row.id}
      />

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete Quote"
        message={deleteTarget ? `Delete quote #${deleteTarget.number}?` : ""}
        confirmLabel="Delete"
        variant="danger"
        onConfirm={() => {
          if (deleteTarget) {
            deleteMutation.mutate(deleteTarget.id);
          }
        }}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  );
}
