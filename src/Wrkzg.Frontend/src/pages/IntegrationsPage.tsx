import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Send, Trash2, Check, Wifi, WifiOff } from "lucide-react";
import { integrationsApi } from "../api/integrations";
import type { DiscordStatus, ObsStatus } from "../api/integrations";
import { PageHeader } from "../components/ui/PageHeader";
import { Card } from "../components/ui/Card";
import { Badge } from "../components/ui/Badge";
import { ConfirmDialog } from "../components/ui/ConfirmDialog";
import { showToast } from "../hooks/useToast";

export function IntegrationsPage() {
  const queryClient = useQueryClient();
  const [webhookUrl, setWebhookUrl] = useState("");
  const [showInput, setShowInput] = useState(false);
  const [showRemoveConfirm, setShowRemoveConfirm] = useState(false);

  const { data: discord } = useQuery<DiscordStatus>({
    queryKey: ["integration-discord"],
    queryFn: integrationsApi.getDiscord,
  });

  const saveMutation = useMutation({
    mutationFn: () => integrationsApi.setDiscord(webhookUrl),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["integration-discord"] });
      showToast("success", "Discord webhook configured!");
      setShowInput(false);
      setWebhookUrl("");
    },
    onError: () => showToast("error", "Invalid webhook URL."),
  });

  const removeMutation = useMutation({
    mutationFn: integrationsApi.removeDiscord,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["integration-discord"] });
      showToast("success", "Discord webhook removed.");
    },
    onError: () => showToast("error", "Failed to remove webhook."),
  });

  const testMutation = useMutation({
    mutationFn: integrationsApi.testDiscord,
    onSuccess: (result) => showToast("success", result.message),
    onError: () => showToast("error", "Failed to send test message."),
  });

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Integrations"
        description="Connect external services to your bot."
        helpKey="integrations"
      />

      {/* Discord */}
      <Card title="Discord" headerRight={
        <Badge variant={discord?.configured ? "success" : "default"}>
          {discord?.configured ? "Connected" : "Not Connected"}
        </Badge>
      }>
        <div className="space-y-4">
          <p className="text-sm text-[var(--color-text-secondary)]">
            Send messages and embeds to Discord via webhooks. No bot token needed — just a webhook URL from your Discord server.
          </p>

          <div className="rounded-lg bg-[var(--color-elevated)] p-4 text-sm text-[var(--color-text-secondary)] space-y-2">
            <p className="font-medium text-[var(--color-text)]">How to get a Discord Webhook URL:</p>
            <ol className="list-decimal list-inside space-y-1">
              <li>Open Discord and go to the channel you want to send messages to</li>
              <li>Click the gear icon (Edit Channel) next to the channel name</li>
              <li>Go to <strong>Integrations</strong> &gt; <strong>Webhooks</strong></li>
              <li>Click <strong>New Webhook</strong>, name it (e.g. "Wrkzg Bot")</li>
              <li>Click <strong>Copy Webhook URL</strong></li>
              <li>Paste it below</li>
            </ol>
          </div>

          {discord?.configured && !showInput ? (
            <div className="flex items-center gap-3">
              <div className="flex items-center gap-2 text-sm text-green-400">
                <Check className="h-4 w-4" /> Webhook configured
              </div>
              <button
                onClick={() => testMutation.mutate()}
                disabled={testMutation.isPending}
                className="flex items-center gap-1.5 rounded-lg border border-[var(--color-border)] px-3 py-1.5 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]"
              >
                <Send className="h-3.5 w-3.5" /> Test
              </button>
              <button
                onClick={() => setShowInput(true)}
                className="rounded-lg border border-[var(--color-border)] px-3 py-1.5 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]"
              >
                Change
              </button>
              <button
                onClick={() => setShowRemoveConfirm(true)}
                className="rounded p-1.5 text-[var(--color-error)] hover:bg-[var(--color-elevated)]"
              >
                <Trash2 className="h-4 w-4" />
              </button>
            </div>
          ) : (
            <div className="space-y-2">
              <input
                type="url"
                value={webhookUrl}
                onChange={(e) => setWebhookUrl(e.target.value)}
                placeholder="https://discord.com/api/webhooks/..."
                className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)] font-mono"
              />
              <div className="flex gap-2">
                <button
                  onClick={() => saveMutation.mutate()}
                  disabled={!webhookUrl.trim() || saveMutation.isPending}
                  className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-50"
                >
                  Save
                </button>
                {discord?.configured && (
                  <button
                    onClick={() => { setShowInput(false); setWebhookUrl(""); }}
                    className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)]"
                  >
                    Cancel
                  </button>
                )}
              </div>
            </div>
          )}

          <div className="border-t border-[var(--color-border)] pt-3">
            <p className="text-xs text-[var(--color-text-muted)]">
              Once configured, use <code className="bg-[var(--color-elevated)] px-1 rounded">discord.send_message</code> and{" "}
              <code className="bg-[var(--color-elevated)] px-1 rounded">discord.send_embed</code> in your{" "}
              <a href="/effects" className="underline hover:text-[var(--color-text)]">Automations</a> to send messages to Discord.
            </p>
          </div>
        </div>
      </Card>

      <ConfirmDialog
        open={showRemoveConfirm}
        title="Remove Webhook"
        message="Remove the Discord webhook? You can re-add it later."
        onConfirm={() => { removeMutation.mutate(); setShowRemoveConfirm(false); }}
        onCancel={() => setShowRemoveConfirm(false)}
      />

      {/* OBS WebSocket */}
      <ObsCard />
    </div>
  );
}

// ─── OBS WebSocket Card ────────────────────────────────────

