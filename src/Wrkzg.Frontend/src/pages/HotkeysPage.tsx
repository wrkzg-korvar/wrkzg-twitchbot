import { useState, useCallback, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Plus, Trash2, Pencil, Play, AlertTriangle } from "lucide-react";
import { hotkeysApi } from "../api/hotkeys";
import { countersApi } from "../api/counters";
import { effectsApi } from "../api/effects";
import { PageHeader } from "../components/ui/PageHeader";
import { SmartDataTable } from "../components/ui/DataTable";
import type { SmartColumn } from "../components/ui/DataTable";
import { Toggle } from "../components/ui/Toggle";
import { Modal } from "../components/ui/Modal";
import { ConfirmDialog } from "../components/ui/ConfirmDialog";
import { showToast } from "../hooks/useToast";
import type { HotkeyBinding } from "../types/hotkeys";
import { ACTION_TYPES } from "../types/hotkeys";
import type { Counter } from "../types/counters";
import type { EffectList } from "../types/effects";

export function HotkeysPage() {
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [editingBinding, setEditingBinding] = useState<HotkeyBinding | null>(null);
  const [deleteId, setDeleteId] = useState<number | null>(null);
  const [showPermissionModal, setShowPermissionModal] = useState(false);

  const { data: bindings, isLoading, isError } = useQuery<HotkeyBinding[]>({
    queryKey: ["hotkeys"],
    queryFn: hotkeysApi.getAll,
  });

  const { data: permission } = useQuery({
    queryKey: ["hotkey-permission"],
    queryFn: hotkeysApi.getPermission,
  });

  const needsPermission = permission && permission.globalHotkeySupported && !permission.hasPermission;

  const toggleMutation = useMutation({
    mutationFn: ({ id, isEnabled }: { id: number; isEnabled: boolean }) =>
      hotkeysApi.update(id, { isEnabled }),
    onSuccess: (_data, { isEnabled }) => {
      showToast("success", `Hotkey ${isEnabled ? "enabled" : "disabled"}`);
      queryClient.invalidateQueries({ queryKey: ["hotkeys"] });
    },
    onError: () => showToast("error", "Failed to toggle hotkey."),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => hotkeysApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["hotkeys"] });
      showToast("success", "Hotkey deleted.");
      setDeleteId(null);
    },
  });

  const triggerMutation = useMutation({
    mutationFn: (id: number) => hotkeysApi.trigger(id),
    onSuccess: (result) => {
      showToast("success", `Triggered: ${result.action}`);
      if (result.action?.startsWith("Counter")) {
        queryClient.invalidateQueries({ queryKey: ["counters"] });
      }
    },
    onError: () => showToast("error", "Failed to trigger hotkey."),
  });

  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-[var(--color-text-muted)]">
        <p className="text-lg font-medium">Failed to load data</p>
        <p className="mt-1 text-sm">Please check your connection and try again.</p>
      </div>
    );
  }

  const columns: SmartColumn<HotkeyBinding>[] = [
    {
      key: "keyCombination",
      header: "Key",
      sortable: true,
      searchable: true,
      render: (v) => (
        <kbd className="rounded-lg bg-[var(--color-elevated)] px-3 py-1.5 font-mono text-sm font-semibold text-[var(--color-text)] border border-[var(--color-border)] whitespace-nowrap">
          {v as string}
        </kbd>
      ),
    },
    {
      key: "description",
      header: "Description",
      searchable: true,
      render: (v, row) => (
        <div>
          <div className="text-sm text-[var(--color-text)]">{(v as string) || row.actionType}</div>
          <div className="text-xs text-[var(--color-text-muted)]">
            {ACTION_TYPES.find((t) => t.value === row.actionType)?.label ?? row.actionType}
            {row.actionPayload && row.actionType === "ChatMessage" && (
              <span className="ml-1">— {row.actionPayload.length > 40 ? row.actionPayload.slice(0, 40) + "..." : row.actionPayload}</span>
            )}
          </div>
        </div>
      ),
    },
    {
      key: "actionType",
      header: "Type",
      sortable: true,
      render: (v) => (
        <span className="rounded bg-[var(--color-elevated)] px-2 py-0.5 text-xs text-[var(--color-text-muted)] border border-[var(--color-border)]">
          {ACTION_TYPES.find((t) => t.value === (v as string))?.label ?? (v as string)}
        </span>
      ),
    },
    {
      key: "isEnabled",
      header: "Active",
      sortable: true,
      render: (_, row) => (
        <Toggle
          checked={row.isEnabled}
          onChange={(checked) => { toggleMutation.mutate({ id: row.id, isEnabled: checked }); }}
        />
      ),
    },
    {
      key: "actions",
      header: "",
      className: "w-28 text-right",
      render: (_, row) => (
        <div className="flex items-center justify-end gap-1">
          <button
            onClick={(e) => { e.stopPropagation(); triggerMutation.mutate(row.id); }}
            className="rounded p-1.5 text-[var(--color-brand-text)] hover:bg-[var(--color-elevated)]"
            title="Test trigger"
          >
            <Play className="h-4 w-4" />
          </button>
          <button
            onClick={(e) => { e.stopPropagation(); setEditingBinding(row); setShowForm(true); }}
            className="rounded p-1.5 text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)]"
            title="Edit"
          >
            <Pencil className="h-4 w-4" />
          </button>
          <button
            onClick={(e) => { e.stopPropagation(); setDeleteId(row.id); }}
            className="rounded p-1.5 text-[var(--color-error)] hover:bg-[var(--color-elevated)]"
            title="Delete"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Hotkey Triggers"
        description="Map keyboard shortcuts to bot actions. Works with Stream Deck."
        helpKey="hotkeys"
        badge={
          needsPermission ? (
            <button
              onClick={() => setShowPermissionModal(true)}
              className="flex items-center gap-1 rounded-full bg-amber-500/15 px-2 py-0.5 text-xs font-medium text-amber-400 hover:bg-amber-500/25 transition-colors"
              title="Accessibility permission required"
            >
              <AlertTriangle className="h-3.5 w-3.5" /> Permission Required
            </button>
          ) : undefined
        }
        actions={
          <button
            onClick={() => { setEditingBinding(null); setShowForm(true); }}
            className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-3 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] transition-colors"
          >
            <Plus className="h-4 w-4" /> Add Hotkey
          </button>
        }
      />

      <SmartDataTable
        data={bindings ?? []}
        columns={columns}
        pageSize={25}
        searchPlaceholder="Search hotkeys..."
        emptyMessage="No hotkeys configured. Create keyboard shortcuts that trigger bot actions."
        isLoading={isLoading}
        getRowKey={(row) => row.id}
      />

      {showForm && (
        <HotkeyFormModal
          editingBinding={editingBinding ?? undefined}
          onClose={() => setShowForm(false)}
          onSaved={() => {
            setShowForm(false);
            queryClient.invalidateQueries({ queryKey: ["hotkeys"] });
          }}
        />
      )}

      <ConfirmDialog
        open={deleteId !== null}
        title="Delete Hotkey"
        message="Are you sure you want to delete this hotkey binding?"
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
        onCancel={() => setDeleteId(null)}
      />

      {showPermissionModal && (
        <PermissionModal
          platform={permission?.platform ?? ""}
          onClose={() => setShowPermissionModal(false)}
          onRefresh={() => queryClient.invalidateQueries({ queryKey: ["hotkey-permission"] })}
        />
      )}
    </div>
  );
}

