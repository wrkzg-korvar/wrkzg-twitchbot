const BASE = "/auth";

export type TokenType = "bot" | "broadcaster";

export interface AuthAccountState {
  tokenType: TokenType;
  isAuthenticated: boolean;
  twitchUsername: string | null;
  twitchDisplayName: string | null;
  twitchUserId: string | null;
  scopes: string[] | null;
}

export interface AuthStatusResponse {
  bot: AuthAccountState;
  broadcaster: AuthAccountState;
}

export async function getAuthStatus(): Promise<AuthStatusResponse> {
  const res = await fetch(`${BASE}/status`);
  if (!res.ok) throw new Error(`Auth status failed: ${res.status}`);
  return res.json();
}

/**
 * Starts the OAuth flow by telling the backend to open the system browser.
 *
 * Why server-side? Photino's WKWebView (macOS) blocks ALL window.open() calls
 * silently. Opening the browser from the .NET server via Process.Start is
 * the only reliable cross-platform approach for embedded WebViews.
 *
 * Flow:
 *   1. POST /auth/open-browser/{type} → server opens OS default browser
 *   2. User authorizes in their browser (already logged into Twitch)
 *   3. Twitch redirects to localhost:5000/auth/callback → Kestrel handles it
 *   4. Callback page: "You can close this tab"
 *   5. SignalR AuthStateChanged → Photino app updates automatically
 */
export async function startOAuthFlow(type: TokenType): Promise<void> {
  const res = await fetch(`${BASE}/open-browser/${type}`, { method: "POST" });
  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.error || `Failed to start auth flow: ${res.status}`);
  }
}

export async function disconnect(type: TokenType): Promise<void> {
  const res = await fetch(`${BASE}/disconnect/${type}`, { method: "POST" });
  if (!res.ok) throw new Error(`Disconnect failed: ${res.status}`);
}

export async function saveCredentials(clientId: string, clientSecret: string): Promise<void> {
  const res = await fetch(`${BASE}/credentials`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ clientId, clientSecret }),
  });
  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.error || `Save credentials failed: ${res.status}`);
  }
}
