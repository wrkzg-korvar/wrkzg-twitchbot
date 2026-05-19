import { formatTimeAgo } from "../../lib/formatters";
import type { LiveEvent } from "../../types/status";

interface EventFeedProps {
  events: LiveEvent[];
}

const EVENT_ICONS: Record<LiveEvent["type"], string> = {
  follow: "❤️",
  subscribe: "⭐",
  gift: "🎁",
  resub: "🔄",
  raid: "⚔️",
};

export function EventFeed({ events }: EventFeedProps) {
  if (events.length === 0) {
    return (
      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-4 py-3">
        <p className="text-xs text-[var(--color-text-muted)]">No recent events</p>
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-4 py-2.5 overflow-hidden">
      <div className="flex items-center gap-4 overflow-x-auto scrollbar-none px-4 py-2.5">
        {events.slice(0, 8).map((event, i) => (
          <div key={i} className="flex items-center gap-1.5 shrink-0 text-xs">
            <span>{EVENT_ICONS[event.type]}</span>
            <span className="font-medium text-[var(--color-text)]">{event.username}</span>
            {event.detail && (
              <span className="text-[var(--color-text-muted)]">({event.detail})</span>
            )}
            <span className="text-[var(--color-text-muted)]">· {formatTimeAgo(event.timestamp)}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
