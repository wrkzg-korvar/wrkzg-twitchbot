import { useEffect, useState } from "react";
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

interface BotStatusData {
  isConnected: boolean;
  channel: string | null;
  reason?: string;
}

export function Dashboard() {
  const { isConnected: signalRConnected, on, off } = useSignalR("/hubs/chat");
  const [botStatus, setBotStatus] = useState<BotStatusData>({ isConnected: false, channel: null });
  const [messages, setMessages] = useState<ChatMsg[]>([]);
  const [viewerCount, setViewerCount] = useState<number | null>(null);

  useEffect(() => {
    if (!signalRConnected) return;

    on<BotStatusData>("BotStatus", (status) => {
      setBotStatus(status);
    });

    on<ChatMsg>("ChatMessage", (msg) => {
      setMessages((prev) => [...prev.slice(-99), msg]);
    });

    on<number>("ViewerCount", (count) => {
      setViewerCount(count);
    });

    return () => {
      off("BotStatus");
      off("ChatMessage");
      off("ViewerCount");
    };
  }, [signalRConnected, on, off]);

  return (
    <div className="p-6 space-y-6">
      {/* ─── Header ─────────────────────────────────────────── */}
      <div>
        <h1 className="text-2xl font-bold text-white">Dashboard</h1>
        <p className="mt-1 text-sm text-gray-500">
          Overview of your bot and stream status.
        </p>
      </div>

      {/* ─── Status Cards ───────────────────────────────────── */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <StatusCard
          label="Bot Status"
          value={botStatus.isConnected ? "Connected" : "Offline"}
          detail={botStatus.channel ? `#${botStatus.channel}` : undefined}
          color={botStatus.isConnected ? "green" : "gray"}
        />
        <StatusCard
          label="SignalR"
          value={signalRConnected ? "Connected" : "Disconnected"}
          color={signalRConnected ? "green" : "gray"}
        />
        <StatusCard
          label="Viewers"
          value={viewerCount !== null ? viewerCount.toString() : "—"}
          detail="Updates every 60s while live"
          color="purple"
        />
      </div>

      {/* ─── Live Chat Feed ─────────────────────────────────── */}
      <div className="rounded-lg border border-gray-800 bg-gray-900/50">
        <div className="flex items-center justify-between border-b border-gray-800 px-4 py-3">
          <h2 className="text-sm font-semibold text-gray-200">Live Chat</h2>
          <span className="text-xs text-gray-500">{messages.length} messages</span>
        </div>

        <div className="h-80 overflow-y-auto p-4 space-y-1.5">
          {messages.length === 0 ? (
            <p className="text-center text-sm text-gray-600 py-8">
              {botStatus.isConnected
                ? "Waiting for chat messages…"
                : "Bot is offline. Connect your accounts in Settings to start."}
            </p>
          ) : (
            messages.map((msg, i) => (
              <div key={i} className="text-sm">
                <span className="font-semibold" style={{ color: getUserColor(msg) }}>
                  {msg.displayName}
                </span>
                <span className="text-gray-500">: </span>
                <span className="text-gray-300">{msg.content}</span>
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
    <div className="rounded-lg border border-gray-800 bg-gray-900/50 p-4">
      <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">{label}</p>
      <div className="mt-2 flex items-center gap-2">
        <span className={`h-2.5 w-2.5 rounded-full ${dotColor}`} />
        <span className="text-lg font-semibold text-gray-100">{value}</span>
      </div>
      {detail && <p className="mt-1 text-xs text-gray-500">{detail}</p>}
    </div>
  );
}

function getUserColor(msg: ChatMsg): string {
  if (msg.isBroadcaster) return "#ff4444";
  if (msg.isMod) return "#00cc00";
  if (msg.isSubscriber) return "#a970ff";
  return "#9ca3af";
}
