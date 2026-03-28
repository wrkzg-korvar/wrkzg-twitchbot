import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Plus, Trash2, Search, MessageSquareQuote } from "lucide-react";

// ─── Types ───────────────────────────────────────────────

interface Quote {
  id: number;
  number: number;
  text: string;
  quotedUser: string;
  savedBy: string;
  gameName: string | null;
  createdAt: string;
}

// ─── API ─────────────────────────────────────────────────

async function fetchQuotes(): Promise<Quote[]> {
  const res = await fetch("/api/quotes");
  if (!res.ok) throw new Error("Failed to fetch quotes");
  return res.json();
}

async function createQuote(body: { text: string; quotedUser: string; gameName?: string }): Promise<void> {
  const res = await fetch("/api/quotes", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.error || "Failed to create quote");
  }
}

async function deleteQuote(id: number): Promise<void> {
  const res = await fetch(`/api/quotes/${id}`, { method: "DELETE" });
  if (!res.ok) throw new Error("Failed to delete quote");
}

// ─── Component ───────────────────────────────────────────

export function Quotes() {
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [search, setSearch] = useState("");

  const { data: quotes, isLoading } = useQuery<Quote[]>({
    queryKey: ["quotes"],
    queryFn: fetchQuotes,
  });

  const deleteMut = useMutation({
    mutationFn: deleteQuote,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["quotes"] }),
  });

  const filteredQuotes = (quotes ?? []).filter(
    (q) =>
      search === "" ||
      q.text.toLowerCase().includes(search.toLowerCase()) ||
      q.quotedUser.toLowerCase().includes(search.toLowerCase()) ||
      (q.gameName ?? "").toLowerCase().includes(search.toLowerCase()) ||
      q.number.toString() === search.trim()
  );

  return (
    <div className="flex h-full flex-col gap-6 overflow-y-auto p-6">
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div>
            <h1 className="text-2xl font-bold text-[var(--color-text)]">Quotes</h1>
            <p className="mt-1 text-sm text-[var(--color-text-muted)]">
              Save memorable chat moments.
            </p>
          </div>
          {quotes && quotes.length > 0 && (
            <span className="rounded-full bg-[var(--color-elevated)] px-2.5 py-0.5 text-xs font-medium text-[var(--color-text-secondary)] border border-[var(--color-border)]">
              {quotes.length}
            </span>
          )}
        </div>
        {!showCreate && (
          <button
            onClick={() => setShowCreate(true)}
            className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-3 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] transition-colors"
          >
            <Plus className="h-4 w-4" /> Add Quote
          </button>
        )}
      </div>

      {showCreate && (
        <CreateQuoteForm
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
            <div className="relative">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--color-text-muted)]" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search quotes..."
                className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] pl-9 pr-3 py-2.5 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none focus:ring-1 focus:ring-[var(--color-brand)]"
              />
            </div>
          )}

          <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-sm min-w-[640px]">
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
                        {quote.gameName ?? "—"}
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
              </table>
            </div>
          </div>

          {search && filteredQuotes.length === 0 && (
            <p className="text-sm text-[var(--color-text-muted)] text-center">
              No quotes match &ldquo;{search}&rdquo;.
            </p>
          )}
        </>
      )}
    </div>
  );
}

// ─── Create Quote Form ───────────────────────────────────

function CreateQuoteForm({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [text, setText] = useState("");
  const [quotedUser, setQuotedUser] = useState("");
  const [gameName, setGameName] = useState("");

  const createMut = useMutation({
    mutationFn: () =>
      createQuote({
        text: text.trim(),
        quotedUser: quotedUser.trim(),
        gameName: gameName.trim() || undefined,
      }),
    onSuccess: onCreated,
  });

  const canCreate = text.trim().length > 0 && quotedUser.trim().length > 0;

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
      <h2 className="text-sm font-semibold text-[var(--color-text)] mb-4">New Quote</h2>

      <div className="space-y-3">
        <div>
          <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Quote Text</label>
          <textarea
            placeholder='e.g. "I can&apos;t believe that just happened!"'
            value={text}
            onChange={(e) => setText(e.target.value)}
            maxLength={500}
            rows={2}
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none resize-none"
          />
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label className="text-xs text-[var(--color-text-muted)] mb-1 block">Who said it</label>
            <input
              type="text"
              placeholder="e.g. StreamerName"
              value={quotedUser}
              onChange={(e) => setQuotedUser(e.target.value)}
              maxLength={100}
              className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
            />
          </div>
          <div>
            <label className="text-xs text-[var(--color-text-muted)] mb-1 block">
              Game <span className="text-[var(--color-text-muted)]">(optional)</span>
            </label>
            <input
              type="text"
              placeholder="e.g. Elden Ring"
              value={gameName}
              onChange={(e) => setGameName(e.target.value)}
              maxLength={200}
              className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
            />
          </div>
        </div>

        <div className="flex gap-2">
          <button
            onClick={() => createMut.mutate()}
            disabled={!canCreate || createMut.isPending}
            className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
          >
            {createMut.isPending ? "Saving..." : "Add Quote"}
          </button>
          <button
            onClick={onClose}
            className="rounded-lg bg-[var(--color-elevated)] px-4 py-2 text-sm font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] transition-colors"
          >
            Cancel
          </button>
        </div>

        {createMut.isError && (
          <p className="text-xs text-red-400">{(createMut.error as Error).message}</p>
        )}
      </div>
    </div>
  );
}
