import { useState, useEffect, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { ChevronLeft, Copy, RefreshCw, ExternalLink } from "lucide-react";
import { customOverlaysApi } from "../api/customOverlays";
import type { CustomOverlay } from "../api/customOverlays";
import { showToast } from "../hooks/useToast";

const CODE_TABS = ["html", "css", "js", "fields"] as const;
type CodeTab = typeof CODE_TABS[number];

export function CustomOverlayEditorPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const [activeTab, setActiveTab] = useState<"code" | "settings">("code");
  const [codeTab, setCodeTab] = useState<CodeTab>("html");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [html, setHtml] = useState("");
  const [css, setCss] = useState("");
  const [js, setJs] = useState("");
  const [fieldDefs, setFieldDefs] = useState("{}");
  const [fieldValues, setFieldValues] = useState("{}");
  const [width, setWidth] = useState(800);
  const [height, setHeight] = useState(600);

  const overlayId = Number(id);

  const { data: overlay } = useQuery<CustomOverlay>({
    queryKey: ["custom-overlay", overlayId],
    queryFn: () => customOverlaysApi.getById(overlayId),
    enabled: !isNaN(overlayId),
  });

  useEffect(() => {
    if (overlay) {
      setName(overlay.name);
      setDescription(overlay.description ?? "");
      setHtml(overlay.html);
      setCss(overlay.css);
      setJs(overlay.javaScript);
      setFieldDefs(overlay.fieldDefinitions);
      setFieldValues(overlay.fieldValues);
      setWidth(overlay.width);
      setHeight(overlay.height);
    }
  }, [overlay]);

  const saveMutation = useMutation({
    mutationFn: () => customOverlaysApi.update(overlayId, {
      name, description, html, css,
      javaScript: js,
      fieldDefinitions: fieldDefs,
      fieldValues: fieldValues,
      width, height,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["custom-overlay", overlayId] });
      queryClient.invalidateQueries({ queryKey: ["custom-overlays"] });
      showToast("success", "Saved!");
    },
    onError: () => showToast("error", "Failed to save."),
  });

  function refreshPreview() {
    if (iframeRef.current) {
      iframeRef.current.src = iframeRef.current.src;
    }
  }

  function copyUrl() {
    const url = `${window.location.origin}/overlay/custom/${overlayId}`;
    navigator.clipboard.writeText(url);
    showToast("success", "URL copied!");
  }

  const codeValue = { html, css, js, fields: fieldDefs }[codeTab];
  const codeOnChange = {
    html: setHtml, css: setCss, js: setJs, fields: setFieldDefs,
  }[codeTab];

  return (
    <div className="flex h-full flex-col">
      {/* Header */}
      <div className="flex items-center gap-3 border-b border-[var(--color-border)] px-4 py-3">
        <button onClick={() => navigate("/overlays")}
          className="flex items-center gap-1 text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text)]">
          <ChevronLeft className="h-4 w-4" /> Back
        </button>
        <div className="flex-1">
          <input
            type="text"
            value={name}
            onChange={e => setName(e.target.value)}
            className="bg-transparent text-lg font-semibold text-[var(--color-text)] outline-none w-full"
            placeholder="Overlay Name"
          />
        </div>
        <button onClick={() => saveMutation.mutate()} disabled={saveMutation.isPending}
          className="rounded-lg bg-[var(--color-brand)] px-4 py-1.5 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-50">
          {saveMutation.isPending ? "Saving..." : "Save"}
        </button>
      </div>

      {/* Split view */}
      <div className="flex flex-1 overflow-hidden">
        {/* Left: Preview */}
        <div className="flex w-1/2 flex-col border-r border-[var(--color-border)] p-4">
          <div className="flex-1 rounded-lg border border-[var(--color-border)] overflow-hidden checkerboard-bg">
            <iframe
              ref={iframeRef}
              src={`/overlay/custom/${overlayId}?preview=true`}
              className="h-full w-full"
              title="Custom Overlay Preview"
            />
          </div>
          <div className="mt-3 flex items-center gap-2">
            <button onClick={refreshPreview}
              className="flex items-center gap-1 rounded border border-[var(--color-border)] px-2 py-1 text-xs text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]">
              <RefreshCw className="h-3 w-3" /> Refresh
            </button>
            <button onClick={() => window.open(`/overlay/custom/${overlayId}`, "_blank")}
              className="flex items-center gap-1 rounded border border-[var(--color-border)] px-2 py-1 text-xs text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]">
              <ExternalLink className="h-3 w-3" /> Open
            </button>
            <button onClick={copyUrl}
              className="flex items-center gap-1 rounded border border-[var(--color-border)] px-2 py-1 text-xs text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]">
              <Copy className="h-3 w-3" /> Copy URL
            </button>
          </div>
        </div>

        {/* Right: Editor */}
        <div className="flex w-1/2 flex-col overflow-y-auto">
          {/* Main tabs */}
          <div className="flex border-b border-[var(--color-border)] px-4">
            {(["code", "settings"] as const).map(tab => (
              <button key={tab} onClick={() => setActiveTab(tab)}
                className={`px-3 py-2 text-sm font-medium capitalize ${
                  activeTab === tab
                    ? "border-b-2 border-[var(--color-brand)] text-[var(--color-brand)]"
                    : "text-[var(--color-text-secondary)] hover:text-[var(--color-text)]"
                }`}>
                {tab}
              </button>
            ))}
          </div>

          {activeTab === "code" && (
            <div className="flex flex-1 flex-col">
              {/* Code sub-tabs */}
              <div className="flex gap-1 border-b border-[var(--color-border)] px-4 py-1">
                {CODE_TABS.map(tab => (
                  <button key={tab} onClick={() => setCodeTab(tab)}
                    className={`rounded px-2 py-1 text-xs font-mono uppercase ${
                      codeTab === tab
                        ? "bg-[var(--color-brand-subtle)] text-[var(--color-brand-text)]"
                        : "text-[var(--color-text-muted)] hover:text-[var(--color-text)]"
                    }`}>
                    {tab === "fields" ? "Fields (JSON)" : tab}
                  </button>
                ))}
              </div>
              <textarea
                className="flex-1 resize-none border-none bg-[var(--color-elevated)] p-4 font-mono text-xs text-[var(--color-text)] outline-none"
                value={codeValue}
                onChange={e => codeOnChange(e.target.value)}
                spellCheck={false}
                placeholder={codeTab === "html" ? "<div>Your overlay HTML</div>"
                  : codeTab === "css" ? "/* Your CSS */"
                  : codeTab === "js" ? "// Your JavaScript\nWrkzg.on('FollowEvent', (data) => {\n  console.log(data.username + ' followed!');\n});"
                  : '{\n  "title": {\n    "type": "text",\n    "label": "Title",\n    "value": "My Widget"\n  }\n}'}
              />
            </div>
          )}

          {activeTab === "settings" && (
            <div className="space-y-4 p-4">
              <div>
                <label className="mb-1 block text-xs font-medium text-[var(--color-text-secondary)]">Description</label>
                <input type="text" value={description} onChange={e => setDescription(e.target.value)}
                  className="w-full rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-sm text-[var(--color-text)]"
                  placeholder="Short description" />
              </div>
              <div className="flex gap-4">
                <div>
                  <label className="mb-1 block text-xs font-medium text-[var(--color-text-secondary)]">Width</label>
                  <input type="number" value={width} onChange={e => setWidth(Number(e.target.value))}
                    className="w-24 rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-sm text-[var(--color-text)]" />
                </div>
                <div>
                  <label className="mb-1 block text-xs font-medium text-[var(--color-text-secondary)]">Height</label>
                  <input type="number" value={height} onChange={e => setHeight(Number(e.target.value))}
                    className="w-24 rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1.5 text-sm text-[var(--color-text)]" />
                </div>
              </div>

              <div className="rounded bg-[var(--color-elevated)] p-3 text-xs text-[var(--color-text-muted)] space-y-1">
                <p className="font-medium text-[var(--color-text-secondary)]">Wrkzg API (available in JS):</p>
                <p><code>Wrkzg.on('EventName', callback)</code> — Listen for SignalR events</p>
                <p><code>Wrkzg.getField('key')</code> — Get a field value from Settings tab</p>
                <p className="mt-2 font-medium text-[var(--color-text-secondary)]">Available Events:</p>
                <p>FollowEvent, SubscribeEvent, GiftSubEvent, ResubEvent, RaidEvent, ChannelPointRedemption, ChatMessage, CounterUpdated, StreamOnline</p>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
