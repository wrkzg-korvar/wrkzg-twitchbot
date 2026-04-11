import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "../api/client";

interface LockState {
  lockedModules: string[];
}

export function useModuleLock(modulePath: string): {
  isLocked: boolean;
  lockReason: string | null;
} {
  const { data } = useQuery<LockState>({
    queryKey: ["import-locks"],
    queryFn: () => api.get<LockState>("/api/import/locks"),
    refetchInterval: 5000,
  });

  return useMemo(() => {
    const isLocked = data?.lockedModules?.includes(modulePath) ?? false;
    return {
      isLocked,
      lockReason: isLocked ? "A data import is running. Changes are temporarily disabled." : null,
    };
  }, [data?.lockedModules, modulePath]);
}
