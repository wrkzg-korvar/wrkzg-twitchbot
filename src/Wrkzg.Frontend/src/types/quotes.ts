export interface Quote {
  id: number;
  number: number;
  text: string;
  quotedUser: string;
  savedBy: string;
  gameName: string | null;
  createdAt: string;
}

export interface CreateQuoteRequest {
  text: string;
  quotedUser: string;
  gameName?: string;
}
