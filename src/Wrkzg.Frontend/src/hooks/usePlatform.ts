import { useQuery } from "@tanstack/react-query";

export type Platform = "macos" | "windows" | "linux";

/**
 * Returns the OS platform from the server.
 * Photino's WKWebView UA is just "Photino WebView" without OS info,
 * so we get the platform from the .NET backend via /api/status.
 * Shares the same React Query cache key as the Dashboard status query.
 */
export function usePlatform(): Platform {
  const { data } = useQuery<{ platform: Platform }>({
    queryKey: ["status"],
    queryFn: async () => {
      const res = await fetch("/api/status");
      if (!res.ok) throw new Error("Failed");
      return res.json();
    },
    staleTime: Infinity, // OS never changes at runtime
  });

  return data?.platform ?? "windows"; // Fallback until first fetch completes
}
