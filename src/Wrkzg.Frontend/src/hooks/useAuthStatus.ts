import { useEffect } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useSignalR } from "./useSignalR";
import { getAuthStatus, type AuthStatusResponse, type AuthAccountState } from "../api/auth";

export function useAuthStatus() {
  const queryClient = useQueryClient();
  const { isConnected, on, off } = useSignalR("/hubs/chat");

  const { data, isLoading, error } = useQuery<AuthStatusResponse>({
    queryKey: ["authStatus"],
    queryFn: getAuthStatus,
    refetchInterval: 5 * 60 * 1000,
    staleTime: 60 * 1000,
  });

  useEffect(() => {
    if (!isConnected) return;

    on<AuthAccountState>("AuthStateChanged", () => {
      queryClient.invalidateQueries({ queryKey: ["authStatus"] });
    });

    return () => off("AuthStateChanged");
  }, [isConnected, on, off, queryClient]);

  return {
    bot: data?.bot ?? {
      tokenType: "bot" as const,
      isAuthenticated: false,
      twitchUsername: null,
      twitchDisplayName: null,
      twitchUserId: null,
      scopes: null,
    },
    broadcaster: data?.broadcaster ?? {
      tokenType: "broadcaster" as const,
      isAuthenticated: false,
      twitchUsername: null,
      twitchDisplayName: null,
      twitchUserId: null,
      scopes: null,
    },
    isLoading,
    error,
  };
}
