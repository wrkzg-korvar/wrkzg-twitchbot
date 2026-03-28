import { useState } from "react";
import { useAuthStatus } from "../../hooks/useAuthStatus";
import { startOAuthFlow, disconnect } from "../../api/auth";

interface BotConnectStepProps {
  onNext: () => void;
  onBack: () => void;
}

export function BotConnectStep({ onNext, onBack }: BotConnectStepProps) {
  const { bot, refetch } = useAuthStatus();
  const [isDisconnecting, setIsDisconnecting] = useState(false);

  const handleDisconnect = async () => {
    setIsDisconnecting(true);
    try {
      await disconnect("bot");
      await refetch();
    } finally {
      setIsDisconnecting(false);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-[var(--color-text)]">Connect Bot Account</h2>
        <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
          This is the Twitch account your bot will use to send and read chat
          messages. It can be your main account, but we recommend creating a
          separate account with a custom bot name (e.g. "MeinStreamBot").
        </p>
      </div>

      <div className="space-y-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-5">
        <h3 className="text-sm font-semibold text-[var(--color-text)]">How it works</h3>
        <ul className="space-y-2 text-sm text-[var(--color-text-secondary)]">
          <li className="flex items-start gap-2">
            <span className="mt-1 text-[var(--color-brand-text)]">•</span>
            <span>
              Clicking "Connect" will open a Twitch login popup. Log in with
              the account you want the bot to use.
            </span>
          </li>
          <li className="flex items-start gap-2">
            <span className="mt-1 text-[var(--color-brand-text)]">•</span>
            <span>
              Twitch will ask you to authorize <strong className="text-[var(--color-text)]">chat:read</strong>{" "}
              and <strong className="text-[var(--color-text)]">chat:edit</strong> permissions.
            </span>
          </li>
          <li className="flex items-start gap-2">
            <span className="mt-1 text-[var(--color-brand-text)]">•</span>
            <span>
              After authorizing, the popup closes automatically and you can continue.
            </span>
          </li>
        </ul>
      </div>

      {/* ─── Connection Status ──────────────────────────────── */}
      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-5">
        {bot.isAuthenticated ? (
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <span className="flex h-10 w-10 items-center justify-center rounded-full bg-green-500/20 text-lg">
                ✓
              </span>
              <div>
                <p className="text-sm font-semibold text-green-400">Bot account connected</p>
                <p className="text-xs text-[var(--color-text-secondary)]">
                  Logged in as{" "}
                  <span className="text-[var(--color-twitch)] font-medium">{bot.twitchUsername}</span>
                </p>
              </div>
            </div>
            <div className="flex gap-2">
              <button
                onClick={() => startOAuthFlow("bot")}
                className="rounded-lg border border-[var(--color-border)] px-3 py-1.5 text-xs font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)] transition-colors"
              >
                Reconnect
              </button>
              <button
                onClick={handleDisconnect}
                disabled={isDisconnecting}
                className="rounded-lg border border-red-500/30 px-3 py-1.5 text-xs font-medium text-red-400 hover:bg-red-500/10 transition-colors disabled:opacity-40"
              >
                {isDisconnecting ? "..." : "Disconnect"}
              </button>
            </div>
          </div>
        ) : (
          <div className="text-center py-2">
            <p className="mb-4 text-sm text-[var(--color-text-secondary)]">No bot account connected yet.</p>
            <button
              onClick={() => startOAuthFlow("bot")}
              className="rounded-lg bg-[var(--color-twitch)] px-6 py-2.5 text-sm font-semibold text-white hover:bg-[var(--color-twitch-hover)] transition-colors"
            >
              Connect Bot Account with Twitch
            </button>
          </div>
        )}
      </div>

      {/* ─── Navigation ─────────────────────────────────────── */}
      <div className="flex items-center justify-between pt-2">
        <button
          onClick={onBack}
          className="rounded-lg px-4 py-2.5 text-sm font-medium text-[var(--color-text-secondary)] hover:text-[var(--color-text)] transition-colors"
        >
          ← Back
        </button>
        <button
          onClick={onNext}
          disabled={!bot.isAuthenticated}
          className="rounded-lg bg-[var(--color-brand)] px-6 py-2.5 text-sm font-semibold text-white hover:bg-[var(--color-brand-hover)] disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          Continue →
        </button>
      </div>
    </div>
  );
}
