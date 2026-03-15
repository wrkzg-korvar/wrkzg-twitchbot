import { useState } from "react";

interface TwitchAppStepProps {
  onNext: () => void;
  onBack: () => void;
}

export function TwitchAppStep({ onNext, onBack }: TwitchAppStepProps) {
  const [clientId, setClientId] = useState("");
  const [clientSecret, setClientSecret] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const redirectUri = `http://localhost:${window.location.port || "5000"}/auth/callback`;

  const handleSave = async () => {
    if (!clientId.trim() || !clientSecret.trim()) return;

    setIsSaving(true);
    setError(null);

    try {
      const res = await fetch("/auth/credentials", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          clientId: clientId.trim(),
          clientSecret: clientSecret.trim(),
        }),
      });

      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        throw new Error(data.error || `Failed to save (${res.status})`);
      }

      onNext();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save credentials.");
    } finally {
      setIsSaving(false);
    }
  };

  const canContinue = clientId.trim().length > 10 && clientSecret.trim().length > 10;

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-[var(--color-text)]">Create a Twitch App</h2>
        <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
          Wrkzg needs a Twitch Developer Application to authenticate with
          Twitch. This is free and takes about 2 minutes.
        </p>
      </div>

      {/* ─── Step-by-step instructions ──────────────────────── */}
      <div className="space-y-4 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-5">
        <h3 className="text-sm font-semibold text-[var(--color-text)]">Instructions</h3>

        <ol className="space-y-4 text-sm text-[var(--color-text-secondary)]">
          <li className="flex items-start gap-3">
            <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-[var(--color-elevated)] text-xs font-bold text-[var(--color-text-secondary)]">
              1
            </span>
            <span>
              Go to the{" "}
              <a
                href="https://dev.twitch.tv/console/apps/create"
                target="_blank"
                rel="noopener noreferrer"
                className="text-[var(--color-twitch)] underline hover:text-[var(--color-twitch-hover)]"
              >
                Twitch Developer Console → Create App
              </a>{" "}
              (opens in a new tab). Log in with your main Twitch account.
            </span>
          </li>
          <li className="flex items-start gap-3">
            <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-[var(--color-elevated)] text-xs font-bold text-[var(--color-text-secondary)]">
              2
            </span>
            <div className="space-y-2">
              <span>Fill out the form with these values:</span>
              <div className="space-y-1.5">
                <CopyField label="Name" value="Wrkzg" />
                <CopyField label="OAuth Redirect URL" value={redirectUri} />
                <div className="text-xs text-[var(--color-text-muted)]">
                  Category: <strong className="text-[var(--color-text-secondary)]">Chat Bot</strong> · Client Type:{" "}
                  <strong className="text-[var(--color-text-secondary)]">Confidential</strong>
                </div>
              </div>
            </div>
          </li>
          <li className="flex items-start gap-3">
            <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-[var(--color-elevated)] text-xs font-bold text-[var(--color-text-secondary)]">
              3
            </span>
            <span>
              Click <strong className="text-[var(--color-text)]">"Create"</strong>, then click{" "}
              <strong className="text-[var(--color-text)]">"New Secret"</strong> to generate your
              Client Secret.
            </span>
          </li>
          <li className="flex items-start gap-3">
            <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-[var(--color-elevated)] text-xs font-bold text-[var(--color-text-secondary)]">
              4
            </span>
            <span>Copy the <strong className="text-[var(--color-text)]">Client ID</strong> and{" "}
              <strong className="text-[var(--color-text)]">Client Secret</strong> into the fields below.
            </span>
          </li>
        </ol>
      </div>

      {/* ─── Input Fields ───────────────────────────────────── */}
      <div className="space-y-4">
        <div>
          <label htmlFor="clientId" className="block text-sm font-medium text-[var(--color-text)]">
            Client ID
          </label>
          <input
            id="clientId"
            type="text"
            value={clientId}
            onChange={(e) => setClientId(e.target.value)}
            placeholder="Paste your Client ID here"
            autoComplete="off"
            className="mt-1 block w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2.5 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none focus:ring-1 focus:ring-[var(--color-brand)]"
          />
        </div>

        <div>
          <label htmlFor="clientSecret" className="block text-sm font-medium text-[var(--color-text)]">
            Client Secret
          </label>
          <input
            id="clientSecret"
            type="password"
            value={clientSecret}
            onChange={(e) => setClientSecret(e.target.value)}
            placeholder="Paste your Client Secret here"
            autoComplete="off"
            className="mt-1 block w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2.5 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none focus:ring-1 focus:ring-[var(--color-brand)]"
          />
          <p className="mt-1 text-xs text-[var(--color-text-muted)]">
            🔒 Encrypted and stored in your OS keychain. Never leaves your machine.
          </p>
        </div>
      </div>

      {error && (
        <div className="rounded-lg border border-red-800/50 bg-red-950/30 p-3 text-sm text-red-400">
          {error}
        </div>
      )}

      {/* ─── Navigation ─────────────────────────────────────── */}
      <div className="flex items-center justify-between pt-2">
        <button
          onClick={onBack}
          className="rounded-lg px-4 py-2.5 text-sm font-medium text-[var(--color-text-secondary)] hover:text-[var(--color-text)] transition-colors"
        >
          ← Back
        </button>
        <button
          onClick={handleSave}
          disabled={!canContinue || isSaving}
          className="rounded-lg bg-[var(--color-brand)] px-6 py-2.5 text-sm font-semibold text-white hover:bg-[var(--color-brand-hover)] disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          {isSaving ? "Saving…" : "Save & Continue →"}
        </button>
      </div>
    </div>
  );
}

function CopyField({ label, value }: { label: string; value: string }) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    await navigator.clipboard.writeText(value);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="flex items-center gap-2 rounded-md bg-[var(--color-elevated)]/70 px-3 py-1.5">
      <span className="text-xs text-[var(--color-text-muted)] shrink-0">{label}:</span>
      <code className="flex-1 text-xs text-[var(--color-text)] truncate">{value}</code>
      <button
        onClick={handleCopy}
        className="shrink-0 rounded px-2 py-0.5 text-xs text-[var(--color-brand-text)] hover:bg-[var(--color-elevated)] transition-colors"
      >
        {copied ? "Copied!" : "Copy"}
      </button>
    </div>
  );
}
