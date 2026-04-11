import { useEffect, useState, useCallback } from "react";
import { signalRManager } from "../lib/signalRManager";

export function useSignalR(hubUrl: string) {
  const [isConnected, setIsConnected] = useState(signalRManager.isConnected);

  useEffect(() => {
    // Ensure the connection is established
    signalRManager.ensureConnection(hubUrl);

    // Subscribe to status changes
    const unsubscribe = signalRManager.subscribe((connected) => {
      setIsConnected(connected);
    });

    return unsubscribe;
    // hubUrl does not change at runtime — safe to ignore
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hubUrl]);

  const on = useCallback(
    <T>(methodName: string, handler: (data: T) => void) => {
      signalRManager.on(methodName, handler);
    },
    [],
  );

  const off = useCallback((methodName: string) => {
    signalRManager.off(methodName);
  }, []);

  return { isConnected, on, off };
}
