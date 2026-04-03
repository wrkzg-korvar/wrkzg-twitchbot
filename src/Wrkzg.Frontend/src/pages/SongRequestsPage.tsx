import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Music, SkipForward, Trash2, XCircle, Lock, Unlock, MessageSquare, Settings, RotateCcw, Save } from "lucide-react";
import { useSignalR } from "../hooks/useSignalR";
import { songRequestsApi } from "../api/songRequests";
import type { SongRequestStatus as SRStatus, SongRequestMessages } from "../api/songRequests";
import { PageHeader } from "../components/ui/PageHeader";
import { Card } from "../components/ui/Card";
import { Badge } from "../components/ui/Badge";
import { Modal } from "../components/ui/Modal";
import { EmptyState } from "../components/ui/EmptyState";
import { showToast } from "../hooks/useToast";
import type { SongRequest } from "../types/songRequests";

function formatDuration(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}:${s.toString().padStart(2, "0")}`;
}

export function SongRequestsPage() {
  const queryClient = useQueryClient();
  const { isConnected, on, off } = useSignalR("/hubs/chat");
  const [showMessages, setShowMessages] = useState(false);
  const [showSettings, setShowSettings] = useState(false);

  const { data: queue, isLoading: queueLoading } = useQuery<SongRequest[]>({
    queryKey: ["song-queue"],
    queryFn: songRequestsApi.getQueue,
    refetchInterval: 5000,
  });

  const { data: status } = useQuery<SRStatus>({
    queryKey: ["song-status"],
    queryFn: songRequestsApi.getStatus,
  });

  useEffect(() => {
    if (!isConnected) return;
    on("SongQueueUpdated", () => {
      queryClient.invalidateQueries({ queryKey: ["song-queue"] });
      queryClient.invalidateQueries({ queryKey: ["song-status"] });
    });
    return () => { off("SongQueueUpdated"); };
  }, [isConnected, on, off, queryClient]);

  const skipMutation = useMutation({
    mutationFn: songRequestsApi.skip,
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ["song-queue"] });
      showToast("success", result.message);
    },
  });

  const removeMutation = useMutation({
    mutationFn: (id: number) => songRequestsApi.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["song-queue"] });
      showToast("success", "Song removed.");
    },
  });

  const clearMutation = useMutation({
    mutationFn: songRequestsApi.clearQueue,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["song-queue"] });
      showToast("success", "Queue cleared.");
    },
  });

  const toggleMutation = useMutation({
    mutationFn: songRequestsApi.toggle,
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ["song-status"] });
      showToast("success", result.queueOpen ? "Queue opened." : "Queue closed.");
    },
  });

  const playNextMutation = useMutation({
    mutationFn: songRequestsApi.playNext,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["song-queue"] }),
  });

  const allSongs = queue ?? [];
  const currentSong = allSongs.find((s) => s.status === 1);
  const queuedSongs = allSongs.filter((s) => s.status === 0);
  const isOpen = status?.queueOpen ?? true;

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Song Requests"
        description="Manage the song request queue. Viewers use !sr to request songs."
        helpKey="song-requests"
      />

      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-3">
        <button
          onClick={() => toggleMutation.mutate()}
          className={`flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${
            isOpen
              ? "bg-green-500/15 text-green-400 hover:bg-green-500/25"
              : "bg-red-500/15 text-red-400 hover:bg-red-500/25"
          }`}
        >
          {isOpen ? <Unlock className="h-4 w-4" /> : <Lock className="h-4 w-4" />}
          {isOpen ? "Queue Open" : "Queue Closed"}
        </button>

        <div className="h-5 w-px bg-[var(--color-border)]" />

        <button
          onClick={() => skipMutation.mutate()}
          disabled={!currentSong || skipMutation.isPending}
          className="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)] disabled:opacity-40 transition-colors"
        >
          <SkipForward className="h-4 w-4" /> Skip
        </button>

        <button
          onClick={() => playNextMutation.mutate()}
          disabled={queuedSongs.length === 0 || playNextMutation.isPending}
          className="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)] disabled:opacity-40 transition-colors"
        >
          <SkipForward className="h-4 w-4" /> Play Next
        </button>

        <button
          onClick={() => clearMutation.mutate()}
          disabled={queuedSongs.length === 0 || clearMutation.isPending}
          className="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)] disabled:opacity-40 transition-colors"
        >
          <XCircle className="h-4 w-4" /> Clear
        </button>

        <div className="flex-1" />

        <button
          onClick={() => setShowMessages(true)}
          className="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)] transition-colors"
        >
          <MessageSquare className="h-4 w-4" /> Messages
        </button>

        <button
          onClick={() => setShowSettings(true)}
          className="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)] transition-colors"
        >
          <Settings className="h-4 w-4" /> Settings
        </button>

        {status && (
          <div className="text-xs text-[var(--color-text-muted)]">
            {status.queueCount} in queue
            {status.pointsCost > 0 && ` · ${status.pointsCost} pts/song`}
          </div>
        )}
      </div>

      {/* Now Playing */}
      {currentSong && (
        <Card title="Now Playing" headerRight={<Badge variant="brand">Playing</Badge>}>
          <div className="flex items-center gap-4">
            {currentSong.thumbnailUrl && (
              <img src={currentSong.thumbnailUrl} alt="" className="h-16 w-28 rounded object-cover flex-shrink-0" />
            )}
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-[var(--color-text)] truncate">{currentSong.title}</p>
              <p className="text-xs text-[var(--color-text-muted)]">
                Requested by {currentSong.requestedBy} · {formatDuration(currentSong.durationSeconds)}
              </p>
            </div>
          </div>
        </Card>
      )}

      {/* Queue */}
      {queueLoading && allSongs.length === 0 && (
        <div className="text-center py-8 text-sm text-[var(--color-text-muted)]">Loading queue...</div>
      )}

      {!queueLoading && allSongs.length === 0 && (
        <EmptyState
          icon={Music}
          title="No songs in the queue"
          description="Viewers can request songs with !sr <YouTube URL> in chat."
        />
      )}

      {queuedSongs.length > 0 && (
        <Card title={`Queue (${queuedSongs.length})`}>
          <div className="space-y-2">
            {queuedSongs.map((song, i) => (
              <div key={song.id} className="flex items-center gap-3 rounded-lg border border-[var(--color-border)] p-3">
                <span className="text-xs font-bold text-[var(--color-text-muted)] w-6 text-center">#{i + 1}</span>
                {song.thumbnailUrl && (
                  <img src={song.thumbnailUrl} alt="" className="h-10 w-16 rounded object-cover flex-shrink-0" />
                )}
                <div className="flex-1 min-w-0">
                  <p className="text-sm text-[var(--color-text)] truncate">{song.title}</p>
                  <p className="text-xs text-[var(--color-text-muted)]">
                    {song.requestedBy} · {formatDuration(song.durationSeconds)}
                  </p>
                </div>
                {song.pointsCost != null && song.pointsCost > 0 && (
                  <Badge>{song.pointsCost} pts</Badge>
                )}
                <button
                  onClick={() => removeMutation.mutate(song.id)}
                  className="rounded p-1.5 text-[var(--color-error)] hover:bg-[var(--color-elevated)]"
                >
                  <Trash2 className="h-4 w-4" />
                </button>
              </div>
            ))}
          </div>
        </Card>
      )}

      {showMessages && <SRMessagesModal onClose={() => setShowMessages(false)} />}
      {showSettings && <SRSettingsModal status={status} onClose={() => setShowSettings(false)} />}
    </div>
  );
}

// ─── Messages Modal ──────────────────────────────────────────

function SRMessagesModal({ onClose }: { onClose: () => void }) {
  const queryClient = useQueryClient();
  const [editedValues, setEditedValues] = useState<Record<string, string>>({});

  const { data, isLoading } = useQuery<SongRequestMessages>({
    queryKey: ["sr-messages"],
    queryFn: songRequestsApi.getMessages,
  });

  const saveMutation = useMutation({
    mutationFn: () => songRequestsApi.updateMessages(editedValues),
    onSuccess: async () => {
      await queryClient.refetchQueries({ queryKey: ["sr-messages"] });
      setEditedValues({});
      showToast("success", "Messages saved.");
    },
  });

  const resetMutation = useMutation({
    mutationFn: (key: string) => songRequestsApi.resetMessage(key),
    onSuccess: (_data, key) => {
      queryClient.setQueryData<SongRequestMessages>(["sr-messages"], (old) => {
        if (!old) return old;
        return { ...old, messages: { ...old.messages, [key]: old.defaults[key] } };
      });
      setEditedValues((prev) => { const n = { ...prev }; delete n[key]; return n; });
      showToast("success", "Message reset to default.");
    },
  });

  if (isLoading || !data) {
    return <Modal open={true} onClose={onClose} title="Song Request Messages" size="lg">
      <div className="text-center py-8 text-sm text-[var(--color-text-muted)]">Loading...</div>
    </Modal>;
  }

  const keys = Object.keys(data.defaults);

  function getCurrent(key: string) {
    return editedValues[key] ?? data!.messages[key] ?? data!.defaults[key] ?? "";
  }

  return (
    <Modal open={true} onClose={onClose} title="Song Request — Bot Messages" size="lg">
      <p className="text-sm text-[var(--color-text-secondary)] mb-4">
        Variables: {"{title}"}, {"{position}"}, {"{cost}"}, {"{max}"}, {"{user}"}
      </p>
      <div className="space-y-3 max-h-[60vh] overflow-y-auto pr-1">
        {keys.map((key) => {
          const current = getCurrent(key);
          const def = data.defaults[key];
          const customized = data.messages[key] !== def || key in editedValues;
          const label = key.replace(/([A-Z])/g, " $1").trim();

          return (
            <div key={key} className="rounded-lg border border-[var(--color-border)] p-3 space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-xs font-medium text-[var(--color-text)]">{label}</span>
                {customized && (
                  <button onClick={() => resetMutation.mutate(key)} className="flex items-center gap-1 text-xs text-[var(--color-text-muted)] hover:text-[var(--color-text)]">
                    <RotateCcw className="h-3 w-3" /> Reset
                  </button>
                )}
              </div>
              <input type="text" value={current} onChange={(e) => setEditedValues((p) => ({ ...p, [key]: e.target.value }))}
                className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] px-3 py-1.5 text-sm text-[var(--color-text)] font-mono" />
              {current !== def && <p className="text-[10px] text-[var(--color-text-muted)]">Default: <span className="font-mono">{def}</span></p>}
            </div>
          );
        })}
      </div>
      <div className="flex justify-end gap-2 pt-4 border-t border-[var(--color-border)] mt-4">
        <button onClick={onClose} className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)]">Cancel</button>
        <button onClick={() => saveMutation.mutate()} disabled={Object.keys(editedValues).length === 0}
          className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] disabled:opacity-50">
          <Save className="h-4 w-4" /> Save
        </button>
      </div>
    </Modal>
  );
}

// ─── Settings Modal ──────────────────────────────────────────

function SRSettingsModal({ status, onClose }: { status?: SRStatus; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [maxDuration, setMaxDuration] = useState(status?.maxDuration ?? 600);
  const [maxPerUser, setMaxPerUser] = useState(status?.maxPerUser ?? 3);
  const [pointsCost, setPointsCost] = useState(status?.pointsCost ?? 0);

  const saveMutation = useMutation({
    mutationFn: () => songRequestsApi.updateSettings({ maxDuration, maxPerUser, pointsCost }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["song-status"] });
      showToast("success", "Settings saved.");
      onClose();
    },
  });

  return (
    <Modal open={true} onClose={onClose} title="Song Request Settings" size="sm">
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Max Duration (seconds)</label>
          <input type="number" value={maxDuration} onChange={(e) => setMaxDuration(Number(e.target.value))} min={30} max={3600}
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]" />
          <p className="text-xs text-[var(--color-text-muted)] mt-1">{Math.floor(maxDuration / 60)}m {maxDuration % 60}s</p>
        </div>
        <div>
          <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Max Songs Per User</label>
          <input type="number" value={maxPerUser} onChange={(e) => setMaxPerUser(Number(e.target.value))} min={1} max={50}
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]" />
        </div>
        <div>
          <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Points Cost Per Request</label>
          <input type="number" value={pointsCost} onChange={(e) => setPointsCost(Number(e.target.value))} min={0}
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]" />
          <p className="text-xs text-[var(--color-text-muted)] mt-1">0 = free</p>
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button onClick={onClose} className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)]">Cancel</button>
          <button onClick={() => saveMutation.mutate()} disabled={saveMutation.isPending}
            className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] disabled:opacity-50">
            Save
          </button>
        </div>
      </div>
    </Modal>
  );
}
