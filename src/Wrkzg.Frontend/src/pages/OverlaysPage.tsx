import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { Bell, MessageSquare, BarChart3, Gift, Hash, List, Music, Plus, Code, Copy, Trash2 } from "lucide-react";
import { PageHeader } from "../components/ui/PageHeader";
import { Card } from "../components/ui/Card";
import { Badge } from "../components/ui/Badge";
import { OverlayCard } from "../components/features/overlays/OverlayCard";
import type { OverlayDefinition } from "../components/features/overlays/OverlayCard";
import { customOverlaysApi } from "../api/customOverlays";
import type { CustomOverlay } from "../api/customOverlays";
import { showToast } from "../hooks/useToast";

const OVERLAYS: OverlayDefinition[] = [
  {
    type: "alerts",
    title: "Alert Box",
    description: "Animated notifications for follows, subs, and raids.",
    icon: Bell,
    width: 800,
    height: 200,
    testEvents: ["follow", "subscribe", "giftsub", "resub", "raid"],
  },
  {
    type: "chat",
    title: "Chat Box",
    description: "Live chat display for your stream.",
    icon: MessageSquare,
    width: 400,
    height: 600,
  },
  {
    type: "poll",
    title: "Poll",
    description: "Live poll results with animated bars.",
    icon: BarChart3,
    width: 600,
    height: 300,
  },
  {
    type: "raffle",
    title: "Raffle",
    description: "Animated raffle draws and winner reveals.",
    icon: Gift,
    width: 600,
    height: 300,
  },
  {
    type: "counter",
    title: "Counter",
    description: "Display a counter value on your stream.",
    icon: Hash,
    width: 300,
    height: 80,
    testEvents: ["counter"],
  },
  {
    type: "events",
    title: "Event List",
    description: "Recent events feed for your stream.",
    icon: List,
    width: 350,
    height: 400,
    testEvents: ["follow", "subscribe", "raid"],
  },
  {
    type: "song-player",
    title: "Song Player",
    description: "Now-playing overlay for song requests. Add ?mode=slim for compact.",
    icon: Music,
    width: 440,
    height: 100,
  },
];

