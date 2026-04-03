import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, MessageSquareQuote } from "lucide-react";
import { quotesApi } from "../api/quotes";
import { PageHeader } from "../components/ui/PageHeader";
import { SearchInput } from "../components/ui/SearchInput";
import { QuoteTable } from "../components/features/quotes/QuoteTable";
import { QuoteForm } from "../components/features/quotes/QuoteForm";
import type { Quote } from "../types/quotes";

export function QuotesPage() {
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [search, setSearch] = useState("");

  const { data: quotes, isLoading } = useQuery<Quote[]>({
    queryKey: ["quotes"],
    queryFn: quotesApi.getAll,
  });

  return (
    <div className="space-y-6 p-6">
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
              className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-3 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] transition-colors"
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

      {isLoading ? (
        <p className="text-sm text-[var(--color-text-muted)]">Loading quotes...</p>
      ) : !quotes || quotes.length === 0 ? (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-8 text-center">
          <MessageSquareQuote className="mx-auto h-10 w-10 text-[var(--color-text-muted)] mb-3" />
          <p className="text-sm text-[var(--color-text-secondary)]">No quotes saved yet.</p>
          <p className="mt-1 text-xs text-[var(--color-text-muted)]">
            Use <code className="text-[var(--color-brand-text)]">!quote add &lt;text&gt;</code> in
            chat or click "Add Quote" above.
          </p>
        </div>
      ) : (
        <>
          {quotes.length > 3 && (
            <SearchInput
              value={search}
              onChange={setSearch}
              placeholder="Search quotes..."
            />
          )}

          <QuoteTable quotes={quotes} search={search} />
        </>
      )}
    </div>
  );
}
