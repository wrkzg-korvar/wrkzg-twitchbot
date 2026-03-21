import { useState } from "react";

interface ChannelStepProps {
  onBack: () => void;
  onFinish: () => void;
}

export function ChannelStep({ onBack, onFinish }: ChannelStepProps) {
  const [channel, setChannel] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleFinish = async () => {
    const trimmed = channel.trim().toLowerCase().replace(/^#/, "");
    if (!trimmed) return;

    setIsSaving(true);
    setError(null);

    try {
      // Save channel name via Settings API
      const res = await fetch("/api/settings", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ "Bot.Channel": trimmed }),
      });

      if (!res.ok) {
        throw new Error(`Failed to save channel (${res.status})`);
      }

      // Trigger the bot to connect now that setup is complete
      await fetch("/api/bot/connect", { method: "POST" });

      onFinish();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save channel name.");
    } finally {
      setIsSaving(false);
    }
  };

  const isValid = channel.trim().length >= 2;

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-[var(--color-text)]">Set Your Channel</h2>
        <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
          Almost done! Enter your Twitch channel name so the bot knows which
          chat to join.
        </p>
      </div>

      <div className="space-y-4">
        <div>
          <label htmlFor="channel" className="block text-sm font-medium text-[var(--color-text)]">
            Channel Name
          </label>
          <div className="mt-1 flex items-center gap-2">
            <span className="text-[var(--color-text-muted)] text-sm">#</span>
            <input
              id="channel"
              type="text"
              value={channel}
              onChange={(e) => setChannel(e.target.value.replace(/\s/g, ""))}
              placeholder="your_channel_name"
              autoComplete="off"
              className="block w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2.5 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none focus:ring-1 focus:ring-[var(--color-brand)]"
            />
          </div>
          <p className="mt-1 text-xs text-[var(--color-text-muted)]">
            This is your Twitch username, all lowercase. No spaces, no # prefix.
          </p>
        </div>
      </div>

      {error && (
        <div className="rounded-lg border border-red-800/50 bg-red-950/30 p-3 text-sm text-red-400">
          {error}
        </div>
      )}

      {/* ─── Success Preview ────────────────────────────────── */}
      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-5">
        <h3 className="text-sm font-semibold text-[var(--color-text)] mb-3">Setup Summary</h3>
        <div className="space-y-2 text-sm text-[var(--color-text-secondary)]">
          <div className="flex items-center gap-2">
            <span className="text-green-400">✓</span>
            <span>Twitch App credentials saved</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-green-400">✓</span>
            <span>Bot account connected</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-green-400">✓</span>
            <span>Broadcaster account connected</span>
          </div>
          <div className="flex items-center gap-2">
            <span className={isValid ? "text-green-400" : "text-[var(--color-text-muted)]"}>
              {isValid ? "✓" : "○"}
            </span>
            <span className={isValid ? "text-[var(--color-text)]" : ""}>
              Channel: {isValid ? `#${channel.trim().toLowerCase()}` : "not set"}
            </span>
          </div>
        </div>
      </div>

      <div className="flex items-center justify-between pt-2">
        <button
          onClick={onBack}
          className="rounded-lg px-4 py-2.5 text-sm font-medium text-[var(--color-text-secondary)] hover:text-[var(--color-text)] transition-colors"
        >
          ← Back
        </button>
        <button
          onClick={handleFinish}
          disabled={!isValid || isSaving}
          className="rounded-lg bg-green-600 px-6 py-2.5 text-sm font-semibold text-white hover:bg-green-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          {isSaving ? "Saving…" : "Finish Setup ✓"}
        </button>
      </div>
    </div>
  );
}
