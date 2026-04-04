import { useEffect, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useSignalR } from "../hooks/useSignalR";
import { useAuthStatus } from "../hooks/useAuthStatus";
import { statusApi } from "../api/status";
import { countersApi } from "../api/counters";
import { StatusCards } from "../components/dashboard/StatusCards";
import { LiveChat } from "../components/dashboard/LiveChat";
import { EventFeed } from "../components/dashboard/EventFeed";
import type { StatusResponse, ChatMsg, LiveEvent } from "../types/status";
import type { Counter } from "../types/counters";

export function DashboardPage() {
  const queryClient = useQueryClient();
  const { isConnected: signalRConnected, on, off } = useSignalR("/hubs/chat");
  const { bot: botAuth, broadcaster: broadcasterAuth } = useAuthStatus();

  const { data: status, isError } = useQuery<StatusResponse>({
    queryKey: ["status"],
    queryFn: () => statusApi.get(),
    refetchInterval: 15_000,
  });

  const { data: counters } = useQuery<Counter[]>({
    queryKey: ["counters"],
    queryFn: countersApi.getAll,
  });

  const [messages, setMessages] = useState<ChatMsg[]>([]);
  const [recentLoaded, setRecentLoaded] = useState(false);
  const [events, setEvents] = useState<LiveEvent[]>([]);

  const botConnected = status?.bot.isConnected ?? false;

  // Load recent messages on mount
  useEffect(() => {
    if (recentLoaded) return;
    statusApi
      .getRecentChat()
      .then((data) => {
        if (data.length > 0) {
          setMessages(data);
        }
        setRecentLoaded(true);
      })
      .catch(() => setRecentLoaded(true));
  }, [recentLoaded]);

  // SignalR listeners
  useEffect(() => {
    if (!signalRConnected) return;

    on<ChatMsg>("ChatMessage", (msg) => {
      setMessages((prev) => [...prev.slice(-99), msg]);
    });

    on("BotStatus", () => {
      queryClient.invalidateQueries({ queryKey: ["status"] });
    });

    on<number>("ViewerCount", () => {
      queryClient.invalidateQueries({ queryKey: ["status"] });
    });

    const addEvent = (event: LiveEvent) => {
      setEvents((prev) => [event, ...prev].slice(0, 20));
    };

    on<{ username: string; timestamp: string }>("FollowEvent", (e) => {
      addEvent({
        type: "follow",
        username: e.username,
        timestamp: e.timestamp,
      });
    });

    on<{ username: string; tier: number; timestamp: string }>(
      "SubscribeEvent",
      (e) => {
        addEvent({
          type: "subscribe",
          username: e.username,
          detail: `Tier ${e.tier}`,
          timestamp: e.timestamp,
        });
      },
    );

    on<{ username: string; count: number; tier: number; timestamp: string }>(
      "GiftSubEvent",
      (e) => {
        addEvent({
          type: "gift",
          username: e.username,
          detail: `${e.count}x Tier ${e.tier}`,
          timestamp: e.timestamp,
        });
      },
    );

    on<{ username: string; months: number; tier: number; timestamp: string }>(
      "ResubEvent",
      (e) => {
        addEvent({
          type: "resub",
          username: e.username,
          detail: `${e.months} months`,
          timestamp: e.timestamp,
        });
      },
    );

    on<{ username: string; viewers: number; timestamp: string }>(
      "RaidEvent",
      (e) => {
        addEvent({
          type: "raid",
          username: e.username,
          detail: `${e.viewers} viewers`,
          timestamp: e.timestamp,
        });
      },
    );

    on("CounterUpdated", () => {
      queryClient.invalidateQueries({ queryKey: ["counters"] });
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
      off("CounterUpdated");
    };
  }, [signalRConnected, on, off, queryClient]);

  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-[var(--color-text-muted)]">
        <p className="text-lg font-medium">Failed to load data</p>
        <p className="mt-1 text-sm">Please check your connection and try again.</p>
      </div>
    );
  }

  return (
    <div className="flex h-full flex-col gap-6 overflow-hidden p-6">
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">
          Dashboard
        </h1>
        <p className="mt-1 text-sm text-[var(--color-text-muted)]">
          Overview of your bot and stream status.
        </p>
      </div>

      <StatusCards status={status} />

      {counters && counters.length > 0 && (
        <div>
          <h2 className="text-sm font-semibold text-[var(--color-text-secondary)] mb-2">Counters</h2>
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6">
            {counters.map((c) => (
              <div
                key={c.id}
                className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-center"
              >
                <div className="text-xs text-[var(--color-text-muted)] truncate" title={c.name}>
                  {c.name}
                </div>
                <div className="text-lg font-bold text-[var(--color-text)] tabular-nums">
                  {c.value}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      <EventFeed events={events} />

      <LiveChat
        messages={messages}
        botConnected={botConnected}
        botDisplayName={botAuth.twitchDisplayName || "Bot"}
        broadcasterDisplayName={
          broadcasterAuth.twitchDisplayName || "Broadcaster"
        }
      />
    </div>
  );
}
