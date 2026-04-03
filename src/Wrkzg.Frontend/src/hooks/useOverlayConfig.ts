import { useEffect, useState } from "react";

/**
 * Fetches overlay configuration from the API.
 * Falls back to defaults if the fetch fails (e.g. running in OBS without auth).
 * Config values from URL query params override fetched/default values.
 * Listens for live config updates from the editor via postMessage.
 */
export function useOverlayConfig(
  type: string,
  defaults: Record<string, string> = {},
): Record<string, string> {
  const [config, setConfig] = useState<Record<string, string>>(defaults);

  useEffect(() => {
    // Parse query params as overrides
    const params = new URLSearchParams(window.location.search);
    const queryOverrides: Record<string, string> = {};
    params.forEach((value, key) => {
      if (key !== "source" && key !== "preview") {
        queryOverrides[key] = value;
      }
    });

    fetch(`/api/overlays/settings/${encodeURIComponent(type)}`)
      .then((res) => {
        if (res.ok) {
          return res.json();
        }
        return null;
      })
      .then((data: Record<string, string> | null) => {
        setConfig({ ...defaults, ...(data ?? {}), ...queryOverrides });
      })
      .catch(() => {
        setConfig({ ...defaults, ...queryOverrides });
      });
  }, [type]); // eslint-disable-line react-hooks/exhaustive-deps

  // Listen for live config updates from editor iframe parent
  useEffect(() => {
    function handleMessage(event: MessageEvent) {
      if (event.data?.type === "wrkzg:config-update") {
        setConfig(prev => ({ ...prev, ...event.data.config }));
      }
    }
    window.addEventListener("message", handleMessage);
    return () => window.removeEventListener("message", handleMessage);
  }, []);

  return config;
}
