import * as signalR from "@microsoft/signalr";
import { getApiToken } from "./apiToken";

type ConnectionListener = (connected: boolean) => void;

class SignalRManager {
  private connection: signalR.HubConnection | null = null;
  private listeners = new Set<ConnectionListener>();
  private _isConnected = false;

  get isConnected(): boolean {
    return this._isConnected;
  }

  async ensureConnection(hubUrl: string): Promise<signalR.HubConnection> {
    if (this.connection) {
      return this.connection;
    }

    const token = getApiToken();
    const authenticatedUrl = token
      ? `${hubUrl}?access_token=${encodeURIComponent(token)}`
      : hubUrl;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(authenticatedUrl)
      .withAutomaticReconnect()
      .build();

    connection.onreconnected(() => this.setConnected(true));
    connection.onclose(() => this.setConnected(false));

    this.connection = connection;

    try {
      await connection.start();
      this.setConnected(true);
    } catch (err) {
      console.error("SignalR connection failed:", err);
    }

    return connection;
  }

  on<T>(method: string, handler: (data: T) => void): void {
    this.connection?.on(method, handler);
  }

  off(method: string): void {
    this.connection?.off(method);
  }

  subscribe(listener: ConnectionListener): () => void {
    this.listeners.add(listener);
    // Immediately report current status
    listener(this._isConnected);
    return () => {
      this.listeners.delete(listener);
    };
  }

  private setConnected(value: boolean) {
    this._isConnected = value;
    this.listeners.forEach((l) => l(value));
  }
}

export const signalRManager = new SignalRManager();
