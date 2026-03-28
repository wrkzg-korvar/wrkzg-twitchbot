import { useEffect, useRef, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useSignalR } from "../hooks/useSignalR";
import { useAuthStatus } from "../hooks/useAuthStatus";

interface ChatMsg {
  username: string;
  displayName: string;
  content: string;
  isMod: boolean;
  isSubscriber: boolean;
  isBroadcaster: boolean;
  timestamp: string;
}

interface LiveEvent {
  type: "follow" | "subscribe" | "gift" | "resub" | "raid";
  username: string;
  detail?: string;
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
  const { bot: botAuth, broadcaster: broadcasterAuth } = useAuthStatus();

  // Fetch status via REST — this is the source of truth for bot/stream status
  const { data: status } = useQuery<StatusResponse>({
    queryKey: ["status"],
    queryFn: fetchStatus,
    refetchInterval: 15_000,
  });

  const [messages, setMessages] = useState<ChatMsg[]>([]);
  const [recentLoaded, setRecentLoaded] = useState(false);
  const [events, setEvents] = useState<LiveEvent[]>([]);

  // Auto-scroll state
  const chatContainerRef = useRef<HTMLDivElement>(null);
  const chatEndRef = useRef<HTMLDivElement>(null);
  const [isAtBottom, setIsAtBottom] = useState(true);

  // Chat input state
  const [sendAs, setSendAs] = useState<"bot" | "broadcaster">(
    () => (localStorage.getItem("wrkzg-chat-send-as") as "bot" | "broadcaster") || "bot"
  );
  const [chatInput, setChatInput] = useState("");
  const [isSending, setIsSending] = useState(false);

  // Derive bot status and viewer count directly from query data
  const botConnected = status?.bot.isConnected ?? false;
  const botChannel = status?.bot.channel ?? null;
  const viewerCount = status?.stream.isLive ? status.stream.viewerCount : null;
  const streamGame = status?.stream.isLive ? status.stream.game : null;

  // Load recent messages on mount (for when navigating back to dashboard)
  useEffect(() => {
    if (recentLoaded) return;
    fetch("/api/chat/recent")
      .then((res) => (res.ok ? res.json() : []))
      .then((data: ChatMsg[]) => {
        if (data.length > 0) {
          setMessages(data);
        }
        setRecentLoaded(true);
      })
      .catch(() => setRecentLoaded(true));
  }, [recentLoaded]);

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

    // Live event notifications
    const addEvent = (event: LiveEvent) => {
      setEvents((prev) => [event, ...prev].slice(0, 20));
    };

    on<{ username: string; timestamp: string }>("FollowEvent", (e) => {
      addEvent({ type: "follow", username: e.username, timestamp: e.timestamp });
    });

    on<{ username: string; tier: number; timestamp: string }>("SubscribeEvent", (e) => {
      addEvent({ type: "subscribe", username: e.username, detail: `Tier ${e.tier}`, timestamp: e.timestamp });
    });

    on<{ username: string; count: number; tier: number; timestamp: string }>("GiftSubEvent", (e) => {
      addEvent({ type: "gift", username: e.username, detail: `${e.count}x Tier ${e.tier}`, timestamp: e.timestamp });
    });

    on<{ username: string; months: number; tier: number; timestamp: string }>("ResubEvent", (e) => {
      addEvent({ type: "resub", username: e.username, detail: `${e.months} months`, timestamp: e.timestamp });
    });

    on<{ username: string; viewers: number; timestamp: string }>("RaidEvent", (e) => {
      addEvent({ type: "raid", username: e.username, detail: `${e.viewers} viewers`, timestamp: e.timestamp });
    });

