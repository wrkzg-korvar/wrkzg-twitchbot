import { useState } from "react";
import { Trash2 } from "lucide-react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { quotesApi } from "../../../api/quotes";
import { ConfirmDialog } from "../../../components/ui/ConfirmDialog";
import { SmartDataTable } from "../../../components/ui/DataTable";
import { showToast } from "../../../hooks/useToast";
import type { SmartColumn } from "../../../components/ui/DataTable";
import type { Quote } from "../../../types/quotes";

interface QuoteTableProps {
  quotes: Quote[];
}

export function QuoteTable({ quotes }: QuoteTableProps) {
  const queryClient = useQueryClient();
  const [deleteTarget, setDeleteTarget] = useState<Quote | null>(null);

  const deleteMut = useMutation({
    mutationFn: quotesApi.remove,
    onSuccess: () => {
      showToast("success", "Quote deleted");
      queryClient.invalidateQueries({ queryKey: ["quotes"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const columns: SmartColumn<Quote>[] = [
    {
      key: "number",
      header: "#",
      sortable: true,
      className: "w-16 font-mono text-[var(--color-text-muted)]",
    },
    {
      key: "text",
      header: "Quote",
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
      searchable: true,
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
    {
      key: "actions",
      header: "",
      className: "w-20 text-right",
      render: (_, row) => (
        <button
          onClick={(e) => {
            e.stopPropagation();
            setDeleteTarget(row);
          }}
          disabled={deleteMut.isPending}
          className="rounded p-1 text-[var(--color-text-muted)] hover:text-red-400 hover:bg-[var(--color-elevated)] transition-colors"
          title="Delete quote"
        >
          <Trash2 className="h-3.5 w-3.5" />
        </button>
      ),
    },
  ];

  return (
    <>
      <SmartDataTable<Quote>
        data={quotes}
        columns={columns}
        pageSize={50}
        searchPlaceholder="Search quotes..."
        emptyMessage="No quotes saved yet."
        getRowKey={(row) => row.id}
      />

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete Quote"
        message={`Delete quote #${deleteTarget?.number}? This action cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={() => {
          if (deleteTarget) {
            deleteMut.mutate(deleteTarget.id);
          }
          setDeleteTarget(null);
        }}
        onCancel={() => setDeleteTarget(null)}
      />
    </>
  );
}
