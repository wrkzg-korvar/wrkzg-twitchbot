import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Check, X } from "lucide-react";
import { SettingsAuthSection } from "./SettingsAuthSection";
import { diagnosticsApi } from "../api/diagnostics";
import { api } from "../api/client";
import { showToast } from "../hooks/useToast";

export function SettingsPage() {
  return (
    <div className="mx-auto max-w-3xl p-6">
      <h1 className="mb-8 text-2xl font-bold">Settings</h1>
      <SettingsAuthSection />
      <BotRequirementsSection />
      <DiagnosticsSection />
    </div>
  );
}

interface BotRequirements {
  botConnected: boolean;
  broadcasterConnected: boolean;
  ircConnected: boolean;
  isMod: boolean;
  isFollower: boolean;
}

function BotRequirementsSection() {
  const { data, isLoading } = useQuery<BotRequirements>({
    queryKey: ["bot-requirements"],
    queryFn: () => api.get<BotRequirements>("/auth/bot-requirements"),
    refetchInterval: 30_000,
  });

  if (isLoading || !data) {
    return null;
  }

  const requirements = [
    {
      label: "Bot is Moderator",
      met: data.isMod,
      pending: !data.ircConnected,
      features: "Announcements, Timeouts, Bans, Shoutouts, Spam Filter actions",
      action: "Type /mod YOUR_BOT_NAME in your Twitch chat",
    },
    {
      label: "Bot is Follower",
      met: data.isFollower,
      pending: false,
      features: "Follower-only emotes from subscribed channels",
      action: "Follow your channel from the bot's Twitch account",
    },
  ];

  return (
    <section className="mt-8 rounded-lg border border-[var(--color-border)] p-6">
      <h2 className="text-lg font-semibold">Bot Requirements</h2>
      <p className="mt-1 text-sm text-[var(--color-text-muted)]">
        Some features require the bot account to have specific status in your channel.
      </p>
      {!data.ircConnected && (
        <p className="mt-2 text-xs text-yellow-400">
          The bot is not connected to chat — moderator status cannot be verified yet.
        </p>
      )}
      <div className="mt-4 space-y-3">
        {requirements.map((req) => (
          <div key={req.label} className="flex items-start gap-3">
            <span
              className={`mt-0.5 flex h-5 w-5 items-center justify-center rounded-full ${
                req.met
                  ? "bg-green-500/20 text-green-400"
                  : req.pending
                    ? "bg-[var(--color-elevated)] text-[var(--color-text-muted)]"
                    : "bg-red-500/20 text-red-400"
              }`}
            >
              {req.met ? <Check className="h-3 w-3" /> : <X className="h-3 w-3" />}
            </span>
            <div className="min-w-0 flex-1">
              <p className="text-sm font-medium text-[var(--color-text)]">{req.label}</p>
              <p className="text-xs text-[var(--color-text-muted)]">
                Required for: {req.features}
              </p>
              {!req.met && !req.pending && (
                <p className="mt-1 text-xs text-yellow-400">→ {req.action}</p>
              )}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

function DiagnosticsSection() {
  const [isExporting, setIsExporting] = useState(false);

  const handleExport = async () => {
    setIsExporting(true);
    try {
      const result = await diagnosticsApi.exportLog();
      showToast("success", `Log saved to: ${result.path}`);
    } catch {
      showToast("error", "Failed to export diagnostic log");
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <section className="mt-8 rounded-lg border border-[var(--color-border)] p-6">
      <div>
        <h2 className="text-lg font-semibold">Diagnostics</h2>
        <p className="mt-1 text-sm text-[var(--color-text-muted)]">
          Export the current diagnostic log file to your Downloads folder.
          Share this file with the developer for bug reports. The log contains
          connection events, errors, and timing information. No sensitive data
          (tokens, passwords) is included.
        </p>
      </div>

      <div className="mt-4">
        <button
          onClick={handleExport}
          disabled={isExporting}
          className="rounded-md bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-50"
        >
          {isExporting ? "Exporting…" : "Export Diagnostic Log"}
        </button>
      </div>
    </section>
  );
}
