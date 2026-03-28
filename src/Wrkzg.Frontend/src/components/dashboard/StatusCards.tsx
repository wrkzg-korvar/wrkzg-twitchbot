import type { StatusResponse } from "../../types/status";

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
      <StatusCard
        label="Viewers"
        value={viewerCount !== null ? viewerCount.toString() : "--"}
        detail={startedAt ? `Uptime: ${formatUptime(startedAt)}` : undefined}
        color={isLive ? "purple" : "gray"}
      />
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
