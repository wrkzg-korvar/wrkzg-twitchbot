import { formatTimeAgo } from "../../lib/formatters";
import type { LiveEvent } from "../../types/status";

interface EventFeedProps {
  events: LiveEvent[];
}

const EVENT_ICONS: Record<LiveEvent["type"], string> = {
  follow: "!",
  subscribe: "*",
  gift: "+",
  resub: "*",
  raid: ">",
};

const EVENT_LABELS: Record<LiveEvent["type"], string> = {
  follow: "followed",
  subscribe: "subscribed",
  gift: "gifted subs",
  resub: "resubscribed",
  raid: "raided",
};

export function EventFeed({ events }: EventFeedProps) {
  if (events.length === 0) {
    return null;
  }

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
      <h2 className="text-sm font-semibold text-[var(--color-text)] mb-2">
        Recent Events
      </h2>
      <div className="space-y-1">
        {events.slice(0, 5).map((event, i) => {
          const ago = formatTimeAgo(event.timestamp);
          return (
            <div
              key={i}
              className="flex items-center justify-between text-xs"
            >
              <span className="text-[var(--color-text-secondary)]">
                {EVENT_ICONS[event.type]}{" "}
                <strong>{event.username}</strong> {EVENT_LABELS[event.type]}
                {event.detail && (
                  <span className="text-[var(--color-text-muted)]">
                    {" "}
                    ({event.detail})
                  </span>
                )}
              </span>
              <span className="text-[var(--color-text-muted)]">{ago}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
}