// ─── Permission Modal ───────────────────────────────────────

function PermissionModal({ platform, onClose, onRefresh }: { platform: string; onClose: () => void; onRefresh: () => void }) {
  const [settingsOpened, setSettingsOpened] = useState(false);

  const openSettingsMutation = useMutation({
    mutationFn: hotkeysApi.requestPermission,
    onSuccess: () => {
      setSettingsOpened(true);
      showToast("info", "System Settings opened. Enable Wrkzg, then click 'Check Again'.");
    },
  });

  const [checking, setChecking] = useState(false);

  const checkPermission = async () => {
    setChecking(true);
    try {
      const result = await hotkeysApi.getPermission();
      if (result.hasPermission) {
        showToast("success", "Permission granted! Global hotkeys are now active.");
        onRefresh();
        onClose();
      } else {
        showToast("info", "Permission not yet granted. Make sure Wrkzg is enabled in the Accessibility list.");
        onRefresh();
      }
    } finally {
      setChecking(false);
    }
  };

  return (
    <Modal open={true} onClose={onClose} title="Accessibility Permission Required" size="md">
      <div className="space-y-4">
        <div className="rounded-lg bg-amber-500/10 border border-amber-500/20 p-4">
          <div className="flex gap-3">
            <AlertTriangle className="h-5 w-5 text-amber-400 flex-shrink-0 mt-0.5" />
            <div className="text-sm text-amber-200">
              {platform === "macos" ? (
                <>
                  <p className="font-medium mb-2">macOS requires Accessibility permission for global hotkeys.</p>
                  <p className="text-amber-200/70">
                    Without this permission, hotkeys won't respond when Wrkzg is in the background.
                    You can still use the <strong>Play button</strong> or <strong>API triggers</strong> as alternatives.
                  </p>
                </>
              ) : (
                <p>Global hotkeys require system permissions on your platform.</p>
              )}
            </div>
          </div>
        </div>

        {platform === "macos" && (
          <div className="space-y-3 text-sm text-[var(--color-text-secondary)]">
            <p className="font-medium text-[var(--color-text)]">How to grant permission:</p>
            <ol className="list-decimal list-inside space-y-1.5">
              <li>Click <strong>"Request Permission"</strong> below</li>
              <li>macOS will show a system dialog</li>
              <li>Go to <strong>System Settings &gt; Privacy &amp; Security &gt; Accessibility</strong></li>
              <li>Find <strong>Wrkzg</strong> in the list and enable it</li>
              <li>You may need to restart Wrkzg for changes to take effect</li>
            </ol>
          </div>
        )}

        <div className="border-t border-[var(--color-border)] pt-4 space-y-3">
          <p className="text-xs text-[var(--color-text-muted)]">
            <strong>Alternative:</strong> You can always trigger hotkeys via the API without any permissions.
            Use <code className="bg-[var(--color-elevated)] px-1 rounded">POST http://localhost:5050/api/hotkeys/{"{"}<em>id</em>{"}"}/trigger</code> from
            Stream Deck, a browser, or any HTTP tool. Each hotkey's ID is shown in the list.
          </p>
        </div>

        <div className="flex justify-end gap-2">
          <button onClick={onClose} className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)]">
            Close
          </button>
          {settingsOpened && (
            <button
              onClick={checkPermission}
              disabled={checking}
              className="rounded-lg border border-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-brand-text)] hover:bg-[var(--color-brand-subtle)] disabled:opacity-50"
            >
              Check Again
            </button>
          )}
          <button
            onClick={() => openSettingsMutation.mutate()}
            disabled={openSettingsMutation.isPending}
            className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-50"
          >
            Open System Settings
          </button>
        </div>
      </div>
    </Modal>
  );
}

