import { useQuery } from "@tanstack/react-query";

export interface SetupStatus {
  hasCredentials: boolean;
  hasBotToken: boolean;
  hasBroadcasterToken: boolean;
  setupComplete: boolean;
}

async function fetchSetupStatus(): Promise<SetupStatus> {
  const res = await fetch("/auth/setup-status");
  if (!res.ok) {
    throw new Error(`Setup status check failed: ${res.status}`);
  }
  return res.json();
}

export function useSetupStatus() {
  const { data, isLoading, error, refetch } = useQuery<SetupStatus>({
    queryKey: ["setupStatus"],
    queryFn: fetchSetupStatus,
    staleTime: 10_000,
  });

  return {
    hasCredentials: data?.hasCredentials ?? false,
    hasBotToken: data?.hasBotToken ?? false,
    hasBroadcasterToken: data?.hasBroadcasterToken ?? false,
    setupComplete: data?.setupComplete ?? false,
    isLoading,
    error,
    refetch,
  };
}
