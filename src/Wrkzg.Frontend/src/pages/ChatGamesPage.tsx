import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Plus, Gamepad2, Trash2, MessageSquare, RotateCcw, Save } from "lucide-react";
import { gamesApi } from "../api/games";
import { PageHeader } from "../components/ui/PageHeader";
import { Card } from "../components/ui/Card";
import { Badge } from "../components/ui/Badge";
import { Toggle } from "../components/ui/Toggle";
import { Modal } from "../components/ui/Modal";
import { EmptyState } from "../components/ui/EmptyState";
import { ConfirmDialog } from "../components/ui/ConfirmDialog";
import { showToast } from "../hooks/useToast";
import type { ChatGame, GameMessages, TriviaQuestion } from "../types/games";

const GAME_ICONS: Record<string, string> = {
  Heist: "🏦",
  Duel: "⚔️",
  Slots: "🎰",
  Roulette: "🎡",
  Trivia: "❓",
};

export function ChatGamesPage() {
  const queryClient = useQueryClient();
  const [showTrivia, setShowTrivia] = useState(false);
  const [messagesGame, setMessagesGame] = useState<string | null>(null);

  const { data: games } = useQuery<ChatGame[]>({
    queryKey: ["games"],
    queryFn: gamesApi.getAll,
  });

  const toggleMutation = useMutation({
    mutationFn: (name: string) => gamesApi.toggle(name),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["games"] }),
  });

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Chat Games"
        description="Points-based chat games for viewer engagement."
        helpKey="chat-games"
        actions={
          <button
            onClick={() => setShowTrivia(true)}
            className="flex items-center gap-1.5 rounded-lg border border-[var(--color-border)] px-3 py-2 text-sm font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)] transition-colors"
          >
            Trivia Questions
          </button>
        }
      />

      {games && games.length === 0 && (
        <EmptyState
          icon={Gamepad2}
          title="No games available"
          description="Games will appear here once the backend registers them."
        />
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {(games ?? []).map((game) => (
          <Card key={game.name} title={`${GAME_ICONS[game.name] ?? "🎮"} ${game.name}`} headerRight={
            <Badge variant={game.isEnabled ? "success" : "default"}>
              {game.isEnabled ? "ON" : "OFF"}
            </Badge>
          }>
            <div className="space-y-3">
              <p className="text-sm text-[var(--color-text-muted)]">{game.description}</p>
              <div className="text-xs text-[var(--color-text-muted)]">
                Command: <code className="text-[var(--color-brand-text)]">{game.trigger}</code>
                {game.aliases.length > 0 && (
                  <span> (also: {game.aliases.join(", ")})</span>
                )}
              </div>
              <div className="flex items-center justify-between pt-2 border-t border-[var(--color-border)]">
                <Toggle
                  checked={game.isEnabled}
                  onChange={() => toggleMutation.mutate(game.name)}
                />
                <div className="flex gap-1">
                  <button
                    onClick={() => setMessagesGame(game.name)}
                    className="flex items-center gap-1 rounded px-2 py-1 text-xs text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)] hover:text-[var(--color-text)] transition-colors"
                    title="Edit bot messages"
                  >
                    <MessageSquare className="h-3.5 w-3.5" /> Messages
                  </button>
                </div>
              </div>
            </div>
          </Card>
        ))}
      </div>

      {showTrivia && (
        <TriviaQuestionsModal onClose={() => setShowTrivia(false)} />
      )}

      {messagesGame && (
        <GameMessagesModal
          gameName={messagesGame}
          onClose={() => setMessagesGame(null)}
        />
      )}
    </div>
  );
}

// ─── Game Messages Modal ─────────────────────────────────────

