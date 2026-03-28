import { useEffect, useState } from "react";
import { useOverlaySignalR } from "../../hooks/useOverlaySignalR";
import { useOverlayConfig } from "../../hooks/useOverlayConfig";
import { OverlayShell } from "./OverlayShell";
import { Confetti } from "./shared/Confetti";

type OverlayState =
  | { kind: "idle" }
  | { kind: "active"; title: string; keyword: string }
  | { kind: "drawing"; winnerName: string }
  | { kind: "accepted"; winnerName: string };

const RaffleDefaults: Record<string, string> = {
  fontSize: "48",
  textColor: "#ffffff",
  accentColor: "#FFD700",
};

export function RaffleOverlay() {
  const config = useOverlayConfig("raffle", RaffleDefaults);
  const { on, off } = useOverlaySignalR("/hubs/chat");
  const [state, setState] = useState<OverlayState>({ kind: "idle" });
  const [showConfetti, setShowConfetti] = useState(false);

  useEffect(() => {
    on<{ id: number; title: string; keyword: string }>("RaffleCreated", (data) => {
      setState({
        kind: "active",
        title: data.title,
        keyword: data.keyword ?? "!join",
      });
    });

    on<{ raffleId: number; winnerName: string; drawNumber: number }>("RaffleDrawPending", (data) => {
      setState({ kind: "drawing", winnerName: data.winnerName });
      setShowConfetti(true);
      setTimeout(() => setShowConfetti(false), 4000);
    });

    on<{ raffleId: number; winnerName: string; drawNumber: number }>("RaffleWinnerAccepted", (data) => {
      setState({ kind: "accepted", winnerName: data.winnerName });
      setShowConfetti(true);
      setTimeout(() => setShowConfetti(false), 4000);
      setTimeout(() => setState({ kind: "idle" }), 8000);
    });

    on<{ raffleId: number }>("RaffleEnded", () => {
      setTimeout(() => setState({ kind: "idle" }), 3000);
    });

    on<{ raffleId: number }>("RaffleCancelled", () => {
      setState({ kind: "idle" });
    });

    return () => {
      off("RaffleCreated");
      off("RaffleDrawPending");
      off("RaffleWinnerAccepted");
      off("RaffleEnded");
      off("RaffleCancelled");
    };
  }, [on, off]);

  if (state.kind === "idle") return null;

  const fontSize = Number(config.fontSize) || 48;

  return (
    <OverlayShell>
      {showConfetti && <Confetti />}
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          height: "100%",
        }}
      >
        {state.kind === "active" && (
          <div
            className="overlay-text"
            style={{
              textAlign: "center",
              animation: "slideDown 0.5s ease-out forwards",
              animationFillMode: "forwards",
              opacity: 1,
            }}
          >
            <div
              style={{
                fontSize: `${fontSize * 0.5}px`,
                color: config.textColor,
                fontWeight: 600,
                marginBottom: "12px",
              }}
            >
              {state.title}
            </div>
            <div
              style={{
                fontSize: `${fontSize * 0.4}px`,
                color: config.textColor,
                opacity: 0.8,
              }}
            >
              Type{" "}
              <span style={{ color: config.accentColor, fontWeight: 700 }}>
                {state.keyword}
              </span>{" "}
              to enter!
            </div>
          </div>
        )}

        {(state.kind === "drawing" || state.kind === "accepted") && (
          <div
            style={{
              textAlign: "center",
              animation: "bounceIn 0.6s ease-out",
            }}
          >
            <div
              className="overlay-text"
              style={{
                fontSize: `${fontSize * 0.4}px`,
                color: config.textColor,
                marginBottom: "16px",
              }}
            >
              {state.kind === "drawing" ? "And the winner is..." : "Winner!"}
            </div>
            <div
              className="overlay-text"
              style={{
                fontSize: `${fontSize}px`,
                color: config.accentColor,
                fontWeight: 800,
              }}
            >
              {state.winnerName}
            </div>
          </div>
        )}
      </div>
    </OverlayShell>
  );
}
