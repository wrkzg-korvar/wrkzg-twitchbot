import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Plus, Gift, RefreshCw, Trash2, Pencil } from "lucide-react";
import { channelPointsApi } from "../api/channelPoints";
import { PageHeader } from "../components/ui/PageHeader";
import { Card } from "../components/ui/Card";
import { Badge } from "../components/ui/Badge";
import { Modal } from "../components/ui/Modal";
import { Toggle } from "../components/ui/Toggle";
import { EmptyState } from "../components/ui/EmptyState";
import { ConfirmDialog } from "../components/ui/ConfirmDialog";
import { showToast } from "../hooks/useToast";
import { ACTION_TYPE_LABELS, RewardActionType } from "../types/channelPoints";
import type { ChannelPointReward, TwitchReward } from "../types/channelPoints";

export function ChannelPointsPage() {
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [deleteId, setDeleteId] = useState<number | null>(null);

  const { data: handlers } = useQuery<ChannelPointReward[]>({
    queryKey: ["channel-points"],
    queryFn: channelPointsApi.getAll,
  });

  const { data: twitchRewards } = useQuery<TwitchReward[]>({
    queryKey: ["channel-points-rewards"],
    queryFn: channelPointsApi.getRewards,
  });

  const syncMutation = useMutation({
    mutationFn: channelPointsApi.getRewards,
    onSuccess: (data) => {
      queryClient.setQueryData(["channel-points-rewards"], data);
      showToast("success", `Synced ${data.length} rewards from Twitch.`);
    },
    onError: () => showToast("error", "Failed to sync rewards from Twitch."),
  });

  const toggleMutation = useMutation({
    mutationFn: ({ id, isEnabled }: { id: number; isEnabled: boolean }) =>
      channelPointsApi.update(id, { isEnabled }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["channel-points"] }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => channelPointsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["channel-points"] });
      showToast("success", "Handler deleted.");
      setDeleteId(null);
    },
  });

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Channel Point Rewards"
        description="React to Twitch reward redemptions with configurable actions."
        helpKey="channel-points"
        actions={
          <div className="flex gap-2">
            <button
              onClick={() => syncMutation.mutate()}
              disabled={syncMutation.isPending}
              className="flex items-center gap-1.5 rounded-lg border border-[var(--color-border)] px-3 py-2 text-sm font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)] transition-colors"
            >
              <RefreshCw className={`h-4 w-4 ${syncMutation.isPending ? "animate-spin" : ""}`} /> Sync from Twitch
            </button>
            <button
              onClick={() => { setEditingId(null); setShowForm(true); }}
              className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-3 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] transition-colors"
            >
              <Plus className="h-4 w-4" /> Add Handler
            </button>
          </div>
        }
      />

      {handlers && handlers.length === 0 && (
        <EmptyState
          icon={Gift}
          title="No reward handlers configured"
          description="Click 'Sync from Twitch' to load your channel's rewards, then add handlers."
        />
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {(handlers ?? []).map((handler) => (
          <Card key={handler.id} title={handler.title} headerRight={
            <Badge>{handler.cost} pts</Badge>
          }>
            <div className="space-y-3">
              <div className="text-sm text-[var(--color-text-muted)]">
                Action: <span className="text-[var(--color-text)]">{ACTION_TYPE_LABELS[handler.actionType] ?? "Unknown"}</span>
              </div>
              {handler.actionPayload && (
                <div className="text-xs text-[var(--color-text-muted)] truncate" title={handler.actionPayload}>
                  {handler.actionPayload}
                </div>
              )}
              <div className="flex items-center justify-between pt-2 border-t border-[var(--color-border)]">
                <Toggle
                  checked={handler.isEnabled}
                  onChange={(checked) => toggleMutation.mutate({ id: handler.id, isEnabled: checked })}
                />
                <div className="flex gap-1">
                  <button
                    onClick={() => { setEditingId(handler.id); setShowForm(true); }}
                    className="rounded p-1.5 text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)]"
                  >
                    <Pencil className="h-4 w-4" />
                  </button>
                  <button
                    onClick={() => setDeleteId(handler.id)}
                    className="rounded p-1.5 text-[var(--color-error)] hover:bg-[var(--color-elevated)]"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              </div>
            </div>
          </Card>
        ))}
      </div>

      {showForm && (
        <ChannelPointFormModal
          editingHandler={editingId ? handlers?.find((h) => h.id === editingId) : undefined}
          twitchRewards={twitchRewards ?? []}
          onClose={() => setShowForm(false)}
          onSaved={() => {
            setShowForm(false);
            queryClient.invalidateQueries({ queryKey: ["channel-points"] });
          }}
        />
      )}

      <ConfirmDialog
        open={deleteId !== null}
        title="Delete Handler"
        message="Are you sure you want to delete this reward handler?"
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
        onCancel={() => setDeleteId(null)}
      />
    </div>
  );
}

