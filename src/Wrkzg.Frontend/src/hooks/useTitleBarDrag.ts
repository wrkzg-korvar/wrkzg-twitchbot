import { useRef, useCallback, useEffect } from "react";

/**
 * Provides mouseDown/mouseMove/mouseUp handlers for window dragging
 * via REST endpoints. Skips dragging if the mouseDown target is inside
 * an element with [data-no-drag].
 */
export function useTitleBarDrag() {
  const isDragging = useRef(false);

  const onMouseDown = useCallback((e: React.MouseEvent) => {
    if (e.button !== 0) return;
    if ((e.target as HTMLElement).closest("[data-no-drag]")) return;

    isDragging.current = true;
    fetch("/api/window/drag-start", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ screenX: e.screenX, screenY: e.screenY }),
    });
  }, []);

  const onMouseMove = useCallback((e: MouseEvent) => {
    if (!isDragging.current) return;
    fetch("/api/window/drag-move", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ screenX: e.screenX, screenY: e.screenY }),
    });
  }, []);

  const onMouseUp = useCallback(() => {
    isDragging.current = false;
  }, []);

  useEffect(() => {
    window.addEventListener("mousemove", onMouseMove);
    window.addEventListener("mouseup", onMouseUp);
    return () => {
      window.removeEventListener("mousemove", onMouseMove);
      window.removeEventListener("mouseup", onMouseUp);
    };
  }, [onMouseMove, onMouseUp]);

  return { onMouseDown };
}
