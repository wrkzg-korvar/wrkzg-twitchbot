import { api } from "./client";
import type { ChatGame, GameMessages, TriviaQuestion } from "../types/games";

export const gamesApi = {
  getAll: () => api.get<ChatGame[]>("/api/games"),

  toggle: (name: string) => api.post<{ name: string; isEnabled: boolean }>(`/api/games/${name}/toggle`),

  updateSettings: (name: string, settings: Record<string, string>) =>
    api.put(`/api/games/${name}/settings`, { settings }),

  getMessages: (name: string) => api.get<GameMessages>(`/api/games/${name}/messages`),

  updateMessages: (name: string, messages: Record<string, string>) =>
    api.put(`/api/games/${name}/messages`, { messages }),

  resetMessage: (name: string, messageKey: string) =>
    api.post<{ name: string; key: string; value: string }>(`/api/games/${name}/messages/${messageKey}/reset`),

  getTriviaQuestions: () => api.get<TriviaQuestion[]>("/api/games/trivia/questions"),

  createTriviaQuestion: (body: {
    question: string;
    answer: string;
    acceptedAnswers?: string[];
    category?: string;
  }) => api.post<TriviaQuestion>("/api/games/trivia/questions", body),

  deleteTriviaQuestion: (id: number) => api.del(`/api/games/trivia/questions/${id}`),
};