function ChannelPointFormModal({
  editingHandler,
  twitchRewards,
  onClose,
  onSaved,
}: {
  editingHandler?: ChannelPointReward;
  twitchRewards: TwitchReward[];
  onClose: () => void;
  onSaved: () => void;
}) {
  const [rewardId, setRewardId] = useState(editingHandler?.twitchRewardId ?? "");
  const [actionType, setActionType] = useState(editingHandler?.actionType ?? RewardActionType.ChatMessage);
  const [payload, setPayload] = useState(editingHandler?.actionPayload ?? "");
  const [autoFulfill, setAutoFulfill] = useState(editingHandler?.autoFulfill ?? true);

  const selectedReward = twitchRewards.find((r) => r.id === rewardId);

  const createMutation = useMutation({
    mutationFn: () =>
      editingHandler
        ? channelPointsApi.update(editingHandler.id, { actionType, actionPayload: payload, autoFulfill })
        : channelPointsApi.create({
            twitchRewardId: rewardId,
            title: selectedReward?.title,
            cost: selectedReward?.cost,
            actionType,
            actionPayload: payload,
            autoFulfill,
          }),
    onSuccess: () => {
      showToast("success", editingHandler ? "Handler updated." : "Handler created.");
      onSaved();
    },
    onError: () => showToast("error", "Failed to save handler."),
  });

  return (
    <Modal open={true} title={editingHandler ? "Edit Handler" : "Add Handler"} onClose={onClose} size="md">
      <div className="space-y-4">
        {!editingHandler && (
          <div>
            <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Twitch Reward</label>
            <select
              value={rewardId}
              onChange={(e) => setRewardId(e.target.value)}
              className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
            >
              <option value="">Select a reward...</option>
              {twitchRewards.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.title} ({r.cost} pts)
                </option>
              ))}
            </select>
          </div>
        )}

        <div>
          <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Action Type</label>
          <select
            value={actionType}
            onChange={(e) => setActionType(Number(e.target.value) as RewardActionType)}
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
          >
            {Object.entries(ACTION_TYPE_LABELS).map(([val, label]) => (
              <option key={val} value={val}>{label}</option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-[var(--color-text)] mb-1">
            {actionType === RewardActionType.ChatMessage ? "Message Template" : "Payload"}
          </label>
          <input
            type="text"
            value={payload}
            onChange={(e) => setPayload(e.target.value)}
            placeholder={actionType === RewardActionType.ChatMessage ? "{user} redeemed a reward!" : ""}
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
          />
          {actionType === RewardActionType.ChatMessage && (
            <p className="mt-1 text-xs text-[var(--color-text-muted)]">Variables: {"{user}"}, {"{input}"}</p>
          )}
        </div>

        <div className="flex items-center justify-between">
          <label className="text-sm text-[var(--color-text)]">Auto-fulfill redemption</label>
          <Toggle checked={autoFulfill} onChange={setAutoFulfill} />
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <button onClick={onClose} className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]">
            Cancel
          </button>
          <button
            onClick={() => createMutation.mutate()}
            disabled={(!editingHandler && !rewardId) || createMutation.isPending}
            className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-50"
          >
            {editingHandler ? "Save" : "Create"}
          </button>
        </div>
      </div>
    </Modal>
  );
}
