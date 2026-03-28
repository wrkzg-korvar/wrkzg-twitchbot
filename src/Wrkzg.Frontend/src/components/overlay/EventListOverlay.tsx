import { useEffect, useState } from "react";
import { useOverlaySignalR } from "../../hooks/useOverlaySignalR";
import { useOverlayConfig } from "../../hooks/useOverlayConfig";
import { OverlayShell } from "./OverlayShell";

interface EventItem {
  id: number;
  type: string;
  text: string;
  timestamp: number;
}

interface EventPayload {
  user: string;
  tier?: string;
  count?: number;
  viewers?: number;
}

const EventIcons: Record<string, string> = {
  FollowEvent: "\u2B50",
  SubscribeEvent: "\uD83D\uDC9C",
  GiftSubEvent: "\uD83C\uDF81",
  ResubEvent: "\uD83D\uDD01",
  RaidEvent: "\u2694\uFE0F",
};

function formatEventText(type: string, data: EventPayload): string {
  switch (type) {
    case "FollowEvent":
      return `${data.user} followed`;
    case "SubscribeEvent":
      return `${data.user} subscribed (T${data.tier ?? "1"})`;
    case "GiftSubEvent":
      return `${data.user} gifted ${data.count ?? 1} sub${(data.count ?? 1) > 1 ? "s" : ""}`;
    case "ResubEvent":
      return `${data.user} resubscribed`;
    case "RaidEvent":
      return `${data.user} raided with ${data.viewers ?? 0}`;
    default:
      return `${data.user}`;
  }
}

const EventListDefaults: Record<string, string> = {
  fontSize: "20",
  textColor: "#ffffff",
  maxEvents: "5",
  fadeAfterMs: "60000",
};

const EventNames = [
  "FollowEvent",
  "SubscribeEvent",
  "GiftSubEvent",
  "ResubEvent",
  "RaidEvent",
];

let nextEventId = 0;

export function EventListOverlay() {
  const config = useOverlayConfig("events", EventListDefaults);
  const { on, off } = useOverlaySignalR("/hubs/chat");
  const [events, setEvents] = useState<EventItem[]>([]);
  const maxEvents = Number(config.maxEvents) || 5;
  const fadeAfterMs = Number(config.fadeAfterMs) || 60000;

  useEffect(() => {
    for (const eventName of EventNames) {
      on<EventPayload>(eventName, (data) => {
        nextEventId += 1;
        const item: EventItem = {
          id: nextEventId,
          type: eventName,
          text: formatEventText(eventName, data),
          timestamp: Date.now(),
        };
        setEvents((prev) => {
          const updated = [...prev, item];
          if (updated.length > maxEvents) {
            return updated.slice(updated.length - maxEvents);
          }
          return updated;
        });
      });
    }

    return () => {
      for (const name of EventNames) {
        off(name);
      }
    };
  }, [on, off, maxEvents]);

  // Fade out old events
  useEffect(() => {
    const interval = setInterval(() => {
      const now = Date.now();
      setEvents((prev) =>
        prev.filter((e) => now - e.timestamp < fadeAfterMs),
      );
    }, 1000);
    return () => clearInterval(interval);
  }, [fadeAfterMs]);

  if (events.length === 0) {
    return null;
  }

  const fontSize = Number(config.fontSize) || 20;

  return (
    <OverlayShell>
      <div
        style={{
          position: "absolute",
          top: "16px",
          left: "16px",
          display: "flex",
          flexDirection: "column",
          gap: "6px",
        }}
      >
        {events.map((evt) => {
          const age = Date.now() - evt.timestamp;
          const fadeStart = fadeAfterMs * 0.8;
          const opacity =
            age > fadeStart
              ? Math.max(0, 1 - (age - fadeStart) / (fadeAfterMs * 0.2))
              : 1;

          return (
            <div
              key={evt.id}
              className="overlay-text"
              style={{
                fontSize: `${fontSize}px`,
                color: config.textColor,
                animation: "slideInLeft 0.4s ease-out",
                opacity,
                transition: "opacity 0.5s ease",
                background: "rgba(0, 0, 0, 0.5)",
                padding: "6px 12px",
                borderRadius: "8px",
              }}
            >
              <span style={{ marginRight: "8px" }}>
                {EventIcons[evt.type] ?? ""}
              </span>
              {evt.text}
            </div>
          );
        })}
      </div>
    </OverlayShell>
  );
}
