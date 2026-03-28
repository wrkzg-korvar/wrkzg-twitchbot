import { useEffect, useState } from "react";
import { useOverlaySignalR } from "../../hooks/useOverlaySignalR";
import { useOverlayConfig } from "../../hooks/useOverlayConfig";
import { OverlayShell } from "./OverlayShell";

interface CounterData {
  id: number;
  name: string;
  value: number;
}

interface CounterUpdatedEvent {
  counterId: number;
  name: string;
  value: number;
}

const CounterDefaults: Record<string, string> = {
  fontSize: "64",
  textColor: "#ffffff",
  labelColor: "#8BBF4C",
};

export function CounterOverlay() {
  const config = useOverlayConfig("counter", CounterDefaults);
  const { on, off } = useOverlaySignalR("/hubs/chat");
  const [counter, setCounter] = useState<CounterData | null>(null);
  const [animating, setAnimating] = useState(false);

  // Get counter ID from URL query params OR config
  const params = new URLSearchParams(window.location.search);
  const counterIdFromUrl = params.get("id");

  // Fetch counter on mount — try URL param first, then fetch all and use first
  useEffect(() => {
    if (counterIdFromUrl) {
      fetch(`/api/overlays/data/counter/${encodeURIComponent(counterIdFromUrl)}`)
        .then((res) => (res.ok ? res.json() : null))
        .then((data: CounterData | null) => {
          if (data) {
            setCounter(data);
          }
        })
        .catch(() => {});
    } else {
      // No ID specified — fetch all counters and use the first one
      fetch("/api/overlays/data/counters")
        .then((res) => (res.ok ? res.json() : []))
        .then((data: CounterData[]) => {
          if (data.length > 0) {
            setCounter(data[0]);
          }
        })
        .catch(() => {});
    }
  }, [counterIdFromUrl]);

  // Listen for counter updates
  useEffect(() => {
    on<CounterUpdatedEvent>("CounterUpdated", (data) => {
      // Update if this is the counter we're displaying
      if (counter && data.counterId === counter.id) {
        setCounter({ id: data.counterId, name: data.name, value: data.value });
        setAnimating(true);
        setTimeout(() => setAnimating(false), 400);
      }

      // Also update if no specific counter is set (show any update)
      if (!counter) {
        setCounter({ id: data.counterId, name: data.name, value: data.value });
      }
    });

    return () => {
      off("CounterUpdated");
    };
  }, [on, off, counter]);

  if (!counter) {
    return (
      <OverlayShell>
        <div
          className="overlay-text"
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            height: "100%",
            fontSize: "24px",
            color: "#666",
          }}
        >
          No counter loaded
        </div>
      </OverlayShell>
    );
  }

  const fontSize = Number(config.fontSize) || 64;

  return (
    <OverlayShell>
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          height: "100%",
        }}
      >
        <div className="overlay-text" style={{ textAlign: "center" }}>
          <span
            style={{
              fontSize: `${fontSize * 0.5}px`,
              color: config.labelColor,
              fontWeight: 600,
              marginRight: "16px",
            }}
          >
            {counter.name}:
          </span>
          <span
            style={{
              fontSize: `${fontSize}px`,
              color: config.textColor,
              fontWeight: 800,
              display: "inline-block",
              transition: "transform 0.3s ease",
              transform: animating ? "scale(1.2)" : "scale(1)",
            }}
          >
            {counter.value}
          </span>
        </div>
      </div>
    </OverlayShell>
  );
}
