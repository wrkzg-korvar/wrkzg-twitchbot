import { api } from "./client";

export interface EmoteDto {
  id: string;
  name: string;
  url: string;
  source: "global" | "channel" | "subscriber" | "bits" | "follower";
  /**
   * Which account loaded this emote. "shared" = available to both bot and broadcaster
   * (e.g. global emotes or emotes both accounts subscribe to).
   */
  owner: "bot" | "broadcaster" | "shared";
}

export const emotesApi = {
  getAll: () => api.get<EmoteDto[]>("/api/emotes"),
  refresh: () => api.post<{ count: number }>("/api/emotes/refresh"),
};
