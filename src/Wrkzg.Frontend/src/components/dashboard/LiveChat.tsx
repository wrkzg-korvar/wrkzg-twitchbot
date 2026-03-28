import { useRef, useState, useEffect } from "react";
import { renderWithEmotes } from "../../lib/emotes";
import type { ChatMsg } from "../../types/status";

interface LiveChatProps {
  messages: ChatMsg[];
  botConnected: boolean;
  botDisplayName: string;
  broadcasterDisplayName: string;
}

export function LiveChat({
  messages,
  botConnected,
  botDisplayName,
  broadcasterDisplayName,
}: LiveChatProps) {
  const chatContainerRef = useRef<HTMLDivElement>(null);
  const chatEndRef = useRef<HTMLDivElement>(null);
  const [isAtBottom, setIsAtBottom] = useState(true);

  const [sendAs, setSendAs] = useState<"bot" | "broadcaster">(
    () =>
      (localStorage.getItem("wrkzg-chat-send-as") as "bot" | "broadcaster") ||
      "bot",
  );
  const [chatInput, setChatInput] = useState("");
  const [isSending, setIsSending] = useState(false);

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
    <div className="flex flex-1 min-h-0 flex-col rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)]">
      <div className="flex items-center justify-between border-b border-[var(--color-border)] px-4 py-3">
        <h2 className="text-sm font-semibold text-[var(--color-text)]">
          Live Chat
        </h2>
        <span className="text-xs text-[var(--color-text-muted)]">
          {messages.length} messages
        </span>
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
              ? "Waiting for chat messages..."
              : "Bot is not connected. Check Settings."}
          </p>
        ) : (
          messages.map((msg, i) => (
            <div key={i} className="text-sm">
              <span
                className="font-semibold"
                style={{ color: getUserColor(msg) }}
              >
                {msg.displayName}
              </span>
              <span className="text-[var(--color-text-muted)]">: </span>
              <span className="text-[var(--color-text)]">{renderWithEmotes(msg.content, msg.emotes, 20)}</span>
            </div>
          ))
        )}
        <div ref={chatEndRef} />
      </div>

      {/* Chat Input */}
      <div className="border-t border-[var(--color-border)] p-3 flex items-center gap-2">
        <select
          value={sendAs}
          onChange={(e) =>
            handleSendAsChange(e.target.value as "bot" | "broadcaster")
          }
          className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-xs text-[var(--color-text)] focus:border-[var(--color-brand)] focus:outline-none"
        >
          <option value="bot">{botDisplayName || "Bot"}</option>
          <option value="broadcaster">
            {broadcasterDisplayName || "Broadcaster"}
          </option>
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
  );
}

function getUserColor(msg: ChatMsg): string {
  if (msg.isBroadcaster) return "#ff4444";
  if (msg.isMod) return "#00cc00";
  if (msg.isSubscriber) return "#a970ff";
  return "#9ca3af";
}
