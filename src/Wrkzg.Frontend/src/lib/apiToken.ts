/**
 * Per-session API token for authenticating requests to the Wrkzg backend.
 *
 * The Photino WebView loads the app with `?__wrkzg_token=xxx` in the URL.
 * This module:
 *   1. Reads the token from the URL on startup
 *   2. Removes it from the visible URL (clean address bar)
 *   3. Patches window.fetch to add the X-Wrkzg-Token header on all requests
 *   4. Exports the token for SignalR connection setup
 */

let apiToken: string | null = null;

/**
 * Initialize the API token from the URL query parameter.
 * Must be called once at app startup (in main.tsx) before any fetch calls.
 */
export function initApiToken(): void {
  const params = new URLSearchParams(window.location.search);
  apiToken = params.get("__wrkzg_token");

  if (apiToken) {
    // Remove the token from the visible URL for cleanliness
    params.delete("__wrkzg_token");
    const cleanUrl = params.toString()
      ? `${window.location.pathname}?${params.toString()}${window.location.hash}`
      : `${window.location.pathname}${window.location.hash}`;
    window.history.replaceState({}, "", cleanUrl);
  }

  // Patch window.fetch to include the token header on all requests.
  // Creates a new init object to avoid mutating the caller's original.
  const originalFetch = window.fetch;
  window.fetch = (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    if (apiToken) {
      const headers = new Headers(init?.headers);
      headers.set("X-Wrkzg-Token", apiToken);
      init = { ...init, headers };
    }
    return originalFetch.call(window, input, init);
  };
}

/**
 * Returns the current API token for use in SignalR connections.
 */
export function getApiToken(): string | null {
  return apiToken;
}
