import { useState } from "react";
import { startOAuthFlow, disconnect, type TokenType, type AuthAccountState } from "../api/auth";

interface TwitchAccountCardProps {
  title: string;
  description: string;
  tokenType: TokenType;
  state: AuthAccountState;
  scopes: string[];
}

export function TwitchAccountCard({
  title,
  description,
  tokenType,
  state,
  scopes,
}: TwitchAccountCardProps) {
  const [isDisconnecting, setIsDisconnecting] = useState(false);

  const handleConnect = () => {
    startOAuthFlow(tokenType);
  };

  const handleDisconnect = async () => {
    setIsDisconnecting(true);
    try {
      await disconnect(tokenType);
    } catch (err) {
      console.error("Disconnect failed:", err);
    } finally {
      setIsDisconnecting(false);
    }
  };

  return (
    <div className="rounded-lg border border-[var(--color-border)] p-6">
      <div className="flex items-start justify-between">
        <div>
          <h3 className="text-lg font-semibold">{title}</h3>
          <p className="mt-1 text-sm text-[var(--color-text-muted)]">
            {description}
          </p>
        </div>

        {state.isAuthenticated ? (
          <span className="inline-flex items-center rounded-full bg-green-100 px-3 py-1 text-sm font-medium text-green-800 dark:bg-green-900 dark:text-green-200">
            Connected
          </span>
        ) : (
          <span className="inline-flex items-center rounded-full bg-[var(--color-elevated)] px-3 py-1 text-sm font-medium text-[var(--color-text)]">
            Not connected
          </span>
        )}
      </div>

      {state.isAuthenticated && state.twitchUsername && (
        <div className="mt-4 rounded-md bg-[var(--color-elevated)] p-3">
          <p className="text-sm">
            <span className="font-medium">Account:</span>{" "}
            <span className="text-[var(--color-twitch)]">
              {state.twitchUsername}
            </span>
          </p>
          {state.scopes && state.scopes.length > 0 && (
            <p className="mt-1 text-xs text-[var(--color-text-muted)]">
              Scopes: {state.scopes.join(", ")}
            </p>
          )}
        </div>
      )}

      <div className="mt-4 flex items-center gap-3">
        {state.isAuthenticated ? (
          <>
            <button
              onClick={handleConnect}
              className="rounded-md bg-[var(--color-elevated)] px-4 py-2 text-sm font-medium text-[var(--color-text)] hover:bg-[var(--color-border)]"
            >
              Reconnect
            </button>
            <button
              onClick={handleDisconnect}
              disabled={isDisconnecting}
              className="rounded-md bg-red-50 px-4 py-2 text-sm font-medium text-red-600 hover:bg-red-100 disabled:opacity-50 dark:bg-red-900/20 dark:text-red-400 dark:hover:bg-red-900/40"
            >
              {isDisconnecting ? "Disconnecting…" : "Disconnect"}
            </button>
          </>
        ) : (
          <button
            onClick={handleConnect}
            className="rounded-md bg-[var(--color-twitch)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--color-twitch-hover)]"
          >
            Connect with Twitch
          </button>
        )}
      </div>

      {!state.isAuthenticated && (
        <div className="mt-3 text-xs text-[var(--color-text-secondary)]">
          Required scopes: {scopes.join(", ")}
        </div>
      )}
    </div>
  );
}
