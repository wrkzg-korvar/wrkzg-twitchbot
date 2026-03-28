import { useEffect, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useSignalR } from "./useSignalR";
import { rafflesApi } from "../api/raffles";
import type { RaffleDto, RaffleDrawDto } from "../types/raffles";

export function useRaffleLive() {
  const queryClient = useQueryClient();
  const { isConnected: signalRConnected, on, off } = useSignalR("/hubs/chat");

  const { data: activeRaffle } = useQuery<RaffleDto | null>({
    queryKey: ["raffleActive"],
    queryFn: rafflesApi.getActive,
    refetchInterval: 10_000,
    retry: false,
  });

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
      setParticipants((prev) => (prev.includes(username) ? prev : [...prev, username]));
      setLiveRaffle((prev) => (prev ? { ...prev, entryCount: prev.entryCount + 1 } : prev));
    });

    on<{ winnerName: string; twitchId: string; drawNumber: number }>(
      "RaffleDrawPending",
      ({ winnerName, twitchId, drawNumber }) => {
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
      },
    );

    on<{ raffleId: number; winnerName: string; drawNumber: number }>(
      "RaffleWinnerAccepted",
      ({ drawNumber }) => {
        setLiveRaffle((prev) => {
          if (!prev) return prev;
          const updatedDraws = (prev.draws || []).map((d) =>
            d.drawNumber === drawNumber ? { ...d, isAccepted: true } : d,
          );
          return { ...prev, pendingWinner: null, draws: updatedDraws };
        });
      },
    );

    const clearRaffle = () => {
      setLiveRaffle(null);
      setParticipants([]);
      queryClient.invalidateQueries({ queryKey: ["raffleActive"] });
      queryClient.invalidateQueries({ queryKey: ["raffleHistory"] });
    };

    on<{ raffleId: number }>("RaffleEnded", clearRaffle);
    on<{ winnerName: string }>("RaffleDrawn", clearRaffle);
    on("RaffleCancelled", clearRaffle);

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

  const hasAcceptedDraws = liveRaffle?.draws?.some((d) => d.isAccepted) ?? false;

  return {
    liveRaffle,
    participants,
    drawnWinner,
    showDrawAnimation,
    hasAcceptedDraws,
    handleAnimationComplete,
  };
}
