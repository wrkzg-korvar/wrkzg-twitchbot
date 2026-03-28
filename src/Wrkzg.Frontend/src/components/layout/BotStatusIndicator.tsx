import { Wifi, WifiOff } from "lucide-react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { useSignalR } from "../../hooks/useSignalR";

interface StatusResponse {
  bot: { isConnected: boolean; channel: string | null };
}

export function BotStatusIndicator() {
  const queryClient = useQueryClient();
  const { isConnected: signalRConnected, on, off } = useSignalR("/hubs/chat");

  // Use the same query key as Dashboard — shared cache
  const { data } = useQuery<StatusResponse>({
    queryKey: ["status"],
    queryFn: async () => {
      const res = await fetch("/api/status");
      if (!res.ok) {
        throw new Error("Failed");
      }
      return res.json();
    },
    refetchInterval: 15_000,
  });

  // Refetch status when SignalR reports a change
  useEffect(() => {
    if (!signalRConnected) {
      return;
    }

    on("BotStatus", () => {
      queryClient.invalidateQueries({ queryKey: ["status"] });
    });

    return () => off("BotStatus");
  }, [signalRConnected, on, off, queryClient]);

  const botConnected = data?.bot.isConnected ?? false;
  const botChannel = data?.bot.channel ?? null;

  if (botConnected) {
    return (
      <div className="flex items-center gap-2 text-xs">
        <Wifi className="h-3.5 w-3.5 text-green-400" />
        <div>
          <p className="font-medium text-green-400">Bot online</p>
          {botChannel && <p className="text-[var(--color-text-muted)]">#{botChannel}</p>}
        </div>
      </div>
    );
  }

  return (
    <div className="flex items-center gap-2 text-xs text-[var(--color-text-muted)]">
      <WifiOff className="h-3.5 w-3.5" />
      <span>Bot offline</span>
    </div>
  );
}
