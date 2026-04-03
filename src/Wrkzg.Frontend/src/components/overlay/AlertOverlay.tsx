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
  months?: number;
  message?: string;
  rewardTitle?: string;
  cost?: number;
  userInput?: string;
}

interface QueuedAlert {
  id: number;
  event: AlertEvent;
}

interface EventConfig {
  enabled: boolean;
  image: string;
  sound: string;
  soundVolume: number;
  message: string;
  animation: string;
  duration: number;
}

// Maps SignalR event name → settings prefix
const EventPrefixMap: Record<string, string> = {
  FollowEvent: "follow",
  SubscribeEvent: "subscribe",
  GiftSubEvent: "giftsub",
  ResubEvent: "resub",
  RaidEvent: "raid",
  ChannelPointRedemption: "channelpoint",
};

const EventIcons: Record<string, string> = {
  FollowEvent: "\u2B50",
  SubscribeEvent: "\uD83D\uDC9C",
  GiftSubEvent: "\uD83C\uDF81",
  ResubEvent: "\uD83D\uDD01",
  RaidEvent: "\u2694\uFE0F",
  ChannelPointRedemption: "\uD83D\uDC8E",
};

const DefaultMessages: Record<string, string> = {
  FollowEvent: "{user} just followed!",
  SubscribeEvent: "{user} subscribed at Tier {tier}!",
  GiftSubEvent: "{user} gifted {count} subs!",
  ResubEvent: "{user} resubscribed for {months} months!",
  RaidEvent: "{user} is raiding with {viewers} viewers!",
  ChannelPointRedemption: "{user} redeemed {rewardTitle}!",
};

function formatMessage(template: string, event: AlertEvent): string {
  return template
    .replace("{user}", event.user)
    .replace("{tier}", event.tier ?? "1")
    .replace("{count}", String(event.count ?? 1))
    .replace("{viewers}", String(event.viewers ?? 0))
    .replace("{months}", String(event.months ?? 1))
    .replace("{rewardTitle}", event.rewardTitle ?? "")
    .replace("{cost}", String(event.cost ?? 0));
}

function getEventConfig(config: Record<string, string>, eventType: string): EventConfig {
  const prefix = EventPrefixMap[eventType] ?? "follow";
  return {
    enabled: config[`${prefix}.enabled`] !== "false",
    image: config[`${prefix}.image`] || "",
    sound: config[`${prefix}.sound`] || "",
    soundVolume: parseInt(config[`${prefix}.soundVolume`] || "80"),
    message: config[`${prefix}.message`] || DefaultMessages[eventType] || "{user}",
    animation: config[`${prefix}.animation`] || config.animation || "slideDown",
    duration: parseInt(config[`${prefix}.duration`] || config.duration || "5000"),
  };
}

function playSound(url: string, volume: number) {
  if (!url) { return; }
  const audio = new Audio(url);
  audio.volume = Math.max(0, Math.min(1, volume / 100));
  audio.play().catch(() => {});
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
  "ChannelPointRedemption",
];

export function AlertOverlay() {
  const config = useOverlayConfig("alerts", AlertDefaults);
  const { on, off } = useOverlaySignalR("/hubs/chat");
  const [currentAlert, setCurrentAlert] = useState<QueuedAlert | null>(null);
  const queueRef = useRef<QueuedAlert[]>([]);
  const idRef = useRef(0);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const configRef = useRef(config);
  configRef.current = config;

  const showNext = useCallback(() => {
    if (queueRef.current.length > 0) {
      const next = queueRef.current.shift()!;
      const eventCfg = getEventConfig(configRef.current, next.event.type);

      // Play sound for this event
      playSound(eventCfg.sound, eventCfg.soundVolume);

      setCurrentAlert(next);
      timerRef.current = setTimeout(() => {
        setCurrentAlert(null);
        setTimeout(showNext, 300);
      }, eventCfg.duration);
    }
  }, []);

  const enqueueAlert = useCallback(
    (event: AlertEvent) => {
      const eventCfg = getEventConfig(configRef.current, event.type);
      if (!eventCfg.enabled) { return; }

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
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const handler = (data: any) => {
        enqueueAlert({
          ...data,
          type: eventName,
          user: data.user ?? data.username ?? "",
        });
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

  useEffect(() => {
    if (currentAlert === null) {
      timerRef.current = null;
    }
  }, [currentAlert]);

  // Load custom CSS
  useEffect(() => {
    const css = config.customCSS;
    if (css) {
      const style = document.createElement("style");
      style.id = "wrkzg-custom-css";
      style.textContent = css;
      document.head.appendChild(style);
      return () => { document.getElementById("wrkzg-custom-css")?.remove(); };
    }
  }, [config.customCSS]);

  // Load Google Font
  useEffect(() => {
    const font = config.fontFamily;
    if (font && font !== "system-ui" && !font.startsWith("system")) {
      const link = document.createElement("link");
      link.href = `https://fonts.googleapis.com/css2?family=${encodeURIComponent(font)}:wght@400;700&display=swap`;
      link.rel = "stylesheet";
      document.head.appendChild(link);
      return () => { link.remove(); };
    }
  }, [config.fontFamily]);

  // Listen for live config updates from editor via postMessage
  useEffect(() => {
    function handleMessage(e: MessageEvent) {
      if (e.data?.type === "wrkzg:config-update") {
        // Config updates are handled by useOverlayConfig internally
        // but we can trigger re-render here
      }
    }
    window.addEventListener("message", handleMessage);
    return () => window.removeEventListener("message", handleMessage);
  }, []);

  if (!currentAlert) {
    return <OverlayShell><div /></OverlayShell>;
  }

  const { event } = currentAlert;
  const eventCfg = getEventConfig(config, event.type);
  const icon = EventIcons[event.type] ?? "";
  const message = formatMessage(eventCfg.message, event);
  const animation = eventCfg.animation || "slideDown";

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
          fontFamily: config.fontFamily || "system-ui",
        }}
      >
        {eventCfg.image ? (
          <img
            src={eventCfg.image}
            alt=""
            className="alert-image"
            style={{ maxWidth: "128px", maxHeight: "128px", marginBottom: "16px" }}
          />
        ) : (
          <div style={{ fontSize: `${Math.round(Number(config.fontSize) * 1.5)}px`, marginBottom: "16px" }}>
            {icon}
          </div>
        )}
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
