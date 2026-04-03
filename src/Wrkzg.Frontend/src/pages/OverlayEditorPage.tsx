import { useState, useEffect, useRef, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { ChevronLeft, Copy, RotateCcw, Play } from "lucide-react";
import { PageHeader } from "../components/ui/PageHeader";
import { FontPicker } from "../components/ui/FontPicker";
import { AssetPicker } from "../components/ui/AssetPicker";
import { showToast } from "../hooks/useToast";

const OVERLAY_TITLES: Record<string, string> = {
  alerts: "Alert Box",
  chat: "Chat Box",
  poll: "Poll",
  raffle: "Raffle",
  counter: "Counter",
  events: "Event List",
  "song-player": "Song Player",
};

const ANIMATIONS = [
  { value: "slideDown", label: "Slide Down" },
  { value: "slideUp", label: "Slide Up" },
  { value: "slideLeft", label: "Slide Left" },
  { value: "slideRight", label: "Slide Right" },
  { value: "fadeIn", label: "Fade In" },
  { value: "bounceIn", label: "Bounce In" },
  { value: "zoomIn", label: "Zoom In" },
  { value: "flipIn", label: "Flip In" },
  { value: "rotateIn", label: "Rotate In" },
  { value: "jackInTheBox", label: "Jack in the Box" },
  { value: "rubberBand", label: "Rubber Band" },
  { value: "heartBeat", label: "Heart Beat" },
  { value: "tada", label: "Tada" },
  { value: "none", label: "No Animation" },
];

const EVENT_TYPES = [
  { key: "follow", label: "Follow", icon: "\u2B50", testKey: "follow" },
  { key: "subscribe", label: "Subscribe", icon: "\uD83D\uDC9C", testKey: "subscribe" },
  { key: "giftsub", label: "Gift Subs", icon: "\uD83C\uDF81", testKey: "giftsub" },
  { key: "resub", label: "Resub", icon: "\uD83D\uDD01", testKey: "resub" },
  { key: "raid", label: "Raid", icon: "\u2694\uFE0F", testKey: "raid" },
  { key: "channelpoint", label: "Channel Points", icon: "\uD83D\uDC8E", testKey: "" },
];

interface OverlaySettings {
  [key: string]: string;
}

async function fetchSettings(type: string): Promise<OverlaySettings> {
  const res = await fetch(`/api/overlays/settings/${type}`);
  return res.ok ? res.json() : {};
}

async function saveSettings(type: string, settings: OverlaySettings) {
  const res = await fetch(`/api/overlays/settings/${type}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(settings),
  });
  if (!res.ok) { throw new Error("Failed to save"); }
}

export function OverlayEditorPage() {
  const { type } = useParams<{ type: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const [activeTab, setActiveTab] = useState("general");
  const [formValues, setFormValues] = useState<OverlaySettings>({});

  const overlayType = type ?? "alerts";
  const title = OVERLAY_TITLES[overlayType] ?? overlayType;
  const isAlerts = overlayType === "alerts";

  const { data: settings } = useQuery({
    queryKey: ["overlay-settings", overlayType],
    queryFn: () => fetchSettings(overlayType),
  });

  useEffect(() => {
    if (settings) {
      setFormValues(settings);
    }
  }, [settings]);

  const saveMutation = useMutation({
    mutationFn: () => saveSettings(overlayType, formValues),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["overlay-settings", overlayType] });
      showToast("success", "Settings saved!");
      sendConfigToPreview(formValues);
    },
    onError: () => showToast("error", "Failed to save settings."),
  });

  const updateField = useCallback((key: string, value: string) => {
    setFormValues(prev => {
      const next = { ...prev, [key]: value };
      // Send live update to preview iframe
      sendConfigToPreview(next);
      return next;
    });
  }, []);

  function sendConfigToPreview(config: OverlaySettings) {
    iframeRef.current?.contentWindow?.postMessage(
      { type: "wrkzg:config-update", config }, "*"
    );
  }

  async function sendTestEvent(eventType: string) {
    // First save current settings so the overlay picks them up
    try {
      await saveSettings(overlayType, formValues);
    } catch { /* ignore save errors for test */ }
    // Then fire the test event
    try {
      await fetch(`/api/overlays/test/${eventType}`, { method: "POST" });
      showToast("success", `Test ${eventType} sent!`);
    } catch {
      showToast("error", "Failed to send test event.");
    }
  }

  function copyUrl() {
    const url = `${window.location.origin}/overlay/${overlayType}`;
    navigator.clipboard.writeText(url);
    showToast("success", "URL copied!");
  }

  const tabs = isAlerts
    ? ["general", "events", "style", "customCSS"]
    : ["general", "style", "customCSS"];

  const tabLabels: Record<string, string> = {
    general: "General",
    events: "Events",
    style: "Style",
    customCSS: "Custom CSS",
  };

  return (
    <div className="flex h-full flex-col">
      {/* Header */}
      <div className="flex items-center gap-3 border-b border-[var(--color-border)] px-4 py-3">
        <button onClick={() => navigate("/overlays")}
          className="flex items-center gap-1 text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text)]">
          <ChevronLeft className="h-4 w-4" /> Back
        </button>
        <PageHeader title={`${title} Editor`} helpKey="overlays" />
      </div>

      {/* Main split view */}
      <div className="flex flex-1 overflow-hidden">
        {/* Left: Preview */}
        <div className="flex w-1/2 flex-col border-r border-[var(--color-border)] p-4">
          {/* Static alert preview — always visible in editor */}
          {isAlerts && (
            <div className="mb-3 rounded-lg border border-[var(--color-border)] p-6 checkerboard-bg">
              <AlertEditorPreview config={formValues} />
            </div>
          )}

          {/* Live iframe preview */}
          <div className="flex-1 rounded-lg border border-[var(--color-border)] overflow-hidden checkerboard-bg"
            style={{ minHeight: "250px" }}>
            <iframe
              ref={iframeRef}
              src={`/overlay/${overlayType}?preview=true`}
              className="h-full w-full"
              title="Overlay Preview"
            />
          </div>

          {/* Test buttons + URL */}
          <div className="mt-3 space-y-2">
            {isAlerts && (
              <div className="flex flex-wrap items-center gap-1.5">
                <span className="text-xs text-[var(--color-text-muted)] mr-1">Test:</span>
                {EVENT_TYPES.filter(e => e.testKey).map(ev => (
                  <button key={ev.key} onClick={() => sendTestEvent(ev.testKey)}
                    className="inline-flex items-center gap-1 rounded-md border border-[var(--color-border)] bg-[var(--color-elevated)] px-2 py-1 text-[10px] font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-border)] hover:text-[var(--color-text)] transition-colors">
                    <Play className="h-2.5 w-2.5" /> {ev.icon} {ev.label}
                  </button>
                ))}
              </div>
            )}
            <div className="flex items-center gap-2">
              <button onClick={copyUrl}
                className="flex items-center gap-1.5 rounded border border-[var(--color-border)] px-3 py-1.5 text-xs text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]">
                <Copy className="h-3 w-3" /> Copy OBS URL
              </button>
              <span className="flex-1 truncate text-xs font-mono text-[var(--color-text-muted)]">
                {window.location.origin}/overlay/{overlayType}
              </span>
            </div>
          </div>
        </div>

        {/* Right: Settings */}
        <div className="flex w-1/2 flex-col overflow-y-auto">
          {/* Tabs */}
          <div className="flex border-b border-[var(--color-border)] px-4">
            {tabs.map(tab => (
              <button
                key={tab}
                onClick={() => setActiveTab(tab)}
                className={`px-3 py-2 text-sm font-medium transition-colors ${
                  activeTab === tab
                    ? "border-b-2 border-[var(--color-brand)] text-[var(--color-brand)]"
                    : "text-[var(--color-text-secondary)] hover:text-[var(--color-text)]"
                }`}
              >
                {tabLabels[tab]}
              </button>
            ))}
          </div>

          <div className="flex-1 space-y-4 p-4">
            {/* General Tab */}
            {activeTab === "general" && (
              <>
                <FieldRow label="Font Size (px)">
                  <input type="number" value={formValues.fontSize ?? ""} min={8} max={128}
                    onChange={e => updateField("fontSize", e.target.value)}
                    className="w-24 rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-sm text-[var(--color-text)]" />
                </FieldRow>
                <FontPicker label="Font Family" value={formValues.fontFamily ?? "system-ui"}
                  onChange={v => updateField("fontFamily", v)} />
                <FieldRow label="Text Color">
                  <input type="color" value={formValues.textColor ?? "#ffffff"}
                    onChange={e => updateField("textColor", e.target.value)} className="h-8 w-16 cursor-pointer" />
                </FieldRow>
                {isAlerts && (
                  <>
                    <FieldRow label="Accent Color">
                      <input type="color" value={formValues.accentColor ?? "#8BBF4C"}
                        onChange={e => updateField("accentColor", e.target.value)} className="h-8 w-16 cursor-pointer" />
                    </FieldRow>
                    <FieldRow label="Default Animation">
                      <select value={formValues.animation ?? "slideDown"} onChange={e => updateField("animation", e.target.value)}
                        className="rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-sm text-[var(--color-text)]">
                        {ANIMATIONS.map(a => <option key={a.value} value={a.value}>{a.label}</option>)}
                      </select>
                    </FieldRow>
                    <FieldRow label="Default Duration (ms)">
                      <input type="number" value={formValues.duration ?? "5000"} min={1000} max={30000} step={500}
                        onChange={e => updateField("duration", e.target.value)}
                        className="w-24 rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-sm text-[var(--color-text)]" />
                    </FieldRow>
                  </>
                )}
                {overlayType === "chat" && (
                  <>
                    <FieldRow label="Max Messages">
                      <input type="number" value={formValues.maxMessages ?? "15"} min={1} max={50}
                        onChange={e => updateField("maxMessages", e.target.value)}
                        className="w-24 rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-sm text-[var(--color-text)]" />
                    </FieldRow>
                    <FieldRow label="Fade After (seconds)">
                      <input type="number" value={formValues.fadeAfter ?? "30"} min={0} max={300}
                        onChange={e => updateField("fadeAfter", e.target.value)}
                        className="w-24 rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-sm text-[var(--color-text)]" />
                    </FieldRow>
                  </>
                )}
                {overlayType === "counter" && (
                  <FieldRow label="Label Template">
                    <input type="text" value={formValues.label ?? "{name}: {value}"}
                      onChange={e => updateField("label", e.target.value)}
                      className="w-full rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-sm text-[var(--color-text)] font-mono" />
                  </FieldRow>
                )}
              </>
            )}

            {/* Events Tab (alerts only) */}
            {activeTab === "events" && isAlerts && (
              <div className="space-y-4">
                {EVENT_TYPES.map(ev => (
                  <div key={ev.key} className="rounded-lg border border-[var(--color-border)] p-3 space-y-3">
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium text-[var(--color-text)]">{ev.icon} {ev.label}</span>
                      <label className="flex items-center gap-2">
                        <input type="checkbox"
                          checked={formValues[`${ev.key}.enabled`] !== "false"}
                          onChange={e => updateField(`${ev.key}.enabled`, e.target.checked ? "true" : "false")} />
                        <span className="text-xs text-[var(--color-text-secondary)]">Enabled</span>
                      </label>
                    </div>
                    <AssetPicker category="images" label="Image"
                      value={formValues[`${ev.key}.image`] ?? ""}
                      onChange={v => updateField(`${ev.key}.image`, v)} />
                    <AssetPicker category="sounds" label="Sound"
                      value={formValues[`${ev.key}.sound`] ?? ""}
                      onChange={v => updateField(`${ev.key}.sound`, v)} />
                    {formValues[`${ev.key}.sound`] && (
                      <FieldRow label="Volume">
                        <input type="range" min={0} max={100}
                          value={formValues[`${ev.key}.soundVolume`] ?? "80"}
                          onChange={e => updateField(`${ev.key}.soundVolume`, e.target.value)}
                          className="w-32" />
                        <span className="text-xs text-[var(--color-text-muted)]">{formValues[`${ev.key}.soundVolume`] ?? "80"}%</span>
                      </FieldRow>
                    )}
                    <FieldRow label="Message">
                      <input type="text" value={formValues[`${ev.key}.message`] ?? ""}
                        onChange={e => updateField(`${ev.key}.message`, e.target.value)}
                        className="w-full rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-sm text-[var(--color-text)] font-mono" />
                    </FieldRow>
                    <FieldRow label="Animation Override">
                      <select value={formValues[`${ev.key}.animation`] ?? ""}
                        onChange={e => updateField(`${ev.key}.animation`, e.target.value)}
                        className="rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-sm text-[var(--color-text)]">
                        <option value="">Use global default</option>
                        {ANIMATIONS.map(a => <option key={a.value} value={a.value}>{a.label}</option>)}
                      </select>
                    </FieldRow>
                  </div>
                ))}
              </div>
            )}

            {/* Style Tab */}
            {activeTab === "style" && (
              <div className="space-y-3">
                <p className="text-xs text-[var(--color-text-muted)]">
                  Additional style settings. Changes are applied to the live preview.
                </p>
                <FieldRow label="Text Color">
                  <input type="color" value={formValues.textColor ?? "#ffffff"}
                    onChange={e => updateField("textColor", e.target.value)} className="h-8 w-16 cursor-pointer" />
                </FieldRow>
                {formValues.accentColor !== undefined && (
                  <FieldRow label="Accent Color">
                    <input type="color" value={formValues.accentColor ?? "#8BBF4C"}
                      onChange={e => updateField("accentColor", e.target.value)} className="h-8 w-16 cursor-pointer" />
                  </FieldRow>
                )}
                {formValues.barColor !== undefined && (
                  <FieldRow label="Bar Color">
                    <input type="color" value={formValues.barColor ?? "#8BBF4C"}
                      onChange={e => updateField("barColor", e.target.value)} className="h-8 w-16 cursor-pointer" />
                  </FieldRow>
                )}
              </div>
            )}

            {/* Custom CSS Tab */}
            {activeTab === "customCSS" && (
              <div className="space-y-3">
                <p className="text-xs text-[var(--color-text-muted)]">
                  Custom CSS is applied after the default styles. No !important needed.
                </p>
                <textarea
                  className="h-64 w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-elevated)] p-3 font-mono text-xs text-[var(--color-text)]"
                  value={formValues.customCSS ?? ""}
                  onChange={e => updateField("customCSS", e.target.value)}
                  placeholder={`/* Custom CSS for ${title} */\n\n.overlay-text {\n  text-shadow: 2px 2px 4px rgba(0,0,0,0.5);\n}`}
                  spellCheck={false}
                />
                <div className="rounded bg-[var(--color-elevated)] p-3 text-xs text-[var(--color-text-muted)] space-y-1">
                  <p className="font-medium text-[var(--color-text-secondary)]">Available CSS classes:</p>
                  <p><code>.overlay-text</code> — Text elements</p>
                  {isAlerts && <p><code>.alert-image</code> — Alert images/GIFs</p>}
                  {overlayType === "chat" && <p><code>.chat-message</code>, <code>.chat-username</code></p>}
                  {overlayType === "poll" && <p><code>.poll-bar</code> — Poll bar elements</p>}
                  {overlayType === "counter" && <p><code>.counter-value</code> — Counter number</p>}
                </div>
              </div>
            )}
          </div>

          {/* Save button */}
          <div className="flex items-center gap-2 border-t border-[var(--color-border)] px-4 py-3">
            <button onClick={() => saveMutation.mutate()} disabled={saveMutation.isPending}
              className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-50">
              {saveMutation.isPending ? "Saving..." : "Save"}
            </button>
            <button
              onClick={async () => {
                try {
                  const res = await fetch(`/api/overlays/defaults/${overlayType}`);
                  if (res.ok) {
                    const defaults = await res.json();
                    setFormValues(defaults);
                    sendConfigToPreview(defaults);
                    showToast("success", "Reset to defaults.");
                  }
                } catch {
                  showToast("error", "Failed to load defaults.");
                }
              }}
              className="flex items-center gap-1.5 rounded-lg border border-[var(--color-border)] px-3 py-2 text-sm text-[var(--color-text)] hover:bg-[var(--color-elevated)]"
            >
              <RotateCcw className="h-3.5 w-3.5" /> Reset to Defaults
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function FieldRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-center gap-3">
      <span className="w-36 shrink-0 text-xs font-medium text-[var(--color-text-secondary)]">{label}</span>
      <div className="flex items-center gap-2">{children}</div>
    </div>
  );
}

/** Static alert preview that updates in real-time with editor changes */
function AlertEditorPreview({ config }: { config: OverlaySettings }) {
  const fontSize = config.fontSize || "24";
  const textColor = config.textColor || "#ffffff";
  const fontFamily = config.fontFamily || "system-ui";
  const animation = config.animation || "slideDown";

  // Show follow alert as example, using per-event config if set
  const image = config["follow.image"] || "";
  const message = config["follow.message"] || "{user} just followed!";
  const displayMessage = message.replace("{user}", "TestUser");

  return (
    <div
      key={animation}
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        fontFamily,
        animation: animation !== "none" ? `${animation} 0.6s ease-out` : undefined,
      }}
    >
      {image ? (
        <img src={image} alt="" style={{ maxWidth: "80px", maxHeight: "80px", marginBottom: "12px" }} />
      ) : (
        <div style={{ fontSize: `${Math.round(Number(fontSize) * 1.2)}px`, marginBottom: "12px" }}>
          {"\u2B50"}
        </div>
      )}
      <div style={{
        fontSize: `${fontSize}px`,
        color: textColor,
        fontWeight: 700,
        textAlign: "center",
      }}>
        {displayMessage}
      </div>
    </div>
  );
}