    return () => {
      off("ChatMessage");
      off("BotStatus");
      off("ViewerCount");
      off("FollowEvent");
      off("SubscribeEvent");
      off("GiftSubEvent");
      off("ResubEvent");
      off("RaidEvent");
    };
  }, [signalRConnected, on, off, queryClient]);

  // Auto-scroll to bottom when new messages arrive (only if already at bottom)
  useEffect(() => {
    if (isAtBottom) {
      chatEndRef.current?.scrollIntoView({ behavior: "smooth" });
    }
  }, [messages, isAtBottom]);

  const handleScroll = () => {
    const el = chatContainerRef.current;
    if (!el) return;
    const threshold = 50;
    setIsAtBottom(el.scrollHeight - el.scrollTop - el.clientHeight < threshold);
  };

  const handleSendAsChange = (value: "bot" | "broadcaster") => {
    setSendAs(value);
    localStorage.setItem("wrkzg-chat-send-as", value);
  };

  const handleSend = async () => {
    if (!chatInput.trim() || isSending) return;
    setIsSending(true);
    try {
      const res = await fetch("/api/chat/send", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message: chatInput.trim(), sendAs }),
      });
      if (res.ok) {
        setChatInput("");
      }
    } finally {
      setIsSending(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="flex h-full flex-col gap-6 overflow-hidden p-6">
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

      {/* ─── Recent Events ──────────────────────────────────── */}
      {events.length > 0 && (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
          <h2 className="text-sm font-semibold text-[var(--color-text)] mb-2">Recent Events</h2>
          <div className="space-y-1">
            {events.slice(0, 5).map((event, i) => {
              const icons: Record<string, string> = {
                follow: "🎉", subscribe: "⭐", gift: "🎁", resub: "⭐", raid: "🚀",
              };
              const labels: Record<string, string> = {
                follow: "followed", subscribe: "subscribed", gift: "gifted subs",
                resub: "resubscribed", raid: "raided",
              };
              const ago = formatTimeAgo(event.timestamp);
              return (
                <div key={i} className="flex items-center justify-between text-xs">
                  <span className="text-[var(--color-text-secondary)]">
                    {icons[event.type]} <strong>{event.username}</strong> {labels[event.type]}
                    {event.detail && <span className="text-[var(--color-text-muted)]"> ({event.detail})</span>}
                  </span>
                  <span className="text-[var(--color-text-muted)]">{ago}</span>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* ─── Live Chat Feed ─────────────────────────────────── */}
      <div className="flex flex-1 min-h-0 flex-col rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)]">
        <div className="flex items-center justify-between border-b border-[var(--color-border)] px-4 py-3">
          <h2 className="text-sm font-semibold text-[var(--color-text)]">Live Chat</h2>
          <span className="text-xs text-[var(--color-text-muted)]">{messages.length} messages</span>
        </div>

        {/* Messages */}
        <div
          ref={chatContainerRef}
          onScroll={handleScroll}
          className="flex-1 min-h-0 overflow-y-auto p-4 space-y-1.5"
        >
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
          <div ref={chatEndRef} />
        </div>

        {/* Chat Input */}
        <div className="border-t border-[var(--color-border)] p-3 flex items-center gap-2">
          <select
            value={sendAs}
            onChange={(e) => handleSendAsChange(e.target.value as "bot" | "broadcaster")}
            className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-xs text-[var(--color-text)] focus:border-[var(--color-brand)] focus:outline-none"
          >
            <option value="bot">{botAuth.twitchDisplayName || "Bot"}</option>
            <option value="broadcaster">{broadcasterAuth.twitchDisplayName || "Broadcaster"}</option>
          </select>

          <input
            type="text"
            value={chatInput}
            onChange={(e) => setChatInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Type a message..."
            maxLength={500}
            disabled={!botConnected}
            className="flex-1 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-1.5 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none disabled:opacity-50"
          />

          <button
            onClick={handleSend}
            disabled={!chatInput.trim() || isSending || !botConnected}
            className="rounded-lg bg-[var(--color-brand)] px-3 py-1.5 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
          >
            Send
          </button>
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

function formatTimeAgo(timestamp: string): string {
  const seconds = Math.floor((Date.now() - new Date(timestamp).getTime()) / 1000);
  if (seconds < 60) return "just now";
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  return `${hours}h ago`;
}
