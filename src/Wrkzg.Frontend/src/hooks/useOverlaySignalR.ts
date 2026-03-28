import { useEffect, useRef, useState, useCallback } from "react";
import * as signalR from "@microsoft/signalr";

const RECONNECT_POLL_INTERVAL = 10_000;

/**
 * SignalR hook for overlay components.
 * Connects WITHOUT an auth token, using `?source=overlay` query param
 * so the server can identify overlay connections.
 *
 * When the backend goes away (bot closed), SignalR's built-in reconnect
 * will exhaust its retries. After that, this hook polls the backend every
 * 30 seconds and reloads the page once it comes back — ensuring fresh state.
 */
export function useOverlaySignalR(hubUrl: string) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const pollTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    const overlayUrl = `${hubUrl}?source=overlay`;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(overlayUrl)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    connectionRef.current = connection;

    connection.onreconnected(() => {
      setIsConnected(true);
      // Reload to get fresh state after reconnect
      window.location.reload();
    });

    connection.onclose(() => {
      setIsConnected(false);
      startBackendPolling();
    });

    function startBackendPolling() {
      if (pollTimerRef.current) return;
      pollTimerRef.current = setInterval(async () => {
        try {
          // Cache-busting timestamp prevents browser from serving cached error responses
          const res = await fetch(`/overlay/health?_=${Date.now()}`);
          if (res.ok) {
            clearInterval(pollTimerRef.current!);
            pollTimerRef.current = null;
            window.location.reload();
          }
        } catch {
          // Backend still down (network error), keep polling
        }
      }, RECONNECT_POLL_INTERVAL);
    }

    connection
      .start()
      .then(() => setIsConnected(true))
      .catch(() => {
        // Initial connection failed (bot not running yet), start polling
        startBackendPolling();
      });

    return () => {
      if (pollTimerRef.current) {
        clearInterval(pollTimerRef.current);
        pollTimerRef.current = null;
      }
      connection.stop();
    };
  }, [hubUrl]);

  const on = useCallback(
    <T>(methodName: string, handler: (data: T) => void) => {
      connectionRef.current?.on(methodName, handler);
    },
    [],
  );

  const off = useCallback((methodName: string) => {
    connectionRef.current?.off(methodName);
  }, []);

  return { isConnected, on, off };
}