// ─── Key Recorder Hook ──────────────────────────────────────

function useKeyRecorder(onRecord: (combo: string) => void) {
  const [isRecording, setIsRecording] = useState(false);

  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    e.preventDefault();
    e.stopPropagation();

    if (["Control", "Shift", "Alt", "Meta"].includes(e.key)) {
      return;
    }

    const parts: string[] = [];
    if (e.ctrlKey || e.metaKey) { parts.push("Ctrl"); }
    if (e.shiftKey) { parts.push("Shift"); }
    if (e.altKey) { parts.push("Alt"); }

    let key = e.key;
    if (key.length === 1) {
      key = key.toUpperCase();
    } else if (key.startsWith("Arrow")) {
      key = key.replace("Arrow", "");
    }

    parts.push(key);
    const combo = parts.join("+");

    onRecord(combo);
    setIsRecording(false);
  }, [onRecord]);

  useEffect(() => {
    if (isRecording) {
      window.addEventListener("keydown", handleKeyDown, true);
      return () => window.removeEventListener("keydown", handleKeyDown, true);
    }
  }, [isRecording, handleKeyDown]);

  return { isRecording, startRecording: () => setIsRecording(true), stopRecording: () => setIsRecording(false) };
}

// ─── Form Modal ─────────────────────────────────────────────

