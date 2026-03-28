import { X, ExternalLink } from "lucide-react";
import { useUpdateCheck } from "../../hooks/useUpdateCheck";

export function UpdateBanner() {
  const { update, dismiss } = useUpdateCheck();

  if (!update) {
    return null;
  }

  return (
    <div className="flex items-center justify-between border-b border-[var(--color-brand-subtle)] bg-[var(--color-brand-subtle)] px-4 py-1.5 text-xs">
      <div className="flex items-center gap-2 text-[var(--color-brand-text)]">
        <span>
          Wrkzg <strong>{update.latestVersion}</strong> is available
        </span>
        <a
          href={update.releaseUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center gap-1 underline hover:no-underline"
        >
          View release <ExternalLink className="h-3 w-3" />
        </a>
      </div>
      <button
        onClick={dismiss}
        className="rounded p-0.5 text-[var(--color-brand-text)] transition-colors hover:bg-[var(--color-brand-subtle)]"
      >
        <X className="h-3.5 w-3.5" />
      </button>
    </div>
  );
}
