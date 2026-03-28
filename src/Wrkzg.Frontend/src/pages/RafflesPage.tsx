import { useQuery } from "@tanstack/react-query";
import { rafflesApi } from "../api/raffles";
import { useRaffleLive } from "../hooks/useRaffleLive";
import { PageHeader } from "../components/ui/PageHeader";
import { RaffleForm } from "../components/features/raffles/RaffleForm";
import {
  ActiveRafflePanel,
  RaffleVerification,
  RafflePostAccept,
  DrawAnimation,
} from "../components/features/raffles/RaffleActive";
import { RaffleHistory, RaffleAnnouncementTemplates } from "../components/features/raffles/RaffleHistory";
import type { RaffleHistoryItem } from "../types/raffles";

export function RafflesPage() {
  const { data: history } = useQuery<RaffleHistoryItem[]>({
    queryKey: ["raffleHistory"],
    queryFn: rafflesApi.getHistory,
  });

  const {
    liveRaffle,
    participants,
    drawnWinner,
    showDrawAnimation,
    hasAcceptedDraws,
    handleAnimationComplete,
  } = useRaffleLive();

  const handleAccept = () => rafflesApi.accept();
  const handleRedraw = () => rafflesApi.redraw({ reason: "User not present" });
  const handleDrawAnother = () => rafflesApi.draw();
  const handleEndRaffle = () => rafflesApi.end();

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Raffles"
        description="Create raffles, draw winners, and browse history."
      />

      {!liveRaffle && <RaffleForm />}

      {liveRaffle && !liveRaffle.pendingWinner && !hasAcceptedDraws && (
        <ActiveRafflePanel raffle={liveRaffle} participants={participants} />
      )}

      {liveRaffle && liveRaffle.pendingWinner && (
        <RaffleVerification
          raffle={liveRaffle}
          participants={participants}
          onAccept={handleAccept}
          onRedraw={handleRedraw}
          onCancel={() => rafflesApi.cancel()}
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

      <RaffleAnnouncementTemplates />

      <RaffleHistory items={history ?? []} />
    </div>
  );
}