function GameMessagesModal({ gameName, onClose }: { gameName: string; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [editedValues, setEditedValues] = useState<Record<string, string>>({});

  const { data, isLoading } = useQuery<GameMessages>({
    queryKey: ["game-messages", gameName],
    queryFn: () => gamesApi.getMessages(gameName),
  });

  const saveMutation = useMutation({
    mutationFn: () => gamesApi.updateMessages(gameName, editedValues),
    onSuccess: async () => {
      await queryClient.refetchQueries({ queryKey: ["game-messages", gameName] });
      setEditedValues({});
      showToast("success", "Messages saved.");
    },
    onError: () => showToast("error", "Failed to save messages."),
  });

  const resetMutation = useMutation({
    mutationFn: (messageKey: string) => gamesApi.resetMessage(gameName, messageKey),
    onSuccess: async (_data, messageKey) => {
      // Update cache immediately so UI reflects the default
      queryClient.setQueryData<GameMessages>(["game-messages", gameName], (old) => {
        if (!old) return old;
        return {
          ...old,
          messages: {
            ...old.messages,
            [messageKey]: old.defaults[messageKey],
          },
        };
      });
      setEditedValues((prev) => {
        const next = { ...prev };
        delete next[messageKey];
        return next;
      });
      showToast("success", "Message reset to default.");
    },
    onError: () => showToast("error", "Failed to reset message."),
  });

  if (isLoading || !data) {
    return (
      <Modal open={true} onClose={onClose} title={`${gameName} — Messages`} size="lg">
        <div className="text-center py-8 text-sm text-[var(--color-text-muted)]">Loading...</div>
      </Modal>
    );
  }

  const messageKeys = Object.keys(data.defaults);
  const hasChanges = Object.keys(editedValues).length > 0;

  function getCurrentValue(key: string): string {
    if (key in editedValues) {
      return editedValues[key];
    }
    return data!.messages[key] ?? data!.defaults[key] ?? "";
  }

  function isCustomized(key: string): boolean {
    const current = data!.messages[key];
    const defaultVal = data!.defaults[key];
    return current !== undefined && current !== defaultVal;
  }

  function handleChange(key: string, value: string) {
    setEditedValues((prev) => ({ ...prev, [key]: value }));
  }

  return (
    <Modal open={true} onClose={onClose} title={`${GAME_ICONS[gameName] ?? "🎮"} ${gameName} — Bot Messages`} size="lg">
      <div className="space-y-1 mb-4">
        <p className="text-sm text-[var(--color-text-secondary)]">
          Customize every message the bot sends for this game. Use <code className="text-xs bg-[var(--color-elevated)] px-1 rounded">{"{ }"}</code> variables in your templates.
        </p>
      </div>

      <div className="space-y-3 max-h-[60vh] overflow-y-auto pr-1">
        {messageKeys.map((key) => {
          const currentValue = getCurrentValue(key);
          const defaultValue = data.defaults[key];
          const customized = isCustomized(key) || key in editedValues;
          const label = key.replace(/([A-Z])/g, " $1").trim();

          return (
            <div key={key} className="rounded-lg border border-[var(--color-border)] p-3 space-y-2">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-medium text-[var(--color-text)]">{label}</span>
                  {customized && (
                    <span className="text-[10px] rounded-full bg-[var(--color-brand-subtle)] text-[var(--color-brand-text)] px-1.5 py-0.5">
                      customized
                    </span>
                  )}
                </div>
                {(customized || key in editedValues) && (
                  <button
                    onClick={() => resetMutation.mutate(key)}
                    disabled={resetMutation.isPending}
                    className="flex items-center gap-1 rounded px-2 py-0.5 text-xs text-[var(--color-text-muted)] hover:bg-[var(--color-elevated)] hover:text-[var(--color-text)] transition-colors"
                    title="Reset to default"
                  >
                    <RotateCcw className="h-3 w-3" /> Reset
                  </button>
                )}
              </div>
              <input
                type="text"
                value={currentValue}
                onChange={(e) => handleChange(key, e.target.value)}
                className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] px-3 py-1.5 text-sm text-[var(--color-text)] font-mono"
              />
              {currentValue !== defaultValue && (
                <p className="text-[10px] text-[var(--color-text-muted)]">
                  Default: <span className="font-mono">{defaultValue}</span>
                </p>
              )}
            </div>
          );
        })}
      </div>

      <div className="flex justify-end gap-2 pt-4 border-t border-[var(--color-border)] mt-4">
        <button
          onClick={onClose}
          className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-elevated)]"
        >
          Cancel
        </button>
        <button
          onClick={() => saveMutation.mutate()}
          disabled={!hasChanges || saveMutation.isPending}
          className="flex items-center gap-1.5 rounded-lg bg-[var(--color-brand)] px-4 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-50"
        >
          <Save className="h-4 w-4" /> Save Changes
        </button>
      </div>
    </Modal>
  );
}

