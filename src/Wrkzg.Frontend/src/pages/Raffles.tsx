import { Fragment, useEffect, useState, useRef, useCallback } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useSignalR } from "../hooks/useSignalR";
import { Plus, Gift, Clock, Trophy, ChevronDown, ChevronRight, RotateCcw, Users, Check, X, CircleDot, MessageSquare, AlertCircle } from "lucide-react";

// ─── Types ───────────────────────────────────────────────

interface RaffleEntryDto {
  username: string;
  twitchId: string;
  ticketCount: number;
}

interface RaffleWinnerDto {
  displayName: string;
  twitchId: string;
}

interface RaffleDrawDto {
  drawNumber: number;
  username: string;
  isAccepted: boolean;
  redrawReason: string | null;
  drawnAt: string;
}

interface RaffleDto {
  id: number;
  title: string;
  keyword: string;
  isOpen: boolean;
  durationSeconds: number;
  entriesCloseAt: string;
  maxEntries: number;
  createdBy: string;
  createdAt: string;
  closedAt: string;
  endReason: string;
  winner: RaffleWinnerDto | null;
  pendingWinner: { displayName: string; twitchId: string } | null;
  draws: RaffleDrawDto[];
  entries: RaffleEntryDto[];
  entryCount: number;
}

interface RaffleHistoryItem {
  id: number;
  title: string;
  keyword: string;
  isOpen: boolean;
  createdAt: string;
  closedAt: string;
  endReason: string;
  winnerName: string;
  entryCount: number;
}

interface RaffleTemplate {
  key: string;
  default: string;
  description: string;
  variables: string[];
  current: string | null;
}

// ─── API ─────────────────────────────────────────────────

async function fetchActiveRaffle(): Promise<RaffleDto | null> {
  const res = await fetch("/api/raffles/active");
  if (res.status === 404) return null;
  if (!res.ok) throw new Error("Failed to fetch active raffle");
  return res.json();
}

async function fetchRaffleHistory(): Promise<RaffleHistoryItem[]> {
  const res = await fetch("/api/raffles/history");
  if (!res.ok) throw new Error("Failed to fetch raffle history");
  return res.json();
}

async function fetchRaffleTemplates(): Promise<RaffleTemplate[]> {
  const res = await fetch("/api/raffles/templates");
  if (!res.ok) throw new Error("Failed to fetch templates");
  return res.json();
}

async function fetchRaffleById(id: number): Promise<RaffleDto> {
  const res = await fetch(`/api/raffles/${id}`);
  if (!res.ok) throw new Error("Failed to fetch raffle details");
  return res.json();
}

// ─── Countdown Hook ──────────────────────────────────────

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

// ─── Draw History List (shared) ──────────────────────────

