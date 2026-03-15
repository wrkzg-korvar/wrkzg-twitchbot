import { useState } from "react";
import { useAuthStatus } from "../hooks/useAuthStatus";
import { TwitchAccountCard } from "../components/TwitchAccountCard";
import { saveCredentials } from "../api/auth";

export function SettingsAuthSection() {
  const { bot, broadcaster, isLoading } = useAuthStatus();

  if (isLoading) {
    return <div className="p-6">Loading settings…</div>;
  }

  return (
    <div className="space-y-8">
      {/* ─── Twitch App Credentials ─────────────────────────────── */}
      <TwitchCredentialsSection />

      {/* ─── Twitch Accounts ────────────────────────────────────── */}
      <section className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold">Twitch Accounts</h2>
          <p className="mt-1 text-sm text-[var(--color-text-muted)]">
            Connect your bot account for chat and your broadcaster account for
            API access. These are two separate Twitch accounts.
          </p>
        </div>

        <TwitchAccountCard
          title="Bot Account"
          description="The Twitch account that sends and reads chat messages."
          tokenType="bot"
          state={bot}
          scopes={["chat:read", "chat:edit"]}
        />

        <TwitchAccountCard
          title="Broadcaster Account"
          description="Your main Twitch channel account. Used for followers, polls, EventSub, and subscriber data."
          tokenType="broadcaster"
          state={broadcaster}
          scopes={[
            "moderator:read:followers",
            "channel:read:polls",
            "channel:manage:polls",
            "bits:read",
            "channel:read:subscriptions",
          ]}
        />
      </section>
    </div>
  );
}

function TwitchCredentialsSection() {
  const [secret, setSecret] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSave = async () => {
    if (!secret.trim()) return;
    setIsSaving(true);
    setError(null);

    try {
      await saveCredentials("", secret.trim());
      setSaved(true);
      setSecret("");
    } catch (err) {
      setError("Failed to save credentials. Please try again.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <section className="rounded-lg border border-[var(--color-border)] p-6">
      <div>
        <h2 className="text-lg font-semibold">Twitch App Credentials</h2>
        <p className="mt-1 text-sm text-[var(--color-text-muted)]">
          Register an app at{" "}
          <a
            href="https://dev.twitch.tv/console"
            target="_blank"
            rel="noopener noreferrer"
            className="text-[var(--color-twitch)] underline hover:text-[var(--color-twitch-hover)]"
          >
            dev.twitch.tv/console
          </a>{" "}
          — set redirect URI to{" "}
          <code className="rounded bg-[var(--color-elevated)] px-1 text-sm">
            http://localhost:5000/auth/callback
          </code>
        </p>
      </div>

      {saved ? (
        <div className="mt-4 flex items-center gap-2 rounded-md bg-green-50 p-3 text-sm text-green-700 dark:bg-green-900/20 dark:text-green-300">
          <span>✓</span>
          <span>Client Secret saved to encrypted storage.</span>
        </div>
      ) : (
        <div className="mt-4 space-y-3">
          <div>
            <label htmlFor="clientSecret" className="block text-sm font-medium text-[var(--color-text)]">
              Client Secret
            </label>
            <input
              id="clientSecret"
              type="password"
              value={secret}
              onChange={(e) => setSecret(e.target.value)}
              placeholder="Enter your Twitch Client Secret"
              className="mt-1 block w-full rounded-md border border-[var(--color-border)] bg-[var(--color-elevated)] px-3 py-2 text-sm shadow-sm focus:border-[var(--color-brand)] focus:outline-none focus:ring-1 focus:ring-[var(--color-brand)]"
            />
            <p className="mt-1 text-xs text-[var(--color-text-secondary)]">
              Encrypted and stored locally. Never leaves your machine.
            </p>
          </div>

          {error && <p className="text-sm text-red-500">{error}</p>}

          <button
            onClick={handleSave}
            disabled={isSaving || !secret.trim()}
            className="rounded-md bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--color-brand-hover)] disabled:opacity-50"
          >
            {isSaving ? "Saving…" : "Save Client Secret"}
          </button>
        </div>
      )}
    </section>
  );
}