// ─── Trivia Questions Modal ──────────────────────────────────

function TriviaQuestionsModal({ onClose }: { onClose: () => void }) {
  const queryClient = useQueryClient();
  const [question, setQuestion] = useState("");
  const [answer, setAnswer] = useState("");
  const [category, setCategory] = useState("");
  const [deleteId, setDeleteId] = useState<number | null>(null);

  const { data: questions } = useQuery<TriviaQuestion[]>({
    queryKey: ["trivia-questions"],
    queryFn: gamesApi.getTriviaQuestions,
  });

  const createMutation = useMutation({
    mutationFn: () => gamesApi.createTriviaQuestion({
      question,
      answer,
      category: category || undefined,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trivia-questions"] });
      setQuestion("");
      setAnswer("");
      setCategory("");
      showToast("success", "Question added.");
    },
    onError: () => showToast("error", "Failed to add question."),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => gamesApi.deleteTriviaQuestion(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trivia-questions"] });
      showToast("success", "Question deleted.");
      setDeleteId(null);
    },
  });

  return (
    <Modal open={true} onClose={onClose} title="Trivia Questions" size="lg">
      <div className="space-y-4 max-h-[70vh] overflow-y-auto">
        <div className="space-y-2 pb-4 border-b border-[var(--color-border)]">
          <input
            type="text"
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
            placeholder="Question..."
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
          />
          <div className="flex gap-2">
            <input
              type="text"
              value={answer}
              onChange={(e) => setAnswer(e.target.value)}
              placeholder="Answer..."
              className="flex-1 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
            />
            <input
              type="text"
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              placeholder="Category (optional)"
              className="w-32 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
            />
            <button
              onClick={() => createMutation.mutate()}
              disabled={!question.trim() || !answer.trim() || createMutation.isPending}
              className="rounded-lg bg-[var(--color-brand)] px-3 py-2 text-sm font-medium text-[var(--color-bg)] hover:bg-[var(--color-brand-hover)] disabled:opacity-50"
            >
              <Plus className="h-4 w-4" />
            </button>
          </div>
        </div>

        {questions && questions.length === 0 && (
          <p className="text-sm text-[var(--color-text-muted)] text-center py-4">
            No custom questions yet. Built-in questions are used automatically.
          </p>
        )}

        <div className="space-y-2">
          {(questions ?? []).map((q) => (
            <div key={q.id} className="flex items-start justify-between gap-2 rounded-lg border border-[var(--color-border)] p-3">
              <div className="flex-1 min-w-0">
                <p className="text-sm text-[var(--color-text)] font-medium">{q.question}</p>
                <p className="text-xs text-[var(--color-text-muted)]">
                  Answer: <span className="text-[var(--color-brand-text)]">{q.answer}</span>
                  {q.category && <span className="ml-2">[{q.category}]</span>}
                </p>
              </div>
              <button
                onClick={() => setDeleteId(q.id)}
                className="rounded p-1.5 text-[var(--color-error)] hover:bg-[var(--color-elevated)]"
              >
                <Trash2 className="h-4 w-4" />
              </button>
            </div>
          ))}
        </div>
      </div>

      <ConfirmDialog
        open={deleteId !== null}
        title="Delete Question"
        message="Are you sure you want to delete this trivia question?"
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
        onCancel={() => setDeleteId(null)}
      />
    </Modal>
  );
}
