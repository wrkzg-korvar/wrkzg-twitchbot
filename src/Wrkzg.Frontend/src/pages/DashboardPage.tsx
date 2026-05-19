import { useEffect, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useSignalR } from "../hooks/useSignalR";
import { useAuthStatus } from "../hooks/useAuthStatus";
import { statusApi } from "../api/status";
import { StatusCards } from "../components/dashboard/StatusCards";
import { LiveChat } from "../components/dashboard/LiveChat";
import { EventFeed } from "../components/dashboard/EventFeed";
import { LiveViewerList } from "../components/dashboard/LiveViewerList";
import type { StatusResponse, ChatMsg, LiveEvent } from "../types/status";

export function DashboardPage() {
  const queryClient = useQueryClient();
  const { isConnected: signalRConnected, on, off } = useSignalR("/hubs/chat");
  const { bot: botAuth, broadcaster: broadcasterAuth } = useAuthStatus();

  const { data: status, isError } = useQuery<StatusResponse>({
    queryKey: ["status"],
    queryFn: () => statusApi.get(),
    refetchInterval: 15_000,
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

    on("ModerationAction", () => {
      queryClient.invalidateQueries({ queryKey: ["live-viewers"] });
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
      off("ModerationAction");
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
    <div className="flex h-full flex-col overflow-hidden p-6 gap-4">
      <div className="shrink-0">
        <h1 className="text-2xl font-bold text-[var(--color-text)]">Dashboard</h1>
        <p className="mt-1 text-sm text-[var(--color-text-muted)]">
          Live chat, viewers, and moderation tools.
        </p>
      </div>

      <div className="shrink-0 space-y-3">
        <StatusCards status={status} />
        <EventFeed events={events} />
      </div>

      <div className="flex flex-1 min-h-0 gap-4">
        <div className="flex-[2] min-w-0 flex flex-col">
          <LiveChat
            messages={messages}
            botConnected={botConnected}
            botDisplayName={botAuth.twitchDisplayName || "Bot"}
            broadcasterDisplayName={
              broadcasterAuth.twitchDisplayName || "Broadcaster"
            }
          />
        </div>

        <div className="flex-1 min-w-0 hidden md:flex flex-col">
          <LiveViewerList />
        </div>
      </div>
    </div>
  );
}
