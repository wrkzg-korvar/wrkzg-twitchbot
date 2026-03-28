import { useEffect, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useSignalR } from "../hooks/useSignalR";
import { pollsApi } from "../api/polls";
import { PageHeader } from "../components/ui/PageHeader";
import { PollForm } from "../components/features/polls/PollForm";
import { PollActive } from "../components/features/polls/PollActive";
import { PollHistory, PollAnnouncementTemplates } from "../components/features/polls/PollHistory";
import type { PollResults, PollHistoryItem } from "../types/polls";

export function PollsPage() {
  const queryClient = useQueryClient();
  const { isConnected: signalRConnected, on, off } = useSignalR("/hubs/chat");

  const { data: activePoll } = useQuery<PollResults | null>({
    queryKey: ["pollActive"],
    queryFn: pollsApi.getActive,
    refetchInterval: 10_000,
  });

  const { data: history } = useQuery<PollHistoryItem[]>({
    queryKey: ["pollHistory"],
    queryFn: pollsApi.getHistory,
  });

  // Live poll state for SignalR updates
  const [livePoll, setLivePoll] = useState<PollResults | null>(null);

  // Sync REST data into live state
  useEffect(() => {
    if (activePoll) {
      setLivePoll(activePoll);
    } else {
      setLivePoll(null);
    }
  }, [activePoll]);

  // SignalR events
  useEffect(() => {
    if (!signalRConnected) return;

    on("PollCreated", () => {
      queryClient.invalidateQueries({ queryKey: ["pollActive"] });
    });

    on<{ pollId: number; optionIndex: number }>("PollVote", ({ pollId, optionIndex }) => {
      setLivePoll((prev) => {
        if (!prev || prev.id !== pollId) return prev;
        const totalVotes = prev.totalVotes + 1;
        const options = prev.options.map((opt, i) => {
          const votes = i === optionIndex ? opt.votes + 1 : opt.votes;
          return { ...opt, votes };
        });
        const recalculated = options.map((opt) => ({
          ...opt,
          percentage: totalVotes > 0 ? Math.round((opt.votes / totalVotes) * 1000) / 10 : 0,
        }));
        return { ...prev, totalVotes, options: recalculated };
      });
    });

    on("PollEnded", () => {
      setLivePoll(null);
      queryClient.invalidateQueries({ queryKey: ["pollActive"] });
      queryClient.invalidateQueries({ queryKey: ["pollHistory"] });
    });

    return () => {
      off("PollCreated");
      off("PollVote");
      off("PollEnded");
    };
  }, [signalRConnected, on, off, queryClient]);

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Polls"
        description="Create polls, view live results, and browse history."
      />

      <PollForm />

      {livePoll && <PollActive poll={livePoll} />}

      <PollAnnouncementTemplates />

      <PollHistory items={history ?? []} />
    </div>
  );
}