function DrawHistoryList({ draws }: { draws: RaffleDrawDto[] }) {
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
              <span className="text-[var(--color-text-muted)]">— {draw.redrawReason}</span>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

// ─── Component ───────────────────────────────────────────

export function Raffles() {
  const queryClient = useQueryClient();
  const { isConnected: signalRConnected, on, off } = useSignalR("/hubs/chat");

  const { data: activeRaffle } = useQuery<RaffleDto | null>({
    queryKey: ["raffleActive"],
    queryFn: fetchActiveRaffle,
    refetchInterval: 10_000,
    retry: false,
  });

  const { data: history } = useQuery<RaffleHistoryItem[]>({
    queryKey: ["raffleHistory"],
    queryFn: fetchRaffleHistory,
  });

  // Live raffle state for SignalR updates
  const [liveRaffle, setLiveRaffle] = useState<RaffleDto | null>(null);
  const [participants, setParticipants] = useState<string[]>([]);
  const [drawnWinner, setDrawnWinner] = useState<string | null>(null);
  const [showDrawAnimation, setShowDrawAnimation] = useState(false);

  // Sync REST data into live state
  useEffect(() => {
    if (activeRaffle) {
      setLiveRaffle(activeRaffle);
      setParticipants(activeRaffle.entries.map((e) => e.username));
    } else {
      setLiveRaffle(null);
      setParticipants([]);
    }
  }, [activeRaffle]);

  // SignalR events
  useEffect(() => {
    if (!signalRConnected) return;

    on("RaffleCreated", () => {
      queryClient.invalidateQueries({ queryKey: ["raffleActive"] });
    });

    on<{ username: string }>("RaffleEntry", ({ username }) => {
      setParticipants((prev) => {
        if (prev.includes(username)) return prev;
        return [...prev, username];
      });
      setLiveRaffle((prev) => {
        if (!prev) return prev;
        return { ...prev, entryCount: prev.entryCount + 1 };
      });
    });

    on<{ winnerName: string; twitchId: string; drawNumber: number }>("RaffleDrawPending", ({ winnerName, twitchId, drawNumber }) => {
      setDrawnWinner(winnerName);
      setShowDrawAnimation(true);
      setLiveRaffle((prev) => {
        if (!prev) return prev;
        const newDraw: RaffleDrawDto = {
          drawNumber,
          username: winnerName,
          isAccepted: false,
          redrawReason: null,
          drawnAt: new Date().toISOString(),
        };
        return {
          ...prev,
          pendingWinner: { displayName: winnerName, twitchId },
          draws: [...(prev.draws || []), newDraw],
        };
      });
    });

    on<{ raffleId: number; winnerName: string; drawNumber: number }>("RaffleWinnerAccepted", ({ drawNumber }) => {
      setLiveRaffle((prev) => {
        if (!prev) return prev;
        const updatedDraws = (prev.draws || []).map((d) =>
          d.drawNumber === drawNumber ? { ...d, isAccepted: true } : d
        );
        return {
          ...prev,
          pendingWinner: null,
          draws: updatedDraws,
        };
      });
    });

    on<{ raffleId: number }>("RaffleEnded", () => {
      setLiveRaffle(null);
      setParticipants([]);
      queryClient.invalidateQueries({ queryKey: ["raffleActive"] });
      queryClient.invalidateQueries({ queryKey: ["raffleHistory"] });
    });

    on<{ winnerName: string }>("RaffleDrawn", () => {
      setLiveRaffle(null);
      setParticipants([]);
      queryClient.invalidateQueries({ queryKey: ["raffleActive"] });
      queryClient.invalidateQueries({ queryKey: ["raffleHistory"] });
    });

    on("RaffleCancelled", () => {
      setLiveRaffle(null);
      setParticipants([]);
      queryClient.invalidateQueries({ queryKey: ["raffleActive"] });
      queryClient.invalidateQueries({ queryKey: ["raffleHistory"] });
    });

    return () => {
      off("RaffleCreated");
      off("RaffleEntry");
      off("RaffleDrawPending");
      off("RaffleWinnerAccepted");
      off("RaffleEnded");
      off("RaffleDrawn");
      off("RaffleCancelled");
    };
  }, [signalRConnected, on, off, queryClient]);

  const handleAnimationComplete = () => {
    setShowDrawAnimation(false);
    setDrawnWinner(null);
  };

  const handleAccept = async () => {
    const res = await fetch("/api/raffles/accept", { method: "POST" });
    if (!res.ok) {
      const data = await res.json().catch(() => ({ error: "Unknown error" }));
      throw new Error(data.error || "Accept failed");
    }
  };

  const handleRedraw = async () => {
    const res = await fetch("/api/raffles/redraw", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ reason: "User not present" }),
    });
    if (!res.ok) {
      const data = await res.json().catch(() => ({ error: "Unknown error" }));
      throw new Error(data.error || "Redraw failed");
    }
  };

  const handleDrawAnother = async () => {
    const res = await fetch("/api/raffles/draw", { method: "POST" });
    if (!res.ok) {
      const data = await res.json().catch(() => ({ error: "Unknown error" }));
      throw new Error(data.error || "Draw failed");
    }
  };

  const handleEndRaffle = async () => {
    const res = await fetch("/api/raffles/end", { method: "POST" });
    if (!res.ok) {
      const data = await res.json().catch(() => ({ error: "Unknown error" }));
      throw new Error(data.error || "End raffle failed");
    }
  };

  const hasAcceptedDraws = liveRaffle?.draws?.some((d) => d.isAccepted) ?? false;

  return (
    <div className="flex h-full flex-col gap-6 overflow-y-auto p-6">
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text)]">Raffles</h1>
        <p className="mt-1 text-sm text-[var(--color-text-muted)]">
          Create raffles, draw winners, and browse history.
        </p>
      </div>

      <CreateRaffleForm />

      {liveRaffle && !liveRaffle.pendingWinner && !hasAcceptedDraws && (
        <ActiveRaffle raffle={liveRaffle} participants={participants} />
      )}

      {liveRaffle && liveRaffle.pendingWinner && (
        <RaffleVerification
          raffle={liveRaffle}
          participants={participants}
          onAccept={handleAccept}
          onRedraw={handleRedraw}
        />
      )}

      {liveRaffle && !liveRaffle.pendingWinner && hasAcceptedDraws && (
        <RafflePostAccept
          raffle={liveRaffle}
          onDrawAnother={handleDrawAnother}
          onEndRaffle={handleEndRaffle}
        />
      )}

      {showDrawAnimation && drawnWinner && participants.length > 0 && (
        <DrawAnimation
          participants={participants}
          winner={drawnWinner}
          onComplete={handleAnimationComplete}
        />
      )}

      <AnnouncementTemplates />

      <RaffleHistory items={history ?? []} />
    </div>
  );
}

