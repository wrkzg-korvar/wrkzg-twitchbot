import { useEffect, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useSignalR } from "../hooks/useSignalR";

interface ChatMsg {
  username: string;
  displayName: string;
  content: string;
  isMod: boolean;
  isSubscriber: boolean;
  isBroadcaster: boolean;
  timestamp: string;
}

interface StatusResponse {
  bot: { isConnected: boolean; channel: string | null };
  stream: {
    isLive: boolean;
    viewerCount: number;
    title: string | null;
    game: string | null;
    startedAt: string | null;
  };
  auth: { botTokenPresent: boolean; broadcasterTokenPresent: boolean };
}

async function fetchStatus(): Promise<StatusResponse> {
  const res = await fetch("/api/status");
  if (!res.ok) throw new Error(`Status fetch failed: ${res.status}`);
  return res.json();
}

export function Dashboard() {
  const queryClient = useQueryClient();
  const { isConnected: signalRConnected, on, off } = useSignalR("/hubs/chat");

  // Fetch status via REST — this is the source of truth for bot/stream status
  const { data: status } = useQuery<StatusResponse>({
    queryKey: ["status"],
    queryFn: fetchStatus,
    refetchInterval: 15_000,
  });

  // Chat messages are SignalR-only (no REST endpoint for chat history)
  const [messages, setMessages] = useState<ChatMsg[]>([]);

  // Derive bot status and viewer count directly from query data
  const botConnected = status?.bot.isConnected ?? false;
  const botChannel = status?.bot.channel ?? null;
  const viewerCount = status?.stream.isLive ? status.stream.viewerCount : null;
  const streamGame = status?.stream.isLive ? status.stream.game : null;

  // SignalR: live chat messages + refetch status on BotStatus changes
  useEffect(() => {
    if (!signalRConnected) return;

    on<ChatMsg>("ChatMessage", (msg) => {
      setMessages((prev) => [...prev.slice(-99), msg]);
    });

    // When bot status changes, refetch the REST status
    on("BotStatus", () => {
      queryClient.invalidateQueries({ queryKey: ["status"] });
    });

    on<number>("ViewerCount", () => {
      queryClient.invalidateQueries({ queryKey: ["status"] });
    });

    return () => {
      off("ChatMessage");
      off("BotStatus");
      off("ViewerCount");
    };
  }, [signalRConnected, on, off, queryClient]);

  return (
    <div className="p-6 space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">Dashboard</h1>
        <p className="mt-1 text-sm text-[var(--color-text-muted)]">
          Overview of your bot and stream status.
        </p>
      </div>

      {/* ─── Status Cards ───────────────────────────────────── */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <StatusCard
          label="Bot Status"
          value={botConnected ? "Connected" : "Offline"}
          detail={botChannel ? `#${botChannel}` : undefined}
          color={botConnected ? "green" : "gray"}
        />
        <StatusCard
          label="SignalR"
          value={signalRConnected ? "Connected" : "Disconnected"}
          color={signalRConnected ? "green" : "gray"}
        />
        <StatusCard
          label="Viewers"
          value={viewerCount !== null ? viewerCount.toString() : "—"}
          detail={streamGame ? `Playing ${streamGame}` : "Stream offline"}
          color={status?.stream.isLive ? "purple" : "gray"}
        />
      </div>

      {/* ─── Live Chat Feed ─────────────────────────────────── */}
      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)]">
        <div className="flex items-center justify-between border-b border-[var(--color-border)] px-4 py-3">
          <h2 className="text-sm font-semibold text-[var(--color-text)]">Live Chat</h2>
          <span className="text-xs text-[var(--color-text-muted)]">{messages.length} messages</span>
        </div>

        <div className="h-80 overflow-y-auto p-4 space-y-1.5">
          {messages.length === 0 ? (
            <p className="text-center text-sm text-[var(--color-text-muted)] py-8">
              {botConnected
                ? "Waiting for chat messages…"
                : "Bot is not connected. Check Settings."}
            </p>
          ) : (
            messages.map((msg, i) => (
              <div key={i} className="text-sm">
                <span className="font-semibold" style={{ color: getUserColor(msg) }}>
                  {msg.displayName}
                </span>
                <span className="text-[var(--color-text-muted)]">: </span>
                <span className="text-[var(--color-text)]">{msg.content}</span>
              </div>
            ))
          )}
        </div>
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
      <p className="text-xs font-medium text-[var(--color-text-muted)] uppercase tracking-wider">{label}</p>
      <div className="mt-2 flex items-center gap-2">
        <span className={`h-2.5 w-2.5 rounded-full ${dotColor}`} />
        <span className="text-lg font-semibold text-[var(--color-text)]">{value}</span>
      </div>
      {detail && <p className="mt-1 text-xs text-[var(--color-text-muted)]">{detail}</p>}
    </div>
  );
}

function getUserColor(msg: ChatMsg): string {
  if (msg.isBroadcaster) return "#ff4444";
  if (msg.isMod) return "#00cc00";
  if (msg.isSubscriber) return "#a970ff";
  return "#9ca3af";
}
