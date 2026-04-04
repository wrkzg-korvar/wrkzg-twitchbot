import { useEffect, useState, useRef, useCallback } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Gift, Clock, Trophy, RotateCcw, Users, Check, X,
  CircleDot, MessageSquare, AlertCircle,
} from "lucide-react";
import { rafflesApi } from "../../../api/raffles";
import { statusApi } from "../../../api/status";
import { showToast } from "../../../hooks/useToast";
import { ConfirmDialog } from "../../ui/ConfirmDialog";
import type { RaffleDto, RaffleDrawDto } from "../../../types/raffles";
import type { ChatMsg } from "../../../types/status";

// ---- Shared: Draw History List ----

export function DrawHistoryList({ draws }: { draws: RaffleDrawDto[] }) {
  if (draws.length === 0) return null;

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-elevated)] p-3">
      <span className="text-xs font-medium text-[var(--color-text-muted)] mb-2 block">Draw History</span>
      <div className="space-y-1">
        {draws.map((draw) => (
          <div key={draw.drawNumber} className="flex items-center gap-2 text-xs">
            {draw.isAccepted ? (
              <Check className="h-3 w-3 text-green-400 shrink-0" />
            ) : draw.redrawReason ? (
              <X className="h-3 w-3 text-red-400 shrink-0" />
            ) : (
              <CircleDot className="h-3 w-3 text-yellow-400 shrink-0" />
            )}
            <span className="text-[var(--color-text)]">
              #{draw.drawNumber} {draw.username}
            </span>
            {draw.redrawReason && (
              <span className="text-[var(--color-text-muted)]">-- {draw.redrawReason}</span>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

// ---- Active Raffle (pre-draw) ----

interface ActiveRaffleProps {
  raffle: RaffleDto;
  participants: string[];
}

export function ActiveRafflePanel({ raffle, participants }: ActiveRaffleProps) {
  const queryClient = useQueryClient();
  const { remaining, display } = useCountdown(raffle.entriesCloseAt || null);
  const [confirmCancel, setConfirmCancel] = useState(false);

  const drawMutation = useMutation({
    mutationFn: rafflesApi.draw,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["raffleActive"] });
      queryClient.invalidateQueries({ queryKey: ["raffleHistory"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  const cancelMutation = useMutation({
    mutationFn: rafflesApi.cancel,
    onSuccess: () => {
      showToast("info", "Raffle cancelled");
      queryClient.invalidateQueries({ queryKey: ["raffleActive"] });
      queryClient.invalidateQueries({ queryKey: ["raffleHistory"] });
    },
    onError: (err: Error) => showToast("error", err.message),
  });

  return (
    <div className="rounded-lg border border-[var(--color-brand)] bg-[var(--color-surface)] p-4">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <span className="h-2 w-2 rounded-full bg-red-500 animate-pulse" />
          <h2 className="text-sm font-semibold text-[var(--color-text)]">Active Raffle</h2>
        </div>
        <div className="flex items-center gap-2 text-xs text-[var(--color-text-muted)]">
          {raffle.entriesCloseAt && (
            <>
              <Clock className="h-3.5 w-3.5" />
              <span className={remaining <= 10 ? "text-red-400 font-bold" : ""}>{display}</span>
            </>
          )}
          <span className="ml-2 flex items-center gap-1">
            <Users className="h-3.5 w-3.5" />
            {participants.length} entries
          </span>
        </div>
      </div>

      <h3 className="text-lg font-semibold text-[var(--color-text)] mb-2">{raffle.title}</h3>

      <div className="flex items-center gap-2 mb-4">
        <span className="inline-block rounded px-2 py-0.5 text-xs bg-[var(--color-brand-subtle)] text-[var(--color-brand-text)]">
          {raffle.keyword || "!join"}
        </span>
        {raffle.maxEntries > 0 && (
          <span className="text-xs text-[var(--color-text-muted)]">
            Max: {raffle.maxEntries}
          </span>
        )}
      </div>

      {participants.length > 0 && (
        <div className="mb-4 max-h-40 overflow-y-auto rounded-lg border border-[var(--color-border)] bg-[var(--color-elevated)] p-3">
          <div className="flex flex-wrap gap-1.5">
            {participants.map((name) => (
              <span
                key={name}
                className="inline-block rounded-full bg-[var(--color-surface)] px-2 py-0.5 text-xs text-[var(--color-text)]"
              >
                {name}
              </span>
            ))}
          </div>
        </div>
      )}

      <div className="flex gap-2">
        <button
          onClick={() => drawMutation.mutate()}
          disabled={drawMutation.isPending || participants.length === 0}
          className="flex items-center gap-1.5 rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white hover:bg-green-500 disabled:opacity-40 transition-colors"
        >
          <Gift className="h-4 w-4" />
          {drawMutation.isPending ? "Drawing..." : "Draw Winner"}
        </button>
        <button
          onClick={() => setConfirmCancel(true)}
          disabled={cancelMutation.isPending}
          className="rounded-lg bg-[var(--color-elevated)] px-3 py-1.5 text-xs font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] disabled:opacity-40 transition-colors"
        >
          Cancel Raffle
        </button>
      </div>

      <ConfirmDialog
        open={confirmCancel}
        title="Cancel Raffle"
        message="Cancel this raffle? All entries will be discarded."
        onConfirm={() => { cancelMutation.mutate(); setConfirmCancel(false); }}
        onCancel={() => setConfirmCancel(false)}
      />

      {drawMutation.isError && (
        <p className="mt-2 text-xs text-red-400">{(drawMutation.error as Error).message}</p>
      )}
    </div>
  );
}

// ---- Raffle Verification (pending winner) ----

interface RaffleVerificationProps {
  raffle: RaffleDto;
  participants: string[];
  onAccept: () => Promise<void>;
  onRedraw: () => Promise<void>;
  onCancel?: () => Promise<void>;
}

export function RaffleVerification({ raffle, participants, onAccept, onRedraw, onCancel }: RaffleVerificationProps) {
  const queryClient = useQueryClient();
  const [messages, setMessages] = useState<ChatMsg[]>([]);
  const [loadingMessages, setLoadingMessages] = useState(true);
  const [acceptPending, setAcceptPending] = useState(false);
  const [redrawPending, setRedrawPending] = useState(false);
  const [cancelPending, setCancelPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const chatEndRef = useRef<HTMLDivElement>(null);

  const pendingWinner = raffle.pendingWinner;
  const draws = raffle.draws || [];
  const currentDraw = draws.length > 0 ? draws[draws.length - 1] : null;
  const actionPending = acceptPending || redrawPending || cancelPending;

  const pendingWinnerRef = useRef(pendingWinner);
  pendingWinnerRef.current = pendingWinner;

  const fetchMessages = useCallback(() => {
    const pw = pendingWinnerRef.current;
    if (!pw?.twitchId) return;

    statusApi.getRecentChatForUser(pw.twitchId)
      .then((data) => setMessages(data))
      .catch(() => setMessages([]))
      .finally(() => setLoadingMessages(false));
  }, []);

  useEffect(() => {
    if (!pendingWinner) return;
    setLoadingMessages(true);
    fetchMessages();
    const interval = setInterval(fetchMessages, 3000);
    return () => clearInterval(interval);
  }, [pendingWinner, fetchMessages]);

  // Auto-scroll chat to bottom when new messages arrive
  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  const handleAcceptClick = async () => {
    setAcceptPending(true);
    setError(null);
    try {
      await onAccept();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Accept failed");
    } finally {
      setAcceptPending(false);
    }
  };

  const handleRedrawClick = async () => {
    setRedrawPending(true);
    setError(null);
    try {
      await onRedraw();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Redraw failed");
    } finally {
      setRedrawPending(false);
    }
  };

  if (!pendingWinner) return null;

  return (
    <div className="rounded-lg border border-yellow-500 bg-[var(--color-surface)] p-4">
      <div className="flex items-center gap-2 mb-4">
        <Trophy className="h-5 w-5 text-yellow-400" />
        <h2 className="text-sm font-semibold text-[var(--color-text)]">Verify Winner</h2>
        {currentDraw && (
          <span className="ml-auto text-xs text-[var(--color-text-muted)]">
            Draw #{currentDraw.drawNumber}
          </span>
        )}
      </div>

      <div className="mb-4 text-center">
        <p className="text-3xl font-bold text-yellow-400">{pendingWinner.displayName}</p>
        <p className="text-xs text-[var(--color-text-muted)] mt-1">
          Is this user present in chat?
        </p>
      </div>

      <div className="mb-4 rounded-lg border border-[var(--color-border)] bg-[var(--color-elevated)] p-3">
        <div className="flex items-center gap-1.5 mb-2">
          <MessageSquare className="h-3.5 w-3.5 text-[var(--color-text-muted)]" />
          <span className="text-xs font-medium text-[var(--color-text-muted)]">Recent Messages</span>
          <span className="ml-1 flex items-center gap-1">
            <span className="h-1.5 w-1.5 rounded-full bg-green-400 animate-pulse" />
            <span className="text-[10px] text-green-400">Live</span>
          </span>
        </div>
        {loadingMessages ? (
          <p className="text-xs text-[var(--color-text-muted)]">Loading messages...</p>
        ) : messages.length === 0 ? (
          <p className="text-xs text-[var(--color-text-muted)]">No recent messages found</p>
        ) : (
          <div className="max-h-40 overflow-y-auto space-y-1">
            {messages.map((msg, i) => (
              <div key={i} className="flex gap-2 text-xs">
                <span className="shrink-0 text-[var(--color-text-muted)]">
                  {new Date(msg.timestamp).toLocaleTimeString()}
                </span>
                <span className="font-semibold text-[var(--color-brand-text)] shrink-0">{msg.displayName}:</span>
                <span className="text-[var(--color-text)]">{msg.content}</span>
              </div>
            ))}
            <div ref={chatEndRef} />
          </div>
        )}
      </div>

      <div className="flex gap-2 mb-4">
        <button
          onClick={handleAcceptClick}
          disabled={actionPending}
          className="flex items-center gap-1.5 rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white hover:bg-green-500 disabled:opacity-40 transition-colors"
        >
          <Check className="h-4 w-4" />
          {acceptPending ? "Accepting..." : "Accept Winner"}
        </button>
        <button
          onClick={handleRedrawClick}
          disabled={actionPending}
          className="flex items-center gap-1.5 rounded-lg bg-[var(--color-elevated)] px-4 py-2 text-sm font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] disabled:opacity-40 transition-colors"
        >
          <RotateCcw className="h-4 w-4" />
          {redrawPending ? "Redrawing..." : "Redraw"}
        </button>
        {onCancel && (
          <button
            onClick={async () => {
              setCancelPending(true);
              setError(null);
              try {
                await onCancel();
                showToast("info", "Raffle cancelled");
                queryClient.invalidateQueries({ queryKey: ["raffleActive"] });
                queryClient.invalidateQueries({ queryKey: ["raffleHistory"] });
              } catch (err) {
                setError(err instanceof Error ? err.message : "Cancel failed");
              } finally {
                setCancelPending(false);
              }
            }}
            disabled={actionPending}
            className="flex items-center gap-1.5 rounded-lg border border-red-500/30 px-3 py-2 text-sm font-medium text-red-400 hover:bg-red-500/10 disabled:opacity-40 transition-colors ml-auto"
          >
            <X className="h-4 w-4" />
            {cancelPending ? "Cancelling..." : "Cancel Raffle"}
          </button>
        )}
      </div>

      {error && (
        <div className="flex items-center gap-1.5 text-xs text-red-400">
          <AlertCircle className="h-3.5 w-3.5 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <DrawHistoryList draws={draws} />

      {participants.length > 0 && (
        <div className="mt-4 text-xs text-[var(--color-text-muted)]">
          <Users className="inline h-3 w-3 mr-1" />
          {participants.length} participants in pool
        </div>
      )}
    </div>
  );
}

// ---- Raffle Post-Accept (draw more / end) ----

interface RafflePostAcceptProps {
  raffle: RaffleDto;
  onDrawAnother: () => Promise<void>;
  onEndRaffle: () => Promise<void>;
}

export function RafflePostAccept({ raffle, onDrawAnother, onEndRaffle }: RafflePostAcceptProps) {
  const [drawPending, setDrawPending] = useState(false);
  const [endPending, setEndPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const draws = raffle.draws || [];
  const acceptedDraws = draws.filter((d) => d.isAccepted);
  const acceptedCount = acceptedDraws.length;
  const totalDrawn = draws.length;
  const remainingEntries = raffle.entryCount - totalDrawn;
  const canDrawMore = remainingEntries > 0;

  const handleDrawClick = async () => {
    setDrawPending(true);
    setError(null);
    try {
      await onDrawAnother();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Draw failed");
    } finally {
      setDrawPending(false);
    }
  };

  const handleEndClick = async () => {
    setEndPending(true);
    setError(null);
    try {
      await onEndRaffle();
    } catch (err) {
      setError(err instanceof Error ? err.message : "End raffle failed");
    } finally {
      setEndPending(false);
    }
  };

  return (
    <div className="rounded-lg border border-green-600 bg-[var(--color-surface)] p-4">
      <div className="flex items-center gap-2 mb-4">
        <Trophy className="h-5 w-5 text-green-400" />
        <h2 className="text-sm font-semibold text-[var(--color-text)]">{raffle.title}</h2>
        <span className="ml-auto text-xs text-[var(--color-text-muted)]">
          {acceptedCount} winner{acceptedCount !== 1 ? "s" : ""} &middot; {remainingEntries} remaining
        </span>
      </div>

      <div className="mb-4 space-y-1.5">
        {acceptedDraws.map((draw) => (
          <div key={draw.drawNumber} className="flex items-center gap-2">
            <Trophy className="h-3.5 w-3.5 text-green-400 shrink-0" />
            <span className="text-sm font-medium text-[var(--color-text)]">{draw.username}</span>
            <span className="text-xs text-[var(--color-text-muted)]">Draw #{draw.drawNumber}</span>
          </div>
        ))}
      </div>

      {draws.some((d) => d.redrawReason) && (
        <div className="mb-4">
          <DrawHistoryList draws={draws} />
        </div>
      )}

      <div className="flex gap-2">
        {canDrawMore && (
          <button
            onClick={handleDrawClick}
            disabled={drawPending || endPending}
            className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
          >
            <Gift className="h-4 w-4" />
            {drawPending ? "Drawing..." : "Draw Another Winner"}
          </button>
        )}
        <button
          onClick={handleEndClick}
          disabled={drawPending || endPending}
          className="flex items-center gap-1.5 rounded-lg bg-[var(--color-elevated)] px-4 py-2 text-sm font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] disabled:opacity-40 transition-colors"
        >
          <X className="h-4 w-4" />
          {endPending ? "Ending..." : "End Raffle"}
        </button>
      </div>

      {error && (
        <div className="mt-2 flex items-center gap-1.5 text-xs text-red-400">
          <AlertCircle className="h-3.5 w-3.5 shrink-0" />
          <span>{error}</span>
        </div>
      )}
    </div>
  );
}

// ---- Draw Animation ----

interface DrawAnimationProps {
  participants: string[];
  winner: string;
  onComplete: () => void;
}

export function DrawAnimation({ participants, winner, onComplete }: DrawAnimationProps) {
  const [currentName, setCurrentName] = useState(participants[0] || "");
  const [revealed, setRevealed] = useState(false);

  useEffect(() => {
    let frame = 0;
    const totalFrames = 50;
    const interval = setInterval(() => {
      frame++;
      if (frame < totalFrames) {
        const randomIndex = Math.floor(Math.random() * participants.length);
        setCurrentName(participants[randomIndex]);
      } else {
        clearInterval(interval);
        setCurrentName(winner);
        setRevealed(true);
      }
    }, 50);

    return () => clearInterval(interval);
  }, [participants, winner]);

  useEffect(() => {
    if (revealed) {
      const timeout = setTimeout(onComplete, 3000);
      return () => clearTimeout(timeout);
    }
  }, [revealed, onComplete]);

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/80"
      onClick={revealed ? onComplete : undefined}
    >
      <div className="flex flex-col items-center gap-4">
        <Trophy
          className={`h-16 w-16 transition-all duration-500 ${
            revealed ? "text-yellow-400 scale-125" : "text-[var(--color-text-muted)]"
          }`}
        />
        <div
          className={`text-center transition-all duration-500 ${
            revealed ? "scale-110" : ""
          }`}
        >
          <p className="text-sm text-[var(--color-text-muted)] mb-2">
            {revealed ? "Winner!" : "Drawing..."}
          </p>
          <p
            className={`text-4xl font-bold transition-all duration-500 ${
              revealed ? "text-yellow-400" : "text-[var(--color-text)]"
            }`}
          >
            {currentName}
          </p>
        </div>
        {revealed && (
          <p className="mt-4 text-xs text-[var(--color-text-muted)]">Click anywhere to close</p>
        )}
      </div>
    </div>
  );
}

// ---- Countdown hook ----

function useCountdown(endsAt: string | null) {
  const [remaining, setRemaining] = useState(0);

  useEffect(() => {
    if (!endsAt) return;

    const update = () => {
      const diff = Math.max(0, Math.floor((new Date(endsAt).getTime() - Date.now()) / 1000));
      setRemaining(diff);
    };

    update();
    const interval = setInterval(update, 1000);
    return () => clearInterval(interval);
  }, [endsAt]);

  const minutes = Math.floor(remaining / 60);
  const seconds = remaining % 60;
  return { remaining, display: `${minutes}:${seconds.toString().padStart(2, "0")}` };
}
