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
        <h2 className="text-2xl font-bold text-white">Create a Twitch App</h2>
        <p className="mt-2 text-sm text-gray-400">
          Wrkzg needs a Twitch Developer Application to authenticate with
          Twitch. This is free and takes about 2 minutes.
        </p>
      </div>

      {/* ─── Step-by-step instructions ──────────────────────── */}
      <div className="space-y-4 rounded-lg border border-gray-800 bg-gray-900/50 p-5">
        <h3 className="text-sm font-semibold text-gray-200">Instructions</h3>

        <ol className="space-y-4 text-sm text-gray-400">
          <li className="flex items-start gap-3">
            <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-gray-800 text-xs font-bold text-gray-400">
              1
            </span>
            <span>
              Go to the{" "}
              <a
                href="https://dev.twitch.tv/console/apps/create"
                target="_blank"
                rel="noopener noreferrer"
                className="text-purple-400 underline hover:text-purple-300"
              >
                Twitch Developer Console → Create App
              </a>{" "}
              (opens in a new tab). Log in with your main Twitch account.
            </span>
          </li>
          <li className="flex items-start gap-3">
            <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-gray-800 text-xs font-bold text-gray-400">
              2
            </span>
            <div className="space-y-2">
              <span>Fill out the form with these values:</span>
              <div className="space-y-1.5">
                <CopyField label="Name" value="Wrkzg" />
                <CopyField label="OAuth Redirect URL" value={redirectUri} />
                <div className="text-xs text-gray-500">
                  Category: <strong className="text-gray-400">Chat Bot</strong> · Client Type:{" "}
                  <strong className="text-gray-400">Confidential</strong>
                </div>
              </div>
            </div>
          </li>
          <li className="flex items-start gap-3">
            <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-gray-800 text-xs font-bold text-gray-400">
              3
            </span>
            <span>
              Click <strong className="text-gray-200">"Create"</strong>, then click{" "}
              <strong className="text-gray-200">"New Secret"</strong> to generate your
              Client Secret.
            </span>
          </li>
          <li className="flex items-start gap-3">
            <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-gray-800 text-xs font-bold text-gray-400">
              4
            </span>
            <span>Copy the <strong className="text-gray-200">Client ID</strong> and{" "}
              <strong className="text-gray-200">Client Secret</strong> into the fields below.
            </span>
          </li>
        </ol>
      </div>

      {/* ─── Input Fields ───────────────────────────────────── */}
      <div className="space-y-4">
        <div>
          <label htmlFor="clientId" className="block text-sm font-medium text-gray-300">
            Client ID
          </label>
          <input
            id="clientId"
            type="text"
            value={clientId}
            onChange={(e) => setClientId(e.target.value)}
            placeholder="Paste your Client ID here"
            autoComplete="off"
            className="mt-1 block w-full rounded-lg border border-gray-700 bg-gray-900 px-3 py-2.5 text-sm text-gray-200 placeholder-gray-600 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500"
          />
        </div>

        <div>
          <label htmlFor="clientSecret" className="block text-sm font-medium text-gray-300">
            Client Secret
          </label>
          <input
            id="clientSecret"
            type="password"
            value={clientSecret}
            onChange={(e) => setClientSecret(e.target.value)}
            placeholder="Paste your Client Secret here"
            autoComplete="off"
            className="mt-1 block w-full rounded-lg border border-gray-700 bg-gray-900 px-3 py-2.5 text-sm text-gray-200 placeholder-gray-600 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500"
          />
          <p className="mt-1 text-xs text-gray-500">
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
          className="rounded-lg px-4 py-2.5 text-sm font-medium text-gray-400 hover:text-gray-200 transition-colors"
        >
          ← Back
        </button>
        <button
          onClick={handleSave}
          disabled={!canContinue || isSaving}
          className="rounded-lg bg-purple-600 px-6 py-2.5 text-sm font-semibold text-white hover:bg-purple-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
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
    <div className="flex items-center gap-2 rounded-md bg-gray-800/70 px-3 py-1.5">
      <span className="text-xs text-gray-500 shrink-0">{label}:</span>
      <code className="flex-1 text-xs text-gray-300 truncate">{value}</code>
      <button
        onClick={handleCopy}
        className="shrink-0 rounded px-2 py-0.5 text-xs text-purple-400 hover:bg-gray-700 transition-colors"
      >
        {copied ? "Copied!" : "Copy"}
      </button>
    </div>
  );
}
