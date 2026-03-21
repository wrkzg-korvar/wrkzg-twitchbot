/**
 * Invisible overlay with resize hotspots for chromeless windows on Windows.
 * On macOS, WebKit handles chromeless resize natively via the CSS border trick.
 * On Windows, we trigger Win32 WM_SYSCOMMAND + SC_SIZE via the backend API.
 */

const EDGE = 6;
const CORNER = 12;

type Direction = "n" | "s" | "e" | "w" | "ne" | "nw" | "se" | "sw";

interface Zone {
  direction: Direction;
  style: React.CSSProperties;
  cursor: string;
}

const zones: Zone[] = [
  // Edges
  { direction: "n", cursor: "ns-resize", style: { top: 0, left: CORNER, right: CORNER, height: EDGE } },
  { direction: "s", cursor: "ns-resize", style: { bottom: 0, left: CORNER, right: CORNER, height: EDGE } },
  { direction: "w", cursor: "ew-resize", style: { left: 0, top: CORNER, bottom: CORNER, width: EDGE } },
  { direction: "e", cursor: "ew-resize", style: { right: 0, top: CORNER, bottom: CORNER, width: EDGE } },
  // Corners
  { direction: "nw", cursor: "nwse-resize", style: { top: 0, left: 0, width: CORNER, height: CORNER } },
  { direction: "ne", cursor: "nesw-resize", style: { top: 0, right: 0, width: CORNER, height: CORNER } },
  { direction: "sw", cursor: "nesw-resize", style: { bottom: 0, left: 0, width: CORNER, height: CORNER } },
  { direction: "se", cursor: "nwse-resize", style: { bottom: 0, right: 0, width: CORNER, height: CORNER } },
];

function startResize(direction: Direction) {
  fetch("/api/window/start-resize", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ direction }),
  });
}

export function ResizeFrame() {
  // Only render on Windows — macOS handles resize natively
  const isWindows = navigator.userAgent.includes("Windows");
  if (!isWindows) {
    return null;
  }

  return (
    <>
      {zones.map((zone) => (
        <div
          key={zone.direction}
          onMouseDown={() => startResize(zone.direction)}
          style={{
            position: "fixed",
            zIndex: 9999,
            cursor: zone.cursor,
            ...zone.style,
          }}
        />
      ))}
    </>
  );
}
