import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Plus, Shield, Trash2, Pencil, RefreshCw } from "lucide-react";
import { rolesApi } from "../api/roles";
import { PageHeader } from "../components/ui/PageHeader";
import { Card } from "../components/ui/Card";
import { Badge } from "../components/ui/Badge";
import { Modal } from "../components/ui/Modal";
import { EmptyState } from "../components/ui/EmptyState";
import { ConfirmDialog } from "../components/ui/ConfirmDialog";
import { showToast } from "../hooks/useToast";
import type { Role, RoleAutoAssignCriteria } from "../types/roles";

export function RolesPage() {
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [editingRole, setEditingRole] = useState<Role | null>(null);
  const [deleteId, setDeleteId] = useState<number | null>(null);

  const { data: roles } = useQuery<Role[]>({
    queryKey: ["roles"],
    queryFn: rolesApi.getAll,
  });

  const evaluateMutation = useMutation({
    mutationFn: rolesApi.evaluateAll,
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ["roles"] });
      showToast("success", `Re-evaluated: ${result.usersUpdated} users updated.`);
    },
    onError: () => showToast("error", "Failed to evaluate roles."),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => rolesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["roles"] });
      showToast("success", "Role deleted.");
      setDeleteId(null);
    },
  });

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Roles & Ranks"
        description="Create community roles with automatic assignment based on activity."
        actions={
          <div className="flex gap-2">
            <button
              onClick={() => evaluateMutation.mutate()}
              disabled={evaluateMutation.isPending}
              className="flex items-center gap-1.5 rounded-lg border border-[var(--color-border)] px-3 py-2 text-sm font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)] transition-colors"
            >
              <RefreshCw className={`h-4 w-4 ${evaluateMutation.isPending ? "animate-spin" : ""}`} /> Re-evaluate All
            </button>
            <button
              onClick={() => { setEditingRole(null); setShowForm(true); }}
              className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-3 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] transition-colors"
            >
              <Plus className="h-4 w-4" /> Create Role
            </button>
          </div>
        }
      />

      {roles && roles.length === 0 && (
        <EmptyState
          icon={Shield}
          title="No roles created"
          description="Create your first community role to reward loyal viewers."
        />
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {(roles ?? []).map((role) => (
          <Card key={role.id} title={role.name} headerRight={
            <Badge>Priority {role.priority}</Badge>
          }>
            <div className="space-y-3">
              {(role.color || role.icon) && (
                <div className="flex items-center gap-2 text-sm">
                  {role.color && (
                    <span className="inline-block h-3 w-3 rounded-full" style={{ backgroundColor: role.color }} />
                  )}
                  {role.icon && <span>{role.icon}</span>}
                </div>
              )}
              {role.autoAssign && (
                <div className="text-xs text-[var(--color-text-muted)] space-y-1">
                  <p className="font-medium text-[var(--color-text-secondary)]">Auto-assign criteria:</p>
                  {role.autoAssign.minWatchedMinutes != null && (
                    <p>Watch time: {Math.floor(role.autoAssign.minWatchedMinutes / 60)}h</p>
                  )}
                  {role.autoAssign.minPoints != null && (
                    <p>Points: {role.autoAssign.minPoints.toLocaleString()}</p>
                  )}
                  {role.autoAssign.minMessages != null && (
                    <p>Messages: {role.autoAssign.minMessages.toLocaleString()}</p>
                  )}
                  {role.autoAssign.mustBeSubscriber && <p>Must be subscriber</p>}
                  {role.autoAssign.mustBeFollower && <p>Must be follower</p>}
                </div>
              )}
              {!role.autoAssign && (
                <p className="text-xs text-[var(--color-text-muted)]">Manual assignment only</p>
              )}

              <div className="flex items-center justify-between pt-2 border-t border-[var(--color-border)]">
                <span className="text-xs text-[var(--color-text-muted)]">
                  {role.userCount ?? 0} user{(role.userCount ?? 0) !== 1 ? "s" : ""}
                </span>
                <div className="flex gap-1">
                  <button
                    onClick={() => { setEditingRole(role); setShowForm(true); }}
                    className="rounded p-1.5 text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)]"
                  >
                    <Pencil className="h-4 w-4" />
                  </button>
                  <button
                    onClick={() => setDeleteId(role.id)}
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
        <RoleFormModal
          editingRole={editingRole ?? undefined}
          onClose={() => setShowForm(false)}
          onSaved={() => {
            setShowForm(false);
            queryClient.invalidateQueries({ queryKey: ["roles"] });
          }}
        />
      )}

      <ConfirmDialog
        open={deleteId !== null}
        title="Delete Role"
        message="Are you sure? All users with this role will lose it."
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
        onCancel={() => setDeleteId(null)}
      />
    </div>
  );
}

function RoleFormModal({
  editingRole,
  onClose,
  onSaved,
}: {
  editingRole?: Role;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [name, setName] = useState(editingRole?.name ?? "");
  const [priority, setPriority] = useState(editingRole?.priority ?? 0);
  const [color, setColor] = useState(editingRole?.color ?? "#8b5cf6");
  const [enableAutoAssign, setEnableAutoAssign] = useState(editingRole?.autoAssign != null);
  const [minHours, setMinHours] = useState(
    editingRole?.autoAssign?.minWatchedMinutes != null ? Math.floor(editingRole.autoAssign.minWatchedMinutes / 60) : 0
  );
  const [minPoints, setMinPoints] = useState(editingRole?.autoAssign?.minPoints ?? 0);
  const [minMessages, setMinMessages] = useState(editingRole?.autoAssign?.minMessages ?? 0);
  const [mustBeSub, setMustBeSub] = useState(editingRole?.autoAssign?.mustBeSubscriber ?? false);

  const mutation = useMutation({
    mutationFn: () => {
      const autoAssign: RoleAutoAssignCriteria | null = enableAutoAssign
        ? {
            minWatchedMinutes: minHours > 0 ? minHours * 60 : null,
            minPoints: minPoints > 0 ? minPoints : null,
            minMessages: minMessages > 0 ? minMessages : null,
            mustBeFollower: null,
            mustBeSubscriber: mustBeSub || null,
          }
        : null;

      return editingRole
        ? rolesApi.update(editingRole.id, { name, priority, color, autoAssign })
        : rolesApi.create({ name, priority, color, autoAssign });
    },
    onSuccess: () => {
      showToast("success", editingRole ? "Role updated." : "Role created.");
      onSaved();
    },
    onError: () => showToast("error", "Failed to save role."),
  });

  return (
    <Modal open={true} title={editingRole ? "Edit Role" : "Create Role"} onClose={onClose} size="md">
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Name</label>
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g. Elite Viewer"
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Priority</label>
            <input
              type="number"
              value={priority}
              onChange={(e) => setPriority(Number(e.target.value))}
              className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Color</label>
            <input
              type="color"
              value={color}
              onChange={(e) => setColor(e.target.value)}
              className="h-10 w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] cursor-pointer"
            />
          </div>
        </div>

        <div className="border-t border-[var(--color-border)] pt-4">
          <label className="flex items-center gap-2 text-sm font-medium text-[var(--color-text)] mb-3">
            <input
              type="checkbox"
              checked={enableAutoAssign}
              onChange={(e) => setEnableAutoAssign(e.target.checked)}
              className="rounded"
            />
            Enable auto-assign criteria
          </label>

          {enableAutoAssign && (
            <div className="space-y-3 pl-6">
              <div>
                <label className="block text-xs text-[var(--color-text-muted)] mb-1">Min. Watch Time (hours)</label>
                <input type="number" value={minHours} onChange={(e) => setMinHours(Number(e.target.value))}
                  className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-1.5 text-sm text-[var(--color-text)]" />
              </div>
              <div>
                <label className="block text-xs text-[var(--color-text-muted)] mb-1">Min. Points</label>
                <input type="number" value={minPoints} onChange={(e) => setMinPoints(Number(e.target.value))}
                  className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-1.5 text-sm text-[var(--color-text)]" />
              </div>
              <div>
                <label className="block text-xs text-[var(--color-text-muted)] mb-1">Min. Messages</label>
                <input type="number" value={minMessages} onChange={(e) => setMinMessages(Number(e.target.value))}
                  className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-1.5 text-sm text-[var(--color-text)]" />
              </div>
              <label className="flex items-center gap-2 text-sm text-[var(--color-text)]">
                <input type="checkbox" checked={mustBeSub} onChange={(e) => setMustBeSub(e.target.checked)} className="rounded" />
                Must be subscriber
              </label>
            </div>
          )}
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <button onClick={onClose} className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]">
            Cancel
          </button>
          <button
            onClick={() => mutation.mutate()}
            disabled={!name.trim() || mutation.isPending}
            className="rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-50"
          >
            {editingRole ? "Save" : "Create"}
          </button>
        </div>
      </div>
    </Modal>
  );
}
