import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { quotesApi } from "../../../api/quotes";
import { showToast } from "../../../hooks/useToast";
import { inputClass } from "../../../lib/constants";

interface QuoteFormProps {
  onClose: () => void;
  onCreated: () => void;
}

export function QuoteForm({ onClose, onCreated }: QuoteFormProps) {
  const [text, setText] = useState("");
  const [quotedUser, setQuotedUser] = useState("");
  const [gameName, setGameName] = useState("");

  const createMut = useMutation({
    mutationFn: () =>
      quotesApi.create({
        text: text.trim(),
        quotedUser: quotedUser.trim(),
        gameName: gameName.trim() || undefined,
      }),
    onSuccess: () => {
      showToast("success", "Quote added");
      onCreated();
    },
    onError: (err: Error) => showToast("error", err.message),
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
              className={inputClass}
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
              className={inputClass}
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
