import { useState, useEffect } from "react";

interface UpdateInfo {
  currentVersion: string;
  latestVersion: string;
  releaseUrl: string;
}

const CACHE_KEY = "wrkzg-update-check";
const DISMISS_KEY = "wrkzg-update-dismissed";
const CACHE_DURATION_MS = 30 * 60 * 1000;

interface CachedCheck {
  latestVersion: string;
  releaseUrl: string;
  checkedAt: number;
}

function compareSemver(a: string, b: string): number {
  const pa = a.replace(/^v/, "").split(".").map(Number);
  const pb = b.replace(/^v/, "").split(".").map(Number);
  for (let i = 0; i < 3; i++) {
    const diff = (pa[i] ?? 0) - (pb[i] ?? 0);
    if (diff !== 0) {
      return diff;
    }
  }
  return 0;
}

export function useUpdateCheck(): {
  update: UpdateInfo | null;
  dismiss: () => void;
} {
  const [update, setUpdate] = useState<UpdateInfo | null>(null);

  const dismiss = () => {
    if (update) {
      localStorage.setItem(DISMISS_KEY, update.latestVersion);
      setUpdate(null);
    }
  };

  useEffect(() => {
    let cancelled = false;

    async function check() {
      try {
        const statusRes = await fetch("/api/status");
        const status = await statusRes.json();
        const currentVersion: string = status.version ?? "0.0.0";

        const cached = localStorage.getItem(CACHE_KEY);
        let latestVersion: string;
        let releaseUrl: string;

        if (cached) {
          const parsed: CachedCheck = JSON.parse(cached);
          if (Date.now() - parsed.checkedAt < CACHE_DURATION_MS) {
            latestVersion = parsed.latestVersion;
            releaseUrl = parsed.releaseUrl;
          } else {
            const result = await fetchLatest();
            latestVersion = result.latestVersion;
            releaseUrl = result.releaseUrl;
          }
        } else {
          const result = await fetchLatest();
          latestVersion = result.latestVersion;
          releaseUrl = result.releaseUrl;
        }

        if (cancelled) {
          return;
        }

        const dismissed = localStorage.getItem(DISMISS_KEY);
        if (dismissed === latestVersion) {
          return;
        }

        if (compareSemver(latestVersion, currentVersion) > 0) {
          setUpdate({ currentVersion, latestVersion, releaseUrl });
        }
      } catch {
        // Silently ignore update check failures
      }
    }

    check();
    return () => {
      cancelled = true;
    };
  }, []);

  return { update, dismiss };
}

async function fetchLatest(): Promise<{ latestVersion: string; releaseUrl: string }> {
  const res = await fetch(
    "https://api.github.com/repos/wrkzg-korvar/wrkzg-twitchbot/releases/latest",
  );
  const data = await res.json();
  const latestVersion: string = data.tag_name ?? "0.0.0";
  const releaseUrl: string = data.html_url ?? "";

  const cached: CachedCheck = { latestVersion, releaseUrl, checkedAt: Date.now() };
  localStorage.setItem(CACHE_KEY, JSON.stringify(cached));

  return { latestVersion, releaseUrl };
}
