import { useEffect, useRef, useState, useCallback } from "react";
import * as signalR from "@microsoft/signalr";
import { getApiToken } from "../lib/apiToken";

export function useSignalR(hubUrl: string) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    // Include API token as access_token query param for WebSocket authentication
    const token = getApiToken();
    const authenticatedUrl = token ? `${hubUrl}?access_token=${encodeURIComponent(token)}` : hubUrl;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(authenticatedUrl)
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;

    connection.onreconnected(() => setIsConnected(true));
    connection.onclose(() => setIsConnected(false));

    connection
      .start()
      .then(() => setIsConnected(true))
      .catch((err) => console.error("SignalR connection failed:", err));

    return () => {
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
