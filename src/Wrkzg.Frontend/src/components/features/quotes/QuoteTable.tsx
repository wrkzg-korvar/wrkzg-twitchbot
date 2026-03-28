import { Trash2 } from "lucide-react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { quotesApi } from "../../../api/quotes";
import { DataTable } from "../../../components/ui/DataTable";
import { showToast } from "../../../hooks/useToast";
import type { Quote } from "../../../types/quotes";

interface QuoteTableProps {
  quotes: Quote[];
  search: string;
}

export function QuoteTable({ quotes, search }: QuoteTableProps) {
  const queryClient = useQueryClient();

  const deleteMut = useMutation({
    mutationFn: quotesApi.remove,
    onSuccess: () => {
      showToast("success", "Quote deleted");
      queryClient.invalidateQueries({ queryKey: ["quotes"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const filteredQuotes = quotes.filter(
    (q) =>
      search === "" ||
      q.text.toLowerCase().includes(search.toLowerCase()) ||
      q.quotedUser.toLowerCase().includes(search.toLowerCase()) ||
      (q.gameName ?? "").toLowerCase().includes(search.toLowerCase()) ||
      q.number.toString() === search.trim()
  );

  return (
    <>
      <DataTable minWidth={640}>
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-[var(--color-surface)]">
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-secondary)] w-16">
                  #
                </th>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-secondary)]">
                  Quote
                </th>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-secondary)] w-32">
                  Said by
                </th>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-secondary)] w-40">
                  Game
                </th>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-secondary)] w-28">
                  Date
                </th>
                <th className="px-4 py-3 text-right font-medium text-[var(--color-text-secondary)] w-20">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody>
              {filteredQuotes.map((quote) => (
                <tr
                  key={quote.id}
                  className="border-b border-[var(--color-border)] hover:bg-[var(--color-elevated)]"
                >
                  <td className="px-4 py-3 text-[var(--color-text-muted)] font-mono">
                    {quote.number}
                  </td>
                  <td className="px-4 py-3 text-[var(--color-text)]">
                    &ldquo;{quote.text}&rdquo;
                  </td>
                  <td className="px-4 py-3 text-[var(--color-text-secondary)]">
                    {quote.quotedUser}
                  </td>
                  <td className="px-4 py-3 text-[var(--color-text-muted)] text-xs">
                    {quote.gameName ?? "\u2014"}
                  </td>
                  <td className="px-4 py-3 text-[var(--color-text-muted)] text-xs">
                    {new Date(quote.createdAt).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => {
                        if (confirm(`Delete quote #${quote.number}?`)) {
                          deleteMut.mutate(quote.id);
                        }
                      }}
                      disabled={deleteMut.isPending}
                      className="rounded p-1 text-[var(--color-text-muted)] hover:text-red-400 hover:bg-[var(--color-elevated)] transition-colors"
                      title="Delete quote"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
      </DataTable>

      {search && filteredQuotes.length === 0 && (
        <p className="text-sm text-[var(--color-text-muted)] text-center">
          No quotes match &ldquo;{search}&rdquo;.
        </p>
      )}
    </>
  );
}
