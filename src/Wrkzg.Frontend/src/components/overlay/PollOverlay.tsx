import { useEffect, useState, useRef } from "react";
import { useOverlaySignalR } from "../../hooks/useOverlaySignalR";
import { useOverlayConfig } from "../../hooks/useOverlayConfig";
import { OverlayShell } from "./OverlayShell";

interface PollCreatedEvent {
  id: number;
  question: string;
  options: string[];
  endsAt: string;
  durationSeconds: number;
}

interface PollVoteEvent {
  pollId: number;
  optionIndex: number;
}

interface PollEndedEvent {
  pollId: number;
}

interface OverlayPoll {
  id: number;
  question: string;
  options: string[];
  votes: number[];
  endsAt: string;
  isActive: boolean;
}

const BAR_COLORS = ["#8BBF4C", "#F59E0B", "#3b82f6", "#ef4444", "#a855f7", "#22c55e"];

const PollDefaults: Record<string, string> = {
  fontSize: "20",
  textColor: "#ffffff",
  barHeight: "28",
};

export function PollOverlay() {
  const config = useOverlayConfig("poll", PollDefaults);
  const { on, off } = useOverlaySignalR("/hubs/chat");
  const [poll, setPoll] = useState<OverlayPoll | null>(null);
  const [secondsLeft, setSecondsLeft] = useState<number | null>(null);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  // Fetch active poll on mount
  useEffect(() => {
    fetch("/api/overlays/data/poll/active")
      .then((res) => (res.ok ? res.json() : null))
      .then((data: PollCreatedEvent | null) => {
        if (data) {
          setPoll({
            id: data.id,
            question: data.question,
            options: data.options,
            votes: new Array(data.options.length).fill(0),
            endsAt: data.endsAt,
            isActive: true,
          });
        }
      })
      .catch(() => {});
  }, []);

  // Listen for poll events
  useEffect(() => {
    on<PollCreatedEvent>("PollCreated", (data) => {
      setPoll({
        id: data.id,
        question: data.question,
        options: data.options,
        votes: new Array(data.options.length).fill(0),
        endsAt: data.endsAt,
        isActive: true,
      });
    });

    on<PollVoteEvent>("PollVote", (data) => {
      setPoll((prev) => {
        if (!prev || prev.id !== data.pollId) return prev;
        const newVotes = [...prev.votes];
        if (data.optionIndex >= 0 && data.optionIndex < newVotes.length) {
          newVotes[data.optionIndex]++;
        }
        return { ...prev, votes: newVotes };
      });
    });

    on<PollEndedEvent>("PollEnded", (data) => {
      setPoll((prev) => {
        if (!prev || prev.id !== data.pollId) return prev;
        return { ...prev, isActive: false };
      });
      // Show results for 10s then hide
      setTimeout(() => setPoll(null), 10000);
    });

    return () => {
      off("PollCreated");
      off("PollVote");
      off("PollEnded");
    };
  }, [on, off]);

  // Countdown timer
  useEffect(() => {
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }

    if (!poll?.endsAt || !poll.isActive) {
      setSecondsLeft(null);
      return;
    }

    const updateTimer = () => {
      const remaining = Math.max(0, Math.ceil((new Date(poll.endsAt).getTime() - Date.now()) / 1000));
      setSecondsLeft(remaining);
      if (remaining <= 0 && timerRef.current) {
        clearInterval(timerRef.current);
        timerRef.current = null;
      }
    };

    updateTimer();
    timerRef.current = setInterval(updateTimer, 1000);

    return () => {
      if (timerRef.current) {
        clearInterval(timerRef.current);
        timerRef.current = null;
      }
    };
  }, [poll?.endsAt, poll?.isActive]);

  if (!poll) return null;

  const totalVotes = poll.votes.reduce((sum, v) => sum + v, 0);
  const fontSize = Number(config.fontSize) || 20;
  const barHeight = Number(config.barHeight) || 28;

  return (
    <OverlayShell>
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          height: "100%",
          padding: "24px",
        }}
      >
        <div
          style={{
            background: "rgba(0, 0, 0, 0.8)",
            borderRadius: "16px",
            padding: "20px 28px",
            width: "100%",
            maxWidth: "560px",
          }}
        >
          {/* Header */}
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "flex-start",
              marginBottom: "16px",
              gap: "12px",
            }}
          >
            <div
              className="overlay-text"
              style={{
                fontSize: `${fontSize}px`,
                fontWeight: 700,
                color: config.textColor,
                overflow: "hidden",
                textOverflow: "ellipsis",
                display: "-webkit-box",
                WebkitLineClamp: 2,
                WebkitBoxOrient: "vertical",
                flex: 1,
              }}
            >
              {poll.question}
            </div>
            {secondsLeft !== null && secondsLeft > 0 && (
              <div
                className="overlay-text"
                style={{
                  fontSize: `${fontSize * 0.75}px`,
                  color: "#F59E0B",
                  fontWeight: 600,
                  whiteSpace: "nowrap",
                  flexShrink: 0,
                }}
              >
                {secondsLeft}s
              </div>
            )}
          </div>

          {/* Options */}
          {poll.options.map((optionText, i) => {
            const votes = poll.votes[i] ?? 0;
            const pct = totalVotes > 0 ? (votes / totalVotes) * 100 : 0;

            return (
              <div key={i} style={{ marginBottom: "8px" }}>
                <div
                  className="overlay-text"
                  style={{
                    fontSize: `${fontSize * 0.8}px`,
                    color: config.textColor,
                    marginBottom: "3px",
                    display: "flex",
                    justifyContent: "space-between",
                    gap: "8px",
                  }}
                >
                  <span
                    style={{
                      overflow: "hidden",
                      textOverflow: "ellipsis",
                      whiteSpace: "nowrap",
                      flex: 1,
                    }}
                  >
                    {optionText}
                  </span>
                  <span style={{ opacity: 0.8, whiteSpace: "nowrap", flexShrink: 0 }}>
                    {votes} ({Math.round(pct)}%)
                  </span>
                </div>
                <div
                  style={{
                    height: `${barHeight}px`,
                    background: "rgba(255, 255, 255, 0.1)",
                    borderRadius: "6px",
                    overflow: "hidden",
                  }}
                >
                  <div
                    style={{
                      height: "100%",
                      width: `${pct}%`,
                      background: BAR_COLORS[i % BAR_COLORS.length],
                      borderRadius: "6px",
                      transition: "width 0.5s ease-out",
                      minWidth: pct > 0 ? "4px" : "0",
                    }}
                  />
                </div>
              </div>
            );
          })}

          {/* Total votes */}
          <div
            className="overlay-text"
            style={{
              fontSize: `${fontSize * 0.65}px`,
              color: "rgba(255, 255, 255, 0.5)",
              marginTop: "10px",
              textAlign: "center",
            }}
          >
            {totalVotes} vote{totalVotes !== 1 ? "s" : ""}
            {!poll.isActive && " — Poll ended"}
          </div>
        </div>
      </div>
    </OverlayShell>
  );
}