const CUSTOM_TEMPLATES = [
  {
    name: "Follow Goal Bar",
    description: "A progress bar that tracks follower goals with animated fill",
    html: '<div id="goal">\n  <div id="label">Follower Goal</div>\n  <div id="bar"><div id="fill"></div></div>\n  <div id="count">0 / 100</div>\n</div>',
    css: '#goal { font-family: system-ui; color: white; padding: 16px; }\n#label { font-size: 14px; margin-bottom: 8px; opacity: 0.8; }\n#bar { background: rgba(255,255,255,0.15); border-radius: 8px; height: 24px; overflow: hidden; }\n#fill { background: linear-gradient(90deg, #8BBF4C, #6da832); height: 100%; width: 0%; transition: width 0.5s ease; border-radius: 8px; }\n#count { font-size: 12px; margin-top: 4px; text-align: right; opacity: 0.7; }',
    js: 'let count = 0;\nconst goal = 100;\n\nfunction update() {\n  document.getElementById("fill").style.width = (count / goal * 100) + "%";\n  document.getElementById("count").textContent = count + " / " + goal;\n}\n\nWrkzg.on("FollowEvent", function() {\n  count++;\n  update();\n});\n\nupdate();',
    width: 400,
    height: 80,
  },
  {
    name: "Recent Follower Ticker",
    description: "Scrolling ticker showing the most recent followers",
    html: '<div id="ticker">\n  <span id="label">Latest Followers:</span>\n  <span id="names">Waiting for followers...</span>\n</div>',
    css: '#ticker { font-family: system-ui; color: white; font-size: 14px; padding: 8px 16px; display: flex; gap: 8px; align-items: center; white-space: nowrap; overflow: hidden; }\n#label { opacity: 0.6; font-size: 12px; flex-shrink: 0; }\n#names { animation: scroll 20s linear infinite; }\n@keyframes scroll { from { transform: translateX(100%); } to { transform: translateX(-100%); } }',
    js: 'const followers = [];\n\nWrkzg.on("FollowEvent", function(data) {\n  followers.push(data.username);\n  if (followers.length > 10) followers.shift();\n  document.getElementById("names").textContent = followers.join(" \\u2022 ");\n});',
    width: 600,
    height: 40,
  },
  {
    name: "Stream Clock",
    description: "A simple clock overlay showing current time",
    html: '<div id="clock"></div>',
    css: '#clock { font-family: "JetBrains Mono", monospace; color: white; font-size: 32px; padding: 12px 24px; text-shadow: 0 2px 8px rgba(0,0,0,0.5); }',
    js: 'function tick() {\n  const now = new Date();\n  document.getElementById("clock").textContent = now.toLocaleTimeString();\n}\ntick();\nsetInterval(tick, 1000);',
    width: 250,
    height: 60,
  },
  {
    name: "Sub Counter with Effects",
    description: "Animated subscriber counter with celebration effects",
    html: '<div id="sub-counter">\n  <div id="icon">\\uD83D\\uDC9C</div>\n  <div id="value">0</div>\n  <div id="label">Subs Today</div>\n  <div id="celebration" class="hidden">+1</div>\n</div>',
    css: '#sub-counter { font-family: system-ui; color: white; text-align: center; padding: 20px; }\n#icon { font-size: 32px; margin-bottom: 8px; }\n#value { font-size: 48px; font-weight: 700; }\n#label { font-size: 12px; opacity: 0.6; margin-top: 4px; }\n#celebration { position: absolute; top: 10px; right: 10px; font-size: 24px; color: #a855f7; animation: popIn 0.5s ease-out forwards; }\n#celebration.hidden { display: none; }\n@keyframes popIn { 0% { transform: scale(0); opacity: 1; } 100% { transform: scale(1.5) translateY(-20px); opacity: 0; } }',
    js: 'let subs = 0;\n\nWrkzg.on("SubscribeEvent", function() {\n  subs++;\n  document.getElementById("value").textContent = subs;\n  const el = document.getElementById("celebration");\n  el.classList.remove("hidden");\n  setTimeout(function() { el.classList.add("hidden"); }, 600);\n});',
    width: 200,
    height: 150,
  },
  {
    name: "Raid Alert Banner",
    description: "Full-width banner that slides in when a raid happens",
    html: '<div id="raid-banner" class="hidden">\n  <span id="raid-text"></span>\n</div>',
    css: '#raid-banner { font-family: system-ui; background: linear-gradient(90deg, #8BBF4C, #e8a100); color: white; padding: 12px 24px; font-size: 20px; font-weight: 700; text-align: center; animation: slideDown 0.5s ease-out; }\n#raid-banner.hidden { display: none; }\n@keyframes slideDown { from { transform: translateY(-100%); } to { transform: translateY(0); } }',
    js: 'Wrkzg.on("RaidEvent", function(data) {\n  const banner = document.getElementById("raid-banner");\n  document.getElementById("raid-text").textContent = "\\u2694\\uFE0F " + data.username + " is raiding with " + data.viewers + " viewers!";\n  banner.classList.remove("hidden");\n  setTimeout(function() { banner.classList.add("hidden"); }, 8000);\n});',
    width: 800,
    height: 50,
  },
];

