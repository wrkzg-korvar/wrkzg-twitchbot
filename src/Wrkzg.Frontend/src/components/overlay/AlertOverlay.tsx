import { useEffect, useState, useRef, useCallback } from "react";
import { useOverlaySignalR } from "../../hooks/useOverlaySignalR";
import { useOverlayConfig } from "../../hooks/useOverlayConfig";
import { OverlayShell } from "./OverlayShell";

interface AlertEvent {
  type: string;
  user: string;
  tier?: string;
  count?: number;
  viewers?: number;
  message?: string;
}

interface QueuedAlert {
  id: number;
  event: AlertEvent;
}

const EventIcons: Record<string, string> = {
  FollowEvent: "\u2B50",
  SubscribeEvent: "\uD83D\uDC9C",
  GiftSubEvent: "\uD83C\uDF81",
  ResubEvent: "\uD83D\uDD01",
  RaidEvent: "\u2694\uFE0F",
};

const DefaultMessages: Record<string, string> = {
  FollowEvent: "{user} just followed!",
  SubscribeEvent: "{user} subscribed at Tier {tier}!",
  GiftSubEvent: "{user} gifted {count} subs!",
  ResubEvent: "{user} resubscribed!",
  RaidEvent: "{user} is raiding with {viewers} viewers!",
};

function formatMessage(template: string, event: AlertEvent): string {
  return template
    .replace("{user}", event.user)
    .replace("{tier}", event.tier ?? "1")
    .replace("{count}", String(event.count ?? 1))
    .replace("{viewers}", String(event.viewers ?? 0));
}

const AlertDefaults: Record<string, string> = {
  fontSize: "48",
  textColor: "#ffffff",
  duration: "5000",
  animation: "slideDown",
};

const EventNames = [
  "FollowEvent",
  "SubscribeEvent",
  "GiftSubEvent",
  "ResubEvent",
  "RaidEvent",
];

export function AlertOverlay() {
  const config = useOverlayConfig("alerts", AlertDefaults);
  const { on, off } = useOverlaySignalR("/hubs/chat");
  const [currentAlert, setCurrentAlert] = useState<QueuedAlert | null>(null);
  const queueRef = useRef<QueuedAlert[]>([]);
  const idRef = useRef(0);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const showNext = useCallback(() => {
    if (queueRef.current.length > 0) {
      const next = queueRef.current.shift()!;
      setCurrentAlert(next);
      timerRef.current = setTimeout(() => {
        setCurrentAlert(null);
        // Small gap between alerts
        setTimeout(showNext, 300);
      }, Number(config.duration) || 5000);
    }
  }, [config.duration]);

  const enqueueAlert = useCallback(
    (event: AlertEvent) => {
      idRef.current += 1;
      queueRef.current.push({ id: idRef.current, event });
      if (!timerRef.current) {
        showNext();
      }
    },
    [showNext],
  );

  useEffect(() => {
    const handlers: Array<[string, (data: AlertEvent) => void]> = [];

    for (const eventName of EventNames) {
      const handler = (data: AlertEvent) => {
        enqueueAlert({ ...data, type: eventName });
      };
      on(eventName, handler);
      handlers.push([eventName, handler]);
    }

    return () => {
      for (const [name] of handlers) {
        off(name);
      }
      if (timerRef.current) {
        clearTimeout(timerRef.current);
        timerRef.current = null;
      }
    };
  }, [on, off, enqueueAlert]);

  // Reset timer ref when alert clears
  useEffect(() => {
    if (currentAlert === null) {
      timerRef.current = null;
    }
  }, [currentAlert]);

  if (!currentAlert) {
    return <OverlayShell><div /></OverlayShell>;
  }

  const { event } = currentAlert;
  const icon = EventIcons[event.type] ?? "";
  const template = DefaultMessages[event.type] ?? "{user}";
  const message = formatMessage(template, event);
  const animation = config.animation || "slideDown";

  return (
    <OverlayShell>
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          height: "100%",
          animation: `${animation} 0.6s ease-out`,
        }}
      >
        <div
          style={{
            fontSize: `${Math.round(Number(config.fontSize) * 1.5)}px`,
            marginBottom: "16px",
          }}
        >
          {icon}
        </div>
        <div
          className="overlay-text"
          style={{
            fontSize: `${config.fontSize}px`,
            color: config.textColor,
            fontWeight: 700,
            textAlign: "center",
            padding: "0 32px",
          }}
        >
          {message}
        </div>
      </div>
    </OverlayShell>
  );
}