const NO_PAYLOAD_ACTIONS = ["PollEnd", "SongSkip"];

function HotkeyFormModal({
  editingBinding,
  onClose,
  onSaved,
}: {
  editingBinding?: HotkeyBinding;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [keyCombination, setKeyCombination] = useState(editingBinding?.keyCombination ?? "");
  const [actionType, setActionType] = useState(editingBinding?.actionType ?? "ChatMessage");
  const [actionPayload, setActionPayload] = useState(editingBinding?.actionPayload ?? "");
  const [description, setDescription] = useState(editingBinding?.description ?? "");

  const [pollQuestion, setPollQuestion] = useState("");
  const [pollOptions, setPollOptions] = useState("");
  const [pollDuration, setPollDuration] = useState(60);

  const [raffleTitle, setRaffleTitle] = useState("");
  const [raffleKeyword, setRaffleKeyword] = useState("!join");
  const [raffleDuration, setRaffleDuration] = useState(120);
  const [raffleMaxEntries, setRaffleMaxEntries] = useState(0);

  const isCounterAction = actionType.startsWith("Counter");
  const isNoPayloadAction = NO_PAYLOAD_ACTIONS.includes(actionType);

  useEffect(() => {
    if (!editingBinding) { return; }
    if (editingBinding.actionType === "PollStart" && editingBinding.actionPayload) {
      try {
        const parsed = JSON.parse(editingBinding.actionPayload);
        setPollQuestion(parsed.question ?? "");
        setPollOptions((parsed.options ?? []).join("\n"));
        setPollDuration(parsed.durationSeconds ?? 60);
      } catch { /* ignore parse errors */ }
    }
    if (editingBinding.actionType === "RaffleStart" && editingBinding.actionPayload) {
      try {
        const parsed = JSON.parse(editingBinding.actionPayload);
        setRaffleTitle(parsed.title ?? "");
        setRaffleKeyword(parsed.keyword ?? "!join");
        setRaffleDuration(parsed.durationSeconds ?? 120);
        setRaffleMaxEntries(parsed.maxEntries ?? 0);
      } catch { /* ignore parse errors */ }
    }
  }, [editingBinding]);

  const { data: counters } = useQuery<Counter[]>({
    queryKey: ["counters"],
    queryFn: countersApi.getAll,
    enabled: isCounterAction,
  });

  const { data: effects } = useQuery<EffectList[]>({
    queryKey: ["effects"],
    queryFn: effectsApi.getAll,
    enabled: actionType === "RunEffect",
  });

  const { isRecording, startRecording } = useKeyRecorder((combo) => {
    setKeyCombination(combo);
  });

  const buildPayload = (): string => {
    if (isNoPayloadAction) { return ""; }
    if (actionType === "PollStart") {
      const options = pollOptions.split("\n").map((o) => o.trim()).filter((o) => o.length > 0);
      return JSON.stringify({ question: pollQuestion, options, durationSeconds: pollDuration });
    }
    if (actionType === "RaffleStart") {
      return JSON.stringify({
        title: raffleTitle,
        keyword: raffleKeyword,
        durationSeconds: raffleDuration,
        maxEntries: raffleMaxEntries,
      });
    }
    return actionPayload;
  };

  const canSave = (): boolean => {
    if (!keyCombination.trim()) { return false; }
    if (isNoPayloadAction) { return true; }
    if (actionType === "PollStart") {
      const options = pollOptions.split("\n").map((o) => o.trim()).filter((o) => o.length > 0);
      return pollQuestion.trim().length > 0 && options.length >= 2;
    }
    if (actionType === "RaffleStart") {
      return raffleTitle.trim().length > 0;
    }
    return actionPayload.trim().length > 0;
  };

  const mutation = useMutation({
    mutationFn: () => {
      const payload = buildPayload();
      return editingBinding
        ? hotkeysApi.update(editingBinding.id, { keyCombination, actionType, actionPayload: payload, description: description || undefined })
        : hotkeysApi.create({ keyCombination, actionType, actionPayload: payload, description: description || undefined });
    },
    onSuccess: () => {
      showToast("success", editingBinding ? "Hotkey updated." : "Hotkey created.");
      onSaved();
    },
    onError: () => showToast("error", "Failed to save hotkey."),
  });

  const handleActionTypeChange = (newType: string) => {
    setActionType(newType);
    setActionPayload("");
    setPollQuestion(""); setPollOptions(""); setPollDuration(60);
    setRaffleTitle(""); setRaffleKeyword("!join"); setRaffleDuration(120); setRaffleMaxEntries(0);
  };

  const fieldClass = "w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]";

  return (
    <Modal open={true} title={editingBinding ? "Edit Hotkey" : "Add Hotkey"} onClose={onClose} size="md">
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Key Combination</label>
          <div className="flex gap-2">
            <div className={`flex-1 rounded-lg border px-3 py-2 text-sm font-mono text-center transition-colors ${isRecording ? "border-[var(--color-brand)] bg-[var(--color-brand-subtle)] text-[var(--color-brand-text)] animate-pulse" : "border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text)]"}`}>
              {isRecording ? "Press your key combination..." : keyCombination || "Not set"}
            </div>
            <button onClick={startRecording} type="button" className={`rounded-lg px-4 py-2 text-sm font-medium transition-colors ${isRecording ? "bg-[var(--color-brand)] text-[var(--color-bg)]" : "border border-[var(--color-border)] text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]"}`}>
              {isRecording ? "Listening..." : "Record"}
            </button>
          </div>
          <p className="text-xs text-[var(--color-text-muted)] mt-1">Click "Record" then press your desired key combination</p>
        </div>

        <div>
          <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Action</label>
          <select value={actionType} onChange={(e) => handleActionTypeChange(e.target.value)} className={fieldClass}>
            {ACTION_TYPES.map((t) => (<option key={t.value} value={t.value}>{t.label}</option>))}
          </select>
        </div>

        {actionType === "ChatMessage" && (
          <div>
            <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Message</label>
            <input type="text" value={actionPayload} onChange={(e) => setActionPayload(e.target.value)} placeholder="Message to send in chat" className={fieldClass} />
          </div>
        )}

        {isCounterAction && (
          <div>
            <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Counter</label>
            <select value={actionPayload} onChange={(e) => setActionPayload(e.target.value)} className={fieldClass}>
              <option value="">Select a counter...</option>
              {(counters ?? []).map((c) => (<option key={c.id} value={c.id.toString()}>{c.name} (current: {c.value})</option>))}
            </select>
          </div>
        )}

        {actionType === "RunEffect" && (
          <div>
            <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Automation</label>
            <select value={actionPayload} onChange={(e) => setActionPayload(e.target.value)} className={fieldClass}>
              <option value="">Select an automation...</option>
              {(effects ?? []).map((ef) => (<option key={ef.id} value={ef.id.toString()}>{ef.name}{ef.description ? ` -- ${ef.description}` : ""}</option>))}
            </select>
          </div>
        )}

        {actionType === "PollStart" && (
          <div className="space-y-3">
            <div>
              <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Question</label>
              <input type="text" value={pollQuestion} onChange={(e) => setPollQuestion(e.target.value)} placeholder="What should we play next?" className={fieldClass} />
            </div>
            <div>
              <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Options (one per line)</label>
              <textarea value={pollOptions} onChange={(e) => setPollOptions(e.target.value)} placeholder={"Option A\nOption B\nOption C"} rows={4} className={fieldClass} />
              <p className="text-xs text-[var(--color-text-muted)] mt-1">At least 2 options required</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Duration (seconds)</label>
              <input type="number" min={15} max={1800} value={pollDuration} onChange={(e) => setPollDuration(Number(e.target.value))} className="w-32 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]" />
            </div>
          </div>
        )}

        {actionType === "PollEnd" && (
          <div className="rounded-lg bg-[var(--color-elevated)] border border-[var(--color-border)] p-3">
            <p className="text-sm text-[var(--color-text-secondary)]">Ends the currently active poll. No configuration needed.</p>
          </div>
        )}

        {actionType === "RaffleStart" && (
          <div className="space-y-3">
            <div>
              <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Title</label>
              <input type="text" value={raffleTitle} onChange={(e) => setRaffleTitle(e.target.value)} placeholder="Win a gift sub!" className={fieldClass} />
            </div>
            <div className="grid grid-cols-3 gap-3">
              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Keyword</label>
                <input type="text" value={raffleKeyword} onChange={(e) => setRaffleKeyword(e.target.value)} placeholder="!join" className={fieldClass} />
              </div>
              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Duration (s)</label>
                <input type="number" min={10} value={raffleDuration} onChange={(e) => setRaffleDuration(Number(e.target.value))} className={fieldClass} />
              </div>
              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Max Entries</label>
                <input type="number" min={0} value={raffleMaxEntries} onChange={(e) => setRaffleMaxEntries(Number(e.target.value))} className={fieldClass} />
                <p className="text-xs text-[var(--color-text-muted)] mt-1">0 = unlimited</p>
              </div>
            </div>
          </div>
        )}

        {actionType === "SongSkip" && (
          <div className="rounded-lg bg-[var(--color-elevated)] border border-[var(--color-border)] p-3">
            <p className="text-sm text-[var(--color-text-secondary)]">Skips the current song. No configuration needed.</p>
          </div>
        )}

        {actionType === "PlayAlert" && (
          <div>
            <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Alert Message</label>
            <input type="text" value={actionPayload} onChange={(e) => setActionPayload(e.target.value)} placeholder="Alert text to display on the overlay" className={fieldClass} />
          </div>
        )}

        {actionType === "ObsSceneSwitch" && (
          <div>
            <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Scene Name</label>
            <input type="text" value={actionPayload} onChange={(e) => setActionPayload(e.target.value)} placeholder="Gaming" className={fieldClass} />
            <p className="text-xs text-[var(--color-text-muted)] mt-1">The exact name of the OBS scene to switch to</p>
          </div>
        )}

        {actionType === "ObsSourceToggle" && (
          <div>
            <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Scene | Source</label>
            <input type="text" value={actionPayload} onChange={(e) => setActionPayload(e.target.value)} placeholder="Gaming|Webcam" className={fieldClass} />
            <p className="text-xs text-[var(--color-text-muted)] mt-1">
              Format: SceneName|SourceName (toggles visibility). Optional: SceneName|SourceName|true or SceneName|SourceName|false
            </p>
          </div>
        )}

        <div>
          <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Description (optional)</label>
          <input type="text" value={description} onChange={(e) => setDescription(e.target.value)} placeholder="e.g. Increment death counter" className={fieldClass} />
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <button onClick={onClose} className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)]">Cancel</button>
          <button onClick={() => mutation.mutate()} disabled={!canSave() || mutation.isPending} className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] disabled:opacity-50">
            {editingBinding ? "Save" : "Create"}
          </button>
        </div>
      </div>
    </Modal>
  );
}
