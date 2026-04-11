/**
 * Per-session API token for authenticating requests to the Wrkzg backend.
 *
 * The Photino WebView loads the app with `?__wrkzg_token=xxx` in the URL.
 * This module:
 *   1. Reads the token from the URL (or sessionStorage on reload)
 *   2. Persists it in sessionStorage so it survives page reloads
 *   3. Removes it from the visible URL (clean address bar)
 *   4. Patches window.fetch to add the X-Wrkzg-Token header on all requests
 *   5. Exports the token for SignalR connection setup
 *
 * sessionStorage is scoped to the tab/window and cleared when the app closes,
 * so the token never persists beyond the current session.
 */

const STORAGE_KEY = "__wrkzg_token";

let apiToken: string | null = null;

/**
 * Returns true if the request targets the local Wrkzg backend (relative URL or same origin).
 * External requests (e.g. api.github.com) must NOT receive the X-Wrkzg-Token header.
 */
function isInternalRequest(input: RequestInfo | URL): boolean {
  if (typeof input === "string") {
    // Relative URLs (e.g. "/api/status") are always internal
    if (input.startsWith("/")) {
      return true;
    }
    try {
      const url = new URL(input, window.location.origin);
      return url.origin === window.location.origin;
    } catch {
      return true; // Malformed URL — treat as internal to be safe
    }
  }
  if (input instanceof URL) {
    return input.origin === window.location.origin;
  }
  if (input instanceof Request) {
    try {
      const url = new URL(input.url);
      return url.origin === window.location.origin;
    } catch {
      return true;
    }
  }
  return true;
}

/**
 * Initialize the API token from the URL query parameter or sessionStorage.
 * Must be called once at app startup (in main.tsx) before any fetch calls.
 */
export function initApiToken(): void {
  // 1. Try URL query parameter (initial load from Photino)
  const params = new URLSearchParams(window.location.search);
  apiToken = params.get(STORAGE_KEY);

  if (apiToken) {
    // Persist in sessionStorage so it survives reload
    sessionStorage.setItem(STORAGE_KEY, apiToken);

    // Remove the token from the visible URL for cleanliness
    params.delete(STORAGE_KEY);
    const cleanUrl = params.toString()
      ? `${window.location.pathname}?${params.toString()}${window.location.hash}`
      : `${window.location.pathname}${window.location.hash}`;
    window.history.replaceState({}, "", cleanUrl);
  } else {
    // 2. Fallback: recover from sessionStorage (page reload)
    apiToken = sessionStorage.getItem(STORAGE_KEY);
  }

  // Patch window.fetch to include the token header on all requests.
  // Uses plain header objects (not the Headers class) to avoid WKWebView
  // "The string did not match the expected pattern" errors.
  const originalFetch = window.fetch;
  window.fetch = (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    if (apiToken && isInternalRequest(input)) {
      const existing = init?.headers;
      let headerRecord: Record<string, string> = {};

      if (existing instanceof Headers) {
        existing.forEach((value, key) => {
          headerRecord[key] = value;
        });
      } else if (Array.isArray(existing)) {
        for (const [key, value] of existing) {
          headerRecord[key] = value;
        }
      } else if (existing && typeof existing === "object") {
        headerRecord = { ...(existing as Record<string, string>) };
      }

      headerRecord["X-Wrkzg-Token"] = apiToken;
      init = { ...init, headers: headerRecord };
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
