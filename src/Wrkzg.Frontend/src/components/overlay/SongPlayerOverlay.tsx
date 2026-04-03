import { useEffect, useState, useCallback } from "react";
import { useOverlaySignalR } from "../../hooks/useOverlaySignalR";
import { OverlayShell } from "./OverlayShell";
import type { SongRequest } from "../../types/songRequests";

function useQueryParam(key: string, defaultValue: string): string {
  const params = new URLSearchParams(window.location.search);
  return params.get(key) ?? defaultValue;
}

// Uses the auth-free overlay endpoint (not /api/song-requests/queue which needs X-Wrkzg-Token)
async function fetchOverlayQueue(): Promise<SongRequest[]> {
  try {
    const res = await fetch("/api/overlays/data/song-queue");
    if (!res.ok) { return []; }
    const text = await res.text();
    if (!text) { return []; }
    return JSON.parse(text);
  } catch {
    return [];
  }
}

export function SongPlayerOverlay() {
  const { on, off } = useOverlaySignalR("/hubs/chat");
  const [queue, setQueue] = useState<SongRequest[]>([]);
  const mode = useQueryParam("mode", "full");

  const loadQueue = useCallback(async () => {
    const data = await fetchOverlayQueue();
    setQueue(data);
  }, []);

  useEffect(() => {
    loadQueue();
    // Poll every 10s as fallback
    const interval = setInterval(loadQueue, 10000);
    return () => clearInterval(interval);
  }, [loadQueue]);

  useEffect(() => {
    on("SongQueueUpdated", () => {
      loadQueue();
    });
    return () => { off("SongQueueUpdated"); };
  }, [on, off, loadQueue]);

  // Find currently playing or first queued
  const currentSong = queue.find((s) => s.status === 1) ?? queue.find((s) => s.status === 0) ?? null;
  const queueCount = queue.filter((s) => s.status === 0).length;

  if (!currentSong) {
    return <OverlayShell><div /></OverlayShell>;
  }

  if (mode === "slim") {
    return <OverlayShell><SlimPlayer song={currentSong} queueCount={queueCount} /></OverlayShell>;
  }

  return <OverlayShell><FullPlayer song={currentSong} queueCount={queueCount} /></OverlayShell>;
}

// ─── Full Player (Apple Music style) ────────────────────────

function FullPlayer({ song, queueCount }: { song: SongRequest; queueCount: number }) {
  const thumbnailUrl = song.thumbnailUrl ?? `https://img.youtube.com/vi/${song.videoId}/mqdefault.jpg`;

  return (
    <div style={{
      display: "flex",
      alignItems: "center",
      gap: "16px",
      padding: "16px",
      background: "rgba(0, 0, 0, 0.75)",
      backdropFilter: "blur(20px)",
      borderRadius: "16px",
      border: "1px solid rgba(255, 255, 255, 0.1)",
      maxWidth: "420px",
      fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
      color: "white",
      animation: "songSlideIn 0.5s ease-out",
    }}>
      {/* Thumbnail */}
      <div style={{
        width: "64px",
        height: "64px",
        borderRadius: "10px",
        overflow: "hidden",
        flexShrink: 0,
        boxShadow: "0 4px 12px rgba(0,0,0,0.4)",
      }}>
        <img
          src={thumbnailUrl}
          alt=""
          style={{ width: "100%", height: "100%", objectFit: "cover" }}
        />
      </div>

      {/* Info */}
      <div style={{ flex: 1, minWidth: 0, overflow: "hidden" }}>
        <div style={{
          fontSize: "14px",
          fontWeight: 600,
          whiteSpace: "nowrap",
          overflow: "hidden",
          textOverflow: "ellipsis",
          lineHeight: 1.3,
        }}>
          {song.title}
        </div>
        <div style={{
          fontSize: "12px",
          opacity: 0.6,
          marginTop: "2px",
          whiteSpace: "nowrap",
          overflow: "hidden",
          textOverflow: "ellipsis",
        }}>
          Requested by {song.requestedBy}
        </div>
        {queueCount > 0 && (
          <div style={{
            fontSize: "10px",
            opacity: 0.4,
            marginTop: "4px",
          }}>
            {queueCount} more in queue
          </div>
        )}
      </div>

      {/* Music bars animation */}
      <div style={{ display: "flex", alignItems: "flex-end", gap: "2px", height: "20px", flexShrink: 0 }}>
        {[1, 2, 3, 4].map((i) => (
          <div
            key={i}
            style={{
              width: "3px",
              borderRadius: "1.5px",
              background: "#a855f7",
              animation: `musicBar 0.8s ease-in-out infinite alternate`,
              animationDelay: `${i * 0.15}s`,
              height: "8px",
            }}
          />
        ))}
      </div>

      <style>{`
        @keyframes songSlideIn {
          from { opacity: 0; transform: translateY(20px); }
          to { opacity: 1; transform: translateY(0); }
        }
        @keyframes musicBar {
          from { height: 4px; }
          to { height: 20px; }
        }
      `}</style>
    </div>
  );
}

// ─── Slim Player (compact bar) ──────────────────────────────

function SlimPlayer({ song, queueCount }: { song: SongRequest; queueCount: number }) {
  const thumbnailUrl = song.thumbnailUrl ?? `https://img.youtube.com/vi/${song.videoId}/mqdefault.jpg`;

  return (
    <div style={{
      display: "flex",
      alignItems: "center",
      gap: "10px",
      padding: "6px 12px 6px 6px",
      background: "rgba(0, 0, 0, 0.7)",
      backdropFilter: "blur(16px)",
      borderRadius: "8px",
      border: "1px solid rgba(255, 255, 255, 0.08)",
      fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
      color: "white",
      animation: "songSlideIn 0.4s ease-out",
      maxWidth: "360px",
    }}>
      {/* Small thumbnail */}
      <div style={{
        width: "32px",
        height: "32px",
        borderRadius: "6px",
        overflow: "hidden",
        flexShrink: 0,
      }}>
        <img
          src={thumbnailUrl}
          alt=""
          style={{ width: "100%", height: "100%", objectFit: "cover" }}
        />
      </div>

      {/* Bars */}
      <div style={{ display: "flex", alignItems: "flex-end", gap: "1.5px", height: "14px", flexShrink: 0 }}>
        {[1, 2, 3].map((i) => (
          <div
            key={i}
            style={{
              width: "2px",
              borderRadius: "1px",
              background: "#a855f7",
              animation: `musicBar 0.8s ease-in-out infinite alternate`,
              animationDelay: `${i * 0.15}s`,
              height: "6px",
            }}
          />
        ))}
      </div>

      {/* Title */}
      <div style={{
        flex: 1,
        minWidth: 0,
        fontSize: "12px",
        fontWeight: 500,
        whiteSpace: "nowrap",
        overflow: "hidden",
        textOverflow: "ellipsis",
      }}>
        {song.title}
      </div>

      {/* Requester */}
      <div style={{
        fontSize: "10px",
        opacity: 0.5,
        flexShrink: 0,
        whiteSpace: "nowrap",
      }}>
        {song.requestedBy}
        {queueCount > 0 && ` · +${queueCount}`}
      </div>

      <style>{`
        @keyframes songSlideIn {
          from { opacity: 0; transform: translateY(10px); }
          to { opacity: 1; transform: translateY(0); }
        }
        @keyframes musicBar {
          from { height: 3px; }
          to { height: 14px; }
        }
      `}</style>
    </div>
  );
}