function ObsCard() {
  const queryClient = useQueryClient();
  const [obsHost, setObsHost] = useState("localhost");
  const [obsPort, setObsPort] = useState("4455");
  const [obsPassword, setObsPassword] = useState("");
  const [showObsRemoveConfirm, setShowObsRemoveConfirm] = useState(false);

  const { data: obs } = useQuery<ObsStatus>({
    queryKey: ["integration-obs"],
    queryFn: integrationsApi.getObs,
    refetchInterval: 5000,
  });

  const saveMutation = useMutation({
    mutationFn: () => integrationsApi.setObs(obsHost, parseInt(obsPort, 10) || 4455, obsPassword || undefined),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["integration-obs"] });
      showToast("success", "OBS settings saved!");
    },
    onError: () => showToast("error", "Failed to save OBS settings."),
  });

  const connectMutation = useMutation({
    mutationFn: integrationsApi.connectObs,
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ["integration-obs"] });
      if (result.connected) {
        showToast("success", "Connected to OBS!");
      } else {
        showToast("error", "Failed to connect to OBS. Check settings and ensure OBS is running.");
      }
    },
    onError: () => showToast("error", "Failed to connect to OBS."),
  });

  const disconnectMutation = useMutation({
    mutationFn: integrationsApi.disconnectObs,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["integration-obs"] });
      showToast("success", "Disconnected from OBS.");
    },
    onError: () => showToast("error", "Failed to disconnect."),
  });

  const removeMutation = useMutation({
    mutationFn: integrationsApi.removeObs,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["integration-obs"] });
      showToast("success", "OBS settings removed.");
      setObsHost("localhost");
      setObsPort("4455");
      setObsPassword("");
    },
    onError: () => showToast("error", "Failed to remove OBS settings."),
  });

  return (
    <>
      <Card title="OBS WebSocket" headerRight={
        <Badge variant={obs?.isConnected ? "success" : obs?.isConfigured ? "warning" : "default"}>
          {obs?.isConnected ? (
            <span className="flex items-center gap-1"><Wifi className="h-3 w-3" /> Connected</span>
          ) : (
            <span className="flex items-center gap-1"><WifiOff className="h-3 w-3" /> Not Connected</span>
          )}
        </Badge>
      }>
        <div className="space-y-4">
          <p className="text-sm text-[var(--color-text-secondary)]">
            Connect to OBS Studio to switch scenes and toggle sources from automations and hotkeys.
          </p>

          {obs?.isConnected && (
            <div className="rounded-lg bg-[var(--color-elevated)] border border-[var(--color-border)] p-3 space-y-1">
              {obs.obsVersion && (
                <p className="text-xs text-[var(--color-text-muted)]">OBS Version: <span className="text-[var(--color-text)]">{obs.obsVersion}</span></p>
              )}
              {obs.currentScene && (
                <p className="text-xs text-[var(--color-text-muted)]">Current Scene: <span className="text-[var(--color-text)]">{obs.currentScene}</span></p>
              )}
            </div>
          )}

          <div className="grid grid-cols-[1fr_100px] gap-3">
            <div>
              <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Host</label>
              <input
                type="text"
                value={obsHost}
                onChange={(e) => setObsHost(e.target.value)}
                placeholder="localhost"
                className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Port</label>
              <input
                type="text"
                value={obsPort}
                onChange={(e) => setObsPort(e.target.value)}
                placeholder="4455"
                className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Password (optional)</label>
            <input
              type="password"
              value={obsPassword}
              onChange={(e) => setObsPassword(e.target.value)}
              placeholder="Leave empty if no password set"
              className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
            />
          </div>

          <div className="flex items-center gap-2 flex-wrap">
            <button
              onClick={() => saveMutation.mutate()}
              disabled={!obsHost.trim() || saveMutation.isPending}
              className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-50"
            >
              Save
            </button>
            {obs?.isConfigured && !obs.isConnected && (
              <button
                onClick={() => connectMutation.mutate()}
                disabled={connectMutation.isPending}
                className="flex items-center gap-1.5 rounded-lg border border-[var(--color-border)] px-3 py-2 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]"
              >
                <Wifi className="h-3.5 w-3.5" /> Connect
              </button>
            )}
            {obs?.isConnected && (
              <button
                onClick={() => disconnectMutation.mutate()}
                disabled={disconnectMutation.isPending}
                className="flex items-center gap-1.5 rounded-lg border border-[var(--color-border)] px-3 py-2 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]"
              >
                <WifiOff className="h-3.5 w-3.5" /> Disconnect
              </button>
            )}
            {obs?.isConfigured && (
              <button
                onClick={() => setShowObsRemoveConfirm(true)}
                className="rounded p-1.5 text-[var(--color-error)] hover:bg-[var(--color-elevated)]"
                title="Remove OBS settings"
              >
                <Trash2 className="h-4 w-4" />
              </button>
            )}
          </div>

          <div className="border-t border-[var(--color-border)] pt-3">
            <p className="text-xs text-[var(--color-text-muted)]">
              Once configured, use <code className="bg-[var(--color-elevated)] px-1 rounded">obs.scene_switch</code> and{" "}
              <code className="bg-[var(--color-elevated)] px-1 rounded">obs.source_toggle</code> in your{" "}
              <a href="/effects" className="underline hover:text-[var(--color-text)]">Automations</a> to control OBS.
            </p>
          </div>
        </div>
      </Card>

      <ConfirmDialog
        open={showObsRemoveConfirm}
        title="Remove OBS Settings"
        message="Remove the OBS WebSocket connection settings? You can re-add them later."
        onConfirm={() => { removeMutation.mutate(); setShowObsRemoveConfirm(false); }}
        onCancel={() => setShowObsRemoveConfirm(false)}
      />
    </>
  );
}
