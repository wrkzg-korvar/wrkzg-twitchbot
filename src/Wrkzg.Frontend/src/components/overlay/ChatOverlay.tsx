import { useEffect, useState, useRef } from "react";
import { useOverlaySignalR } from "../../hooks/useOverlaySignalR";
import { useOverlayConfig } from "../../hooks/useOverlayConfig";
import { renderWithEmotes } from "../../lib/emotes";
import { OverlayShell } from "./OverlayShell";

interface IncomingChatMessage {
  displayName: string;
  content: string;
  isBroadcaster: boolean;
  isMod: boolean;
  isSubscriber: boolean;
  emotes: Record<string, string[]> | null;
}

interface DisplayMessage {
  id: number;
  displayName: string;
  content: string;
  isBroadcaster: boolean;
  isMod: boolean;
  isSubscriber: boolean;
  emotes: Record<string, string[]>;
  timestamp: number;
}

const ROLE_COLORS = {
  broadcaster: "#ff4444",
  mod: "#00cc00",
  sub: "#a970ff",
  viewer: "#aaaaaa",
};

function getRoleColor(msg: DisplayMessage): string {
  if (msg.isBroadcaster) return ROLE_COLORS.broadcaster;
  if (msg.isMod) return ROLE_COLORS.mod;
  if (msg.isSubscriber) return ROLE_COLORS.sub;
  return ROLE_COLORS.viewer;
}

const ChatDefaults: Record<string, string> = {
  fontSize: "16",
  textColor: "#ffffff",
  maxMessages: "15",
  fadeAfter: "30",
};

let nextMsgId = 0;

export function ChatOverlay() {
  const config = useOverlayConfig("chat", ChatDefaults);
  const { on, off } = useOverlaySignalR("/hubs/chat");
  const [messages, setMessages] = useState<DisplayMessage[]>([]);
  const containerRef = useRef<HTMLDivElement>(null);
  const maxMessages = Number(config.maxMessages) || 15;
  const fadeAfterMs = (Number(config.fadeAfter) || 30) * 1000;
  const fontSize = Number(config.fontSize) || 16;

  useEffect(() => {
    on<IncomingChatMessage>("ChatMessage", (data) => {
      nextMsgId += 1;
      const msg: DisplayMessage = {
        id: nextMsgId,
        displayName: data.displayName,
        content: data.content,
        isBroadcaster: data.isBroadcaster,
        isMod: data.isMod,
        isSubscriber: data.isSubscriber,
        emotes: data.emotes ?? {},
        timestamp: Date.now(),
      };

      setMessages((prev) => {
        const updated = [...prev, msg];
        return updated.length > maxMessages
          ? updated.slice(updated.length - maxMessages)
          : updated;
      });
    });

    return () => {
      off("ChatMessage");
    };
  }, [on, off, maxMessages]);

  // Fade out old messages
  useEffect(() => {
    if (fadeAfterMs <= 0) return;
    const interval = setInterval(() => {
      const now = Date.now();
      setMessages((prev) => prev.filter((m) => now - m.timestamp < fadeAfterMs));
    }, 1000);
    return () => clearInterval(interval);
  }, [fadeAfterMs]);

  // Auto-scroll to bottom
  useEffect(() => {
    if (containerRef.current) {
      containerRef.current.scrollTop = containerRef.current.scrollHeight;
    }
  }, [messages]);

  return (
    <OverlayShell>
      <div
        ref={containerRef}
        style={{
          position: "absolute",
          bottom: 0,
          left: 0,
          right: 0,
          maxHeight: "100vh",
          display: "flex",
          flexDirection: "column",
          justifyContent: "flex-end",
          padding: "12px 16px",
          overflow: "hidden",
        }}
      >
        {messages.map((msg) => {
          const age = Date.now() - msg.timestamp;
          const fadeStart = fadeAfterMs * 0.75;
          const opacity =
            fadeAfterMs > 0 && age > fadeStart
              ? Math.max(0, 1 - (age - fadeStart) / (fadeAfterMs * 0.25))
              : 1;

          return (
            <div
              key={msg.id}
              className="overlay-text"
              style={{
                fontSize: `${fontSize}px`,
                lineHeight: "1.5",
                marginBottom: "6px",
                opacity,
                transition: "opacity 1s ease",
                animation: "slideInLeft 0.3s ease-out",
              }}
            >
              <span
                style={{
                  fontWeight: 700,
                  color: getRoleColor(msg),
                  marginRight: "6px",
                }}
              >
                {msg.displayName}:
              </span>
              <span style={{ color: config.textColor }}>
                {renderWithEmotes(msg.content, msg.emotes, Math.round(fontSize * 1.5))}
              </span>
            </div>
          );
        })}
      </div>
    </OverlayShell>
  );
}
