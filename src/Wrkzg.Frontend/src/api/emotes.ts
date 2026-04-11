import { api } from "./client";

export interface EmoteDto {
  id: string;
  name: string;
  url: string;
  source: "global" | "channel" | "subscriber" | "bits" | "follower";
}

export const emotesApi = {
  getAll: () => api.get<EmoteDto[]>("/api/emotes"),
  refresh: () => api.post<{ count: number }>("/api/emotes/refresh"),
};
