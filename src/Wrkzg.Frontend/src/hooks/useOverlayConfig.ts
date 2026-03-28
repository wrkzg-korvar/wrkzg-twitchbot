import { useEffect, useState } from "react";

/**
 * Fetches overlay configuration from the API.
 * Falls back to defaults if the fetch fails (e.g. running in OBS without auth).
 * Config values from URL query params override fetched/default values.
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
      if (key !== "source") {
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
        // Fetch failed (no auth, network error) — use defaults + query overrides
        setConfig({ ...defaults, ...queryOverrides });
      });
  }, [type]); // eslint-disable-line react-hooks/exhaustive-deps

  return config;
}
