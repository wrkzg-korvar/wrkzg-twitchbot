import { useState } from "react";
import { Copy, Settings, Play } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { Card } from "../../ui/Card";
import { Button } from "../../ui/Button";
import { OverlayConfigModal } from "./OverlayConfigModal";
import { showToast } from "../../../hooks/useToast";

export interface OverlayDefinition {
  type: string;
  title: string;
  description: string;
  icon: LucideIcon;
  width: number;
  height: number;
  testEvents?: string[];
}

const TEST_EVENT_LABELS: Record<string, string> = {
  follow: "Follow",
  subscribe: "Sub",
  giftsub: "Gift Sub",
  resub: "Resub",
  raid: "Raid",
  counter: "Counter",
};

interface OverlayCardProps {
  overlay: OverlayDefinition;
}

function getOverlayUrl(type: string): string {
  const base = window.location.origin;
  return `${base}/overlay/${type}`;
}

export function OverlayCard({ overlay }: OverlayCardProps) {
  const [configOpen, setConfigOpen] = useState(false);
  const [testing, setTesting] = useState(false);
  const url = getOverlayUrl(overlay.type);
  const Icon = overlay.icon;

  async function handleCopyUrl() {
    try {
      await navigator.clipboard.writeText(url);
      showToast("success", "Overlay URL copied to clipboard");
    } catch {
      showToast("error", "Failed to copy URL");
    }
  }

  async function handleTestEvent(eventType: string) {
    setTesting(true);
    try {
      const res = await fetch(`/api/overlays/test/${eventType}`, { method: "POST" });
      if (res.ok) {
        showToast("success", `Test ${TEST_EVENT_LABELS[eventType] ?? eventType} event sent`);
      }
    } catch {
      showToast("error", "Failed to send test event");
    } finally {
      setTesting(false);
    }
  }

  return (
    <>
      <Card>
        <div className="space-y-4">
          {/* Header */}
          <div className="flex items-start gap-3">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-[var(--color-brand-subtle)]">
              <Icon className="h-5 w-5 text-[var(--color-brand-text)]" />
            </div>
            <div className="min-w-0">
              <h3 className="text-sm font-semibold text-[var(--color-text)]">{overlay.title}</h3>
              <p className="text-xs text-[var(--color-text-muted)]">{overlay.description}</p>
            </div>
          </div>

          {/* Live preview */}
          <div className="overflow-hidden rounded-lg border border-[var(--color-border)] bg-gray-950">
            <iframe
              src={url}
              title={`${overlay.title} preview`}
              className="pointer-events-none block h-32 w-full origin-top-left"
              style={{
                transform: `scale(${Math.min(1, 320 / overlay.width)})`,
                height: `${Math.min(128, (overlay.height / overlay.width) * 320)}px`,
              }}
            />
          </div>

          {/* URL display */}
          <div className="flex items-center gap-2">
            <code className="flex-1 truncate rounded bg-[var(--color-elevated)] px-2 py-1.5 text-xs text-[var(--color-text-secondary)]">
              {url}
            </code>
            <Button variant="ghost" size="sm" onClick={handleCopyUrl} title="Copy URL">
              <Copy className="h-3.5 w-3.5" />
            </Button>
          </div>

          {/* Test events */}
          {overlay.testEvents && overlay.testEvents.length > 0 && (
            <div className="flex flex-wrap items-center gap-1.5">
              <span className="text-xs text-[var(--color-text-muted)] mr-1">Test:</span>
              {overlay.testEvents.map((evt) => (
                <button
                  key={evt}
                  onClick={() => handleTestEvent(evt)}
                  disabled={testing}
                  className="inline-flex items-center gap-1 rounded-md border border-[var(--color-border)] bg-[var(--color-elevated)] px-2 py-1 text-[10px] font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] hover:text-[var(--color-text)] transition-colors disabled:opacity-40"
                >
                  <Play className="h-2.5 w-2.5" />
                  {TEST_EVENT_LABELS[evt] ?? evt}
                </button>
              ))}
            </div>
          )}

          {/* Actions + recommended size */}
          <div className="flex items-center justify-between">
            <span className="text-xs text-[var(--color-text-muted)]">
              Recommended: {overlay.width} x {overlay.height}
            </span>
            <Button variant="secondary" size="sm" onClick={() => setConfigOpen(true)}>
              <Settings className="h-3.5 w-3.5" />
              Configure
            </Button>
          </div>
        </div>
      </Card>

      <OverlayConfigModal
        open={configOpen}
        onClose={() => setConfigOpen(false)}
        overlay={overlay}
      />
    </>
  );
}