// ─── Create Raffle Form ─────────────────────────────────

function CreateRaffleForm() {
  const queryClient = useQueryClient();
  const [title, setTitle] = useState("");
  const [keyword, setKeyword] = useState("");
  const [duration, setDuration] = useState<number | "">("");
  const [maxEntries, setMaxEntries] = useState<number | "">("");

  const createMutation = useMutation({
    mutationFn: async () => {
      const res = await fetch("/api/raffles", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          title,
          keyword: keyword.trim() || undefined,
          durationSeconds: duration || undefined,
          maxEntries: maxEntries || undefined,
        }),
      });
      if (!res.ok) {
        const data = await res.json();
        throw new Error(data.error || "Failed to create raffle");
      }
    },
    onSuccess: () => {
      setTitle("");
      setKeyword("");
      setDuration("");
      setMaxEntries("");
      queryClient.invalidateQueries({ queryKey: ["raffleActive"] });
    },
  });

  const canCreate = title.trim().length > 0;

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
      <h2 className="text-sm font-semibold text-[var(--color-text)] mb-4">Create Raffle</h2>

      <div className="space-y-3">
        <input
          type="text"
          placeholder="Raffle title..."
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          maxLength={200}
          className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
        />

        <input
          type="text"
          placeholder="Leave empty for !join"
          value={keyword}
          onChange={(e) => setKeyword(e.target.value)}
          maxLength={50}
          className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
        />

        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <label className="text-xs text-[var(--color-text-muted)]">Duration (s):</label>
            <input
              type="number"
              placeholder="Optional"
              value={duration}
              onChange={(e) => setDuration(e.target.value ? Number(e.target.value) : "")}
              min={0}
              className="w-24 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-1.5 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
            />
          </div>
          <div className="flex items-center gap-2">
            <label className="text-xs text-[var(--color-text-muted)]">Max entries:</label>
            <input
              type="number"
              placeholder="Optional"
              value={maxEntries}
              onChange={(e) => setMaxEntries(e.target.value ? Number(e.target.value) : "")}
              min={0}
              className="w-24 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-1.5 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none"
            />
          </div>
        </div>

        <div className="flex gap-2">
          <button
            onClick={() => createMutation.mutate()}
            disabled={!canCreate || createMutation.isPending}
            className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
          >
            <Plus className="h-4 w-4" />
            {createMutation.isPending ? "Creating..." : "Start Raffle"}
          </button>
        </div>

        {createMutation.isError && (
          <p className="text-xs text-red-400">{(createMutation.error as Error).message}</p>
        )}
      </div>
    </div>
  );
}

// ─── Active Raffle ───────────────────────────────────────

function ActiveRaffle({ raffle, participants }: { raffle: RaffleDto; participants: string[] }) {
  const queryClient = useQueryClient();
  const { remaining, display } = useCountdown(raffle.entriesCloseAt || null);

  const drawMutation = useMutation({
    mutationFn: async () => {
      const res = await fetch("/api/raffles/draw", { method: "POST" });
      if (!res.ok) {
        const data = await res.json();
        throw new Error(data.error || "Failed to draw winner");
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["raffleActive"] });
      queryClient.invalidateQueries({ queryKey: ["raffleHistory"] });
    },
  });

  const cancelMutation = useMutation({
    mutationFn: async () => {
      await fetch("/api/raffles/cancel", { method: "POST" });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["raffleActive"] });
      queryClient.invalidateQueries({ queryKey: ["raffleHistory"] });
    },
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
          onClick={() => cancelMutation.mutate()}
          disabled={cancelMutation.isPending}
          className="rounded-lg bg-[var(--color-elevated)] px-3 py-1.5 text-xs font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] disabled:opacity-40 transition-colors"
        >
          Cancel Raffle
        </button>
      </div>

      {drawMutation.isError && (
        <p className="mt-2 text-xs text-red-400">{(drawMutation.error as Error).message}</p>
      )}
    </div>
  );
}

