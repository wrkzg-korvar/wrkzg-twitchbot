import { type ReactNode } from "react";

interface OverlayShellProps {
  children: ReactNode;
}

/**
 * Minimal wrapper for all overlay components.
 * Provides transparent background, full viewport, no padding/scrollbars.
 */
export function OverlayShell({ children }: OverlayShellProps) {
  return (
    <div
      className="overlay-root"
      style={{
        position: "fixed",
        top: 0,
        left: 0,
        width: "100vw",
        height: "100vh",
        margin: 0,
        padding: 0,
        overflow: "hidden",
        background: "transparent",
        fontFamily:
          'system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
      }}
    >
      {children}
    </div>
  );
}
