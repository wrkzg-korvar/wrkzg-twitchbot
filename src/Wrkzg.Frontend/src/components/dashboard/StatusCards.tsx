import { useState } from "react";
import { Eye, EyeOff } from "lucide-react";
import type { StatusResponse } from "../../types/status";

const VIEWER_HIDDEN_KEY = "wrkzg-viewer-count-hidden";

interface StatusCardsProps {
  status: StatusResponse | undefined;
}

export function StatusCards({ status }: StatusCardsProps) {
  const botConnected = status?.bot.isConnected ?? false;
  const botChannel = status?.bot.channel ?? null;
  const isLive = status?.stream.isLive ?? false;
  const viewerCount = isLive ? status!.stream.viewerCount : null;
  const streamGame = isLive ? status!.stream.game : null;
  const startedAt = isLive ? status!.stream.startedAt : null;

  const [viewerHidden, setViewerHidden] = useState(() =>
    localStorage.getItem(VIEWER_HIDDEN_KEY) === "true"
  );

  const toggleViewerVisibility = () => {
    const next = !viewerHidden;
    setViewerHidden(next);
    localStorage.setItem(VIEWER_HIDDEN_KEY, String(next));
  };

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
      <StatusCard
        label="Bot Status"
        value={botConnected ? "Connected" : "Offline"}
        detail={botChannel ? `#${botChannel}` : undefined}
        color={botConnected ? "green" : "gray"}
      />
      <StatusCard
        label="Stream"
        value={isLive ? "Live" : "Offline"}
        detail={isLive && streamGame ? `Playing ${streamGame}` : isLive ? "Live" : "Not live"}
        color={isLive ? "purple" : "gray"}
      />
      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
        <div className="flex items-center justify-between">
          <p className="text-xs font-medium text-[var(--color-text-muted)] uppercase tracking-wider">
            Viewers
          </p>
          <button
            onClick={toggleViewerVisibility}
            className="rounded p-1 text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)] hover:text-[var(--color-text)] transition-colors"
            title={viewerHidden ? "Show viewer count" : "Hide viewer count"}
          >
            {viewerHidden ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
          </button>
        </div>
        <div className="mt-2 flex items-center gap-2">
          <span className={`h-2.5 w-2.5 rounded-full ${isLive ? "bg-purple-500" : "bg-gray-600"}`} />
          <span className="text-lg font-semibold text-[var(--color-text)]">
            {viewerHidden ? "***" : viewerCount !== null ? viewerCount.toString() : "--"}
          </span>
        </div>
        {startedAt && (
          <p className="mt-1 text-xs text-[var(--color-text-muted)]">
            Uptime: {formatUptime(startedAt)}
          </p>
        )}
      </div>
    </div>
  );
}

function StatusCard({
  label,
  value,
  detail,
  color,
}: {
  label: string;
  value: string;
  detail?: string;
  color: "green" | "gray" | "purple";
}) {
  const dotColor = {
    green: "bg-green-500",
    gray: "bg-gray-600",
    purple: "bg-purple-500",
  }[color];

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
      <p className="text-xs font-medium text-[var(--color-text-muted)] uppercase tracking-wider">
        {label}
      </p>
      <div className="mt-2 flex items-center gap-2">
        <span className={`h-2.5 w-2.5 rounded-full ${dotColor}`} />
        <span className="text-lg font-semibold text-[var(--color-text)]">{value}</span>
      </div>
      {detail && <p className="mt-1 text-xs text-[var(--color-text-muted)]">{detail}</p>}
    </div>
  );
}

function formatUptime(startedAt: string): string {
  const seconds = Math.floor((Date.now() - new Date(startedAt).getTime()) / 1000);
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  if (hours > 0) {
    return `${hours}h ${minutes}m`;
  }
  return `${minutes}m`;
}