// ─── Draw Animation ──────────────────────────────────────

function DrawAnimation({
  participants,
  winner,
  onComplete,
}: {
  participants: string[];
  winner: string;
  onComplete: () => void;
}) {
  const [currentName, setCurrentName] = useState(participants[0] || "");
  const [revealed, setRevealed] = useState(false);

  useEffect(() => {
    let frame = 0;
    const totalFrames = 50; // ~2.5 seconds at 50ms intervals
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

// ─── Raffle Verification ─────────────────────────────────

interface ChatMessageDto {
  timestamp: string;
  content: string;
}

function RaffleVerification({
  raffle,
  participants,
  onAccept,
  onRedraw,
}: {
  raffle: RaffleDto;
  participants: string[];
  onAccept: () => Promise<void>;
  onRedraw: () => Promise<void>;
}) {
  const [messages, setMessages] = useState<ChatMessageDto[]>([]);
  const [loadingMessages, setLoadingMessages] = useState(true);
  const [acceptPending, setAcceptPending] = useState(false);
  const [redrawPending, setRedrawPending] = useState(false);

  const pendingWinner = raffle.pendingWinner;
  const draws = raffle.draws || [];
  const [error, setError] = useState<string | null>(null);
  const currentDraw = draws.length > 0 ? draws[draws.length - 1] : null;
  const actionPending = acceptPending || redrawPending;

  // Fetch chat messages initially and poll every 3 seconds
  const pendingWinnerRef = useRef(pendingWinner);
  pendingWinnerRef.current = pendingWinner;

  const fetchMessages = useCallback(() => {
    const pw = pendingWinnerRef.current;
    if (!pw?.twitchId) return;

    fetch(`/api/chat/recent?userId=${encodeURIComponent(pw.twitchId)}`)
      .then((res) => {
        if (!res.ok) return [];
        return res.json();
      })
      .then((data: ChatMessageDto[]) => setMessages(data))
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

      {/* Recent chat messages with live polling indicator */}
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
          <div className="max-h-32 overflow-y-auto space-y-1">
            {messages.map((msg, i) => (
              <div key={i} className="flex gap-2 text-xs">
                <span className="shrink-0 text-[var(--color-text-muted)]">
                  {new Date(msg.timestamp).toLocaleTimeString()}
                </span>
                <span className="text-[var(--color-text)]">{msg.content}</span>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Action buttons */}
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
      </div>

      {/* Error feedback */}
      {error && (
        <div className="flex items-center gap-1.5 text-xs text-red-400">
          <AlertCircle className="h-3.5 w-3.5 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {/* Draw history */}
      <DrawHistoryList draws={draws} />

      {/* Participants reference */}
      {participants.length > 0 && (
        <div className="mt-4 text-xs text-[var(--color-text-muted)]">
          <Users className="inline h-3 w-3 mr-1" />
          {participants.length} participants in pool
        </div>
      )}
    </div>
  );
}

// ─── Raffle Post-Accept ──────────────────────────────────

function RafflePostAccept({
  raffle,
  onDrawAnother,
  onEndRaffle,
}: {
  raffle: RaffleDto;
  onDrawAnother: () => Promise<void>;
  onEndRaffle: () => Promise<void>;
}) {
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
      {/* Summary header */}
      <div className="flex items-center gap-2 mb-4">
        <Trophy className="h-5 w-5 text-green-400" />
        <h2 className="text-sm font-semibold text-[var(--color-text)]">{raffle.title}</h2>
        <span className="ml-auto text-xs text-[var(--color-text-muted)]">
          {acceptedCount} winner{acceptedCount !== 1 ? "s" : ""} &middot; {remainingEntries} remaining
        </span>
      </div>

      {/* Accepted winners */}
      <div className="mb-4 space-y-1.5">
        {acceptedDraws.map((draw) => (
          <div key={draw.drawNumber} className="flex items-center gap-2">
            <Trophy className="h-3.5 w-3.5 text-green-400 shrink-0" />
            <span className="text-sm font-medium text-[var(--color-text)]">{draw.username}</span>
            <span className="text-xs text-[var(--color-text-muted)]">Draw #{draw.drawNumber}</span>
          </div>
        ))}
      </div>

      {/* Draw history (if redraws happened) */}
      {draws.some((d) => d.redrawReason) && (
        <div className="mb-4">
          <DrawHistoryList draws={draws} />
        </div>
      )}

      {/* Action buttons */}
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

      {/* Error feedback */}
      {error && (
        <div className="mt-2 flex items-center gap-1.5 text-xs text-red-400">
          <AlertCircle className="h-3.5 w-3.5 shrink-0" />
          <span>{error}</span>
        </div>
      )}
    </div>
  );
}

// ─── Announcement Templates ──────────────────────────────

function AnnouncementTemplates() {
  const queryClient = useQueryClient();
  const [expanded, setExpanded] = useState(false);

  const { data: templates } = useQuery<RaffleTemplate[]>({
    queryKey: ["raffleTemplates"],
    queryFn: fetchRaffleTemplates,
    enabled: expanded,
  });

  const [edits, setEdits] = useState<Record<string, string>>({});

  // Sync edits when templates load
  useEffect(() => {
    if (templates) {
      const initial: Record<string, string> = {};
      for (const t of templates) {
        initial[t.key] = t.current ?? "";
      }
      setEdits(initial);
    }
  }, [templates]);

  const saveMutation = useMutation({
    mutationFn: async ({ key, value }: { key: string; value: string }) => {
      const res = await fetch("/api/settings", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ [key]: value }),
      });
      if (!res.ok) throw new Error("Failed to save template");
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["raffleTemplates"] });
    },
  });

  const resetMutation = useMutation({
    mutationFn: async (key: string) => {
      const res = await fetch(`/api/raffles/templates/reset/${encodeURIComponent(key)}`, {
        method: "POST",
      });
      if (!res.ok) throw new Error("Failed to reset template");
    },
    onSuccess: (_data, key) => {
      setEdits((prev) => ({ ...prev, [key]: "" }));
      queryClient.invalidateQueries({ queryKey: ["raffleTemplates"] });
    },
  });

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)]">
      <button
        onClick={() => setExpanded(!expanded)}
        className="flex w-full items-center justify-between px-4 py-3 text-left"
      >
        <h2 className="text-sm font-semibold text-[var(--color-text)]">Announcement Templates</h2>
        {expanded ? (
          <ChevronDown className="h-4 w-4 text-[var(--color-text-muted)]" />
        ) : (
          <ChevronRight className="h-4 w-4 text-[var(--color-text-muted)]" />
        )}
      </button>

      {expanded && (
        <div className="border-t border-[var(--color-border)] p-4 space-y-4">
          {!templates ? (
            <p className="text-sm text-[var(--color-text-muted)]">Loading...</p>
          ) : (
            templates.map((t) => {
              const editValue = edits[t.key] ?? "";
              const isCustom = t.current !== null && t.current.length > 0;
              const hasUnsavedChanges = editValue !== (t.current ?? "");

              return (
                <div key={t.key} className="space-y-1.5">
                  <div className="flex items-center justify-between">
                    <label className="text-xs font-medium text-[var(--color-text)]">
                      {t.description}
                      {isCustom && (
                        <span className="ml-2 inline-block rounded px-1.5 py-0.5 text-[10px] bg-[var(--color-brand-subtle)] text-[var(--color-brand-text)]">
                          custom
                        </span>
                      )}
                    </label>
                    <code className="text-[10px] text-[var(--color-text-muted)]">{t.key}</code>
                  </div>

                  <textarea
                    value={editValue}
                    onChange={(e) => setEdits((prev) => ({ ...prev, [t.key]: e.target.value }))}
                    placeholder={t.default}
                    rows={2}
                    className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] placeholder-[var(--color-text-muted)] focus:border-[var(--color-brand)] focus:outline-none font-mono"
                  />

                  <div className="flex items-center justify-between">
                    <span className="text-[10px] text-[var(--color-text-muted)]">
                      Variables: {t.variables.map((v) => `{${v}}`).join(", ")}
                    </span>
                    <div className="flex gap-1.5">
                      {isCustom && (
                        <button
                          onClick={() => resetMutation.mutate(t.key)}
                          disabled={resetMutation.isPending}
                          className="flex items-center gap-1 rounded px-2 py-1 text-xs text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)] transition-colors"
                          title="Reset to default"
                        >
                          <RotateCcw className="h-3 w-3" /> Reset
                        </button>
                      )}
                      <button
                        onClick={() => saveMutation.mutate({ key: t.key, value: editValue })}
                        disabled={!hasUnsavedChanges || !editValue.trim() || saveMutation.isPending}
                        className="rounded bg-[var(--color-brand)] px-2 py-1 text-xs font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-40 transition-colors"
                      >
                        Save
                      </button>
                    </div>
                  </div>
                </div>
              );
            })
          )}
        </div>
      )}
    </div>
  );
}