export function OverlaysPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [deleteId, setDeleteId] = useState<number | null>(null);
  const [showHowItWorks, setShowHowItWorks] = useState(false);

  const { data: customOverlays } = useQuery<CustomOverlay[]>({
    queryKey: ["custom-overlays"],
    queryFn: customOverlaysApi.getAll,
  });

  const createMutation = useMutation({
    mutationFn: (template?: typeof CUSTOM_TEMPLATES[0]) => customOverlaysApi.create({
      name: template?.name ?? "New Custom Overlay",
      description: template?.description,
      html: template?.html ?? '<div id="widget">\n  <h1>Hello World</h1>\n</div>',
      css: template?.css ?? "#widget { color: white; text-align: center; padding: 20px; }",
      javaScript: template?.js ?? '// Your code here\nWrkzg.on("FollowEvent", function(data) {\n  console.log(data.username + " followed!");\n});',
      width: template?.width ?? 800,
      height: template?.height ?? 600,
    }),
    onSuccess: (overlay) => {
      queryClient.invalidateQueries({ queryKey: ["custom-overlays"] });
      navigate(`/overlays/custom/${overlay.id}/edit`);
      showToast("success", "Custom overlay created!");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => customOverlaysApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["custom-overlays"] });
      showToast("success", "Custom overlay deleted.");
      setDeleteId(null);
    },
  });

  function copyUrl(id: number) {
    const url = `${window.location.origin}/overlay/custom/${id}`;
    navigator.clipboard.writeText(url);
    showToast("success", "URL copied!");
  }

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Overlays"
        description="Add browser sources in OBS to display overlays on your stream."
        helpKey="overlays"
      />

      {/* Built-in Overlays */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        {OVERLAYS.map((overlay) => (
          <OverlayCard key={overlay.type} overlay={overlay} />
        ))}
      </div>

      {/* Custom Overlays */}
      <div className="border-t border-[var(--color-border)] pt-6">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h2 className="flex items-center gap-2 text-lg font-semibold text-[var(--color-text)]">
              <Code className="h-5 w-5" /> Custom Overlays
              <Badge variant="default">Developer</Badge>
            </h2>
            <p className="text-sm text-[var(--color-text-secondary)]">
              Create your own overlays with HTML, CSS, and JavaScript. Full access to real-time stream events.
            </p>
          </div>
          <div className="flex items-center gap-2">
            <button onClick={() => setShowHowItWorks(!showHowItWorks)}
              className="rounded-lg border border-[var(--color-border)] px-3 py-2 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]">
              {showHowItWorks ? "Hide" : "How it works"}
            </button>
            <button
              onClick={() => createMutation.mutate(undefined)}
              disabled={createMutation.isPending}
              className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-3 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)]"
            >
              <Plus className="h-4 w-4" /> Blank Overlay
            </button>
          </div>
        </div>

        {/* How it works */}
        {showHowItWorks && (
          <div className="mb-4 rounded-lg bg-[var(--color-elevated)] p-4 space-y-3 text-sm">
            <p className="font-medium text-[var(--color-text)]">How Custom Overlays Work</p>
            <div className="grid grid-cols-3 gap-4 text-[var(--color-text-secondary)]">
              <div>
                <p className="font-medium text-[var(--color-text)] mb-1">1. Write Code</p>
                <p>Write HTML for structure, CSS for styling, and JavaScript for logic. Each tab in the editor handles one part.</p>
              </div>
              <div>
                <p className="font-medium text-[var(--color-text)] mb-1">2. React to Events</p>
                <p>Use <code className="bg-[var(--color-surface)] px-1 rounded">Wrkzg.on('FollowEvent', callback)</code> to react to follows, subs, raids, and more in real-time.</p>
              </div>
              <div>
                <p className="font-medium text-[var(--color-text)] mb-1">3. Add to OBS</p>
                <p>Copy the overlay URL and add it as a Browser Source in OBS. Your custom overlay runs alongside the built-in ones.</p>
              </div>
            </div>
            <div className="border-t border-[var(--color-border)] pt-3 space-y-1 text-xs text-[var(--color-text-muted)]">
              <p><strong>Available Events:</strong> FollowEvent, SubscribeEvent, GiftSubEvent, ResubEvent, RaidEvent, ChannelPointRedemption, ChatMessage, CounterUpdated, StreamOnline</p>
              <p><strong>API:</strong> <code>Wrkzg.on(event, callback)</code> — listen for events | <code>Wrkzg.getField(key)</code> — read configured field values</p>
              <p><strong>Everything runs locally</strong> — no cloud, no upload limits, no external dependencies. Your overlays load instantly from localhost.</p>
              <p>For a full guide with examples, see the <strong>Custom Overlays</strong> section in the <a href="https://github.com/wrkzg-korvar/wrkzg-twitchbot/blob/main/_docs/HANDBOOK.md#811-custom-overlays-developer-mode" className="underline hover:text-[var(--color-text)]">Handbook</a>.</p>
            </div>
          </div>
        )}

        {/* Templates */}
        <div className="mb-4">
          <p className="text-xs font-medium text-[var(--color-text-secondary)] mb-2">Start from a template:</p>
          <div className="grid grid-cols-2 gap-2 lg:grid-cols-3">
            {CUSTOM_TEMPLATES.map((t) => (
              <button
                key={t.name}
                onClick={() => createMutation.mutate(t)}
                disabled={createMutation.isPending}
                className="rounded-lg border border-[var(--color-border)] p-3 text-left hover:bg-[var(--color-elevated)] transition-colors"
              >
                <div className="text-sm font-medium text-[var(--color-text)]">{t.name}</div>
                <div className="mt-0.5 text-xs text-[var(--color-text-muted)]">{t.description}</div>
                <div className="mt-1 text-[10px] text-[var(--color-text-muted)]">{t.width} x {t.height}</div>
              </button>
            ))}
          </div>
        </div>

        {/* Existing custom overlays */}
        {customOverlays && customOverlays.length > 0 && (
          <div className="space-y-3">
            <p className="text-xs font-medium text-[var(--color-text-secondary)]">Your Custom Overlays:</p>
            {customOverlays.map((co) => (
              <Card key={co.id}>
                <div className="flex items-center justify-between">
                  <div className="min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="font-medium text-[var(--color-text)]">{co.name}</span>
                      <Badge variant={co.isEnabled ? "success" : "default"}>
                        {co.isEnabled ? "Active" : "Disabled"}
                      </Badge>
                    </div>
                    {co.description && (
                      <p className="text-xs text-[var(--color-text-muted)] mt-0.5">{co.description}</p>
                    )}
                    <code className="text-xs text-[var(--color-text-secondary)] font-mono">
                      {window.location.origin}/overlay/custom/{co.id}
                    </code>
                  </div>
                  <div className="flex items-center gap-1.5">
                    <button onClick={() => copyUrl(co.id)} title="Copy URL"
                      className="rounded p-1.5 text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]">
                      <Copy className="h-4 w-4" />
                    </button>
                    <button onClick={() => navigate(`/overlays/custom/${co.id}/edit`)} title="Edit Code"
                      className="rounded p-1.5 text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]">
                      <Code className="h-4 w-4" />
                    </button>
                    <button onClick={() => setDeleteId(co.id)} title="Delete"
                      className="rounded p-1.5 text-[var(--color-error)] hover:bg-[var(--color-elevated)]">
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </Card>
            ))}
          </div>
        )}
      </div>

      {/* Delete confirmation */}
      {deleteId !== null && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="rounded-lg bg-[var(--color-surface)] p-6 shadow-xl max-w-sm">
            <p className="text-sm text-[var(--color-text)]">Delete this custom overlay? This cannot be undone.</p>
            <div className="mt-4 flex justify-end gap-2">
              <button onClick={() => setDeleteId(null)}
                className="rounded px-3 py-1.5 text-sm text-[var(--color-text-secondary)]">Cancel</button>
              <button onClick={() => deleteMutation.mutate(deleteId)}
                className="rounded bg-[var(--color-error)] px-3 py-1.5 text-sm text-white">Delete</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
