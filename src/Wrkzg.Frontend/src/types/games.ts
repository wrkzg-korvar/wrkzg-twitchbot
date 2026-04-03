export interface ChatGame {
  name: string;
  trigger: string;
  aliases: string[];
  description: string;
  isEnabled: boolean;
  minRolePriority: number;
}

export interface GameMessages {
  name: string;
  messages: Record<string, string>;
  defaults: Record<string, string>;
}

export interface TriviaQuestion {
  id: number;
  question: string;
  answer: string;
  acceptedAnswers: string[];
  category: string | null;
  isCustom: boolean;
  createdAt: string;
}