// ─── Raffle History ──────────────────────────────────────

function RaffleHistory({ items }: { items: RaffleHistoryItem[] }) {
  const closedItems = items.filter((r) => !r.isOpen);
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const [expandedRaffle, setExpandedRaffle] = useState<RaffleDto | null>(null);
  const [loadingExpanded, setLoadingExpanded] = useState(false);

  const handleRowClick = async (id: number) => {
    if (expandedId === id) {
      setExpandedId(null);
      setExpandedRaffle(null);
      return;
    }

    setExpandedId(id);
    setExpandedRaffle(null);
    setLoadingExpanded(true);
    try {
      const raffle = await fetchRaffleById(id);
      setExpandedRaffle(raffle);
    } catch {
      setExpandedRaffle(null);
    } finally {
      setLoadingExpanded(false);
    }
  };

  if (closedItems.length === 0) {
    return (
      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-4">
        <h2 className="text-sm font-semibold text-[var(--color-text)] mb-2">Raffle History</h2>
        <p className="text-sm text-[var(--color-text-muted)]">No raffles yet.</p>
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)]">
      <div className="border-b border-[var(--color-border)] px-4 py-3">
        <h2 className="text-sm font-semibold text-[var(--color-text)]">Raffle History</h2>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-[var(--color-border)] text-left text-xs text-[var(--color-text-muted)]">
              <th className="px-4 py-2 font-medium w-6"></th>
              <th className="px-4 py-2 font-medium">Title</th>
              <th className="px-4 py-2 font-medium">Winner</th>
              <th className="px-4 py-2 font-medium text-right">Entries</th>
              <th className="px-4 py-2 font-medium">Keyword</th>
              <th className="px-4 py-2 font-medium">Status</th>
              <th className="px-4 py-2 font-medium">Created</th>
            </tr>
          </thead>
          <tbody>
            {closedItems.map((raffle) => {
              const isExpanded = expandedId === raffle.id;
              return (
                <Fragment key={raffle.id}>
                  <tr
                    onClick={() => handleRowClick(raffle.id)}
                    className={`border-b border-[var(--color-border)] last:border-0 hover:bg-[var(--color-elevated)] transition-colors cursor-pointer ${isExpanded ? "bg-[var(--color-elevated)]" : ""}`}
                  >
                    <td className="px-4 py-2.5 text-[var(--color-text-muted)]">
                      {isExpanded ? (
                        <ChevronDown className="h-3.5 w-3.5" />
                      ) : (
                        <ChevronRight className="h-3.5 w-3.5" />
                      )}
                    </td>
                    <td className="px-4 py-2.5 text-[var(--color-text)]">{raffle.title}</td>
                    <td className="px-4 py-2.5">
                      {raffle.winnerName ? (
                        <span className="flex items-center gap-1 text-[var(--color-text)]">
                          <Trophy className="h-3 w-3 text-yellow-500" />
                          {raffle.winnerName}
                        </span>
                      ) : (
                        <span className="text-[var(--color-text-muted)]">No winner</span>
                      )}
                    </td>
                    <td className="px-4 py-2.5 text-right text-[var(--color-text)]">{raffle.entryCount}</td>
                    <td className="px-4 py-2.5">
                      <span className="inline-block rounded px-1.5 py-0.5 text-xs bg-[var(--color-elevated)] text-[var(--color-text-secondary)]">
                        {raffle.keyword || "!join"}
                      </span>
                    </td>
                    <td className="px-4 py-2.5 text-xs text-[var(--color-text-muted)]">{raffle.endReason}</td>
                    <td className="px-4 py-2.5 text-xs text-[var(--color-text-muted)]">
                      {new Date(raffle.createdAt).toLocaleString()}
                    </td>
                  </tr>
                  {isExpanded && (
                    <tr className="border-b border-[var(--color-border)] last:border-0">
                      <td colSpan={7} className="px-4 py-3 bg-[var(--color-elevated)]">
                        {loadingExpanded ? (
                          <p className="text-xs text-[var(--color-text-muted)]">Loading draw history...</p>
                        ) : expandedRaffle && expandedRaffle.draws && expandedRaffle.draws.length > 0 ? (
                          <DrawHistoryList draws={expandedRaffle.draws} />
                        ) : (
                          <p className="text-xs text-[var(--color-text-muted)]">No draw history available.</p>
                        )}
                      </td>
                    </tr>
                  )}
                </Fragment>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
