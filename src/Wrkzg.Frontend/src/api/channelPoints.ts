import { api } from "./client";
import type { ChannelPointReward, TwitchReward } from "../types/channelPoints";

export const channelPointsApi = {
  getAll: () => api.get<ChannelPointReward[]>("/api/channel-points"),

  getRewards: () => api.get<TwitchReward[]>("/api/channel-points/rewards"),

  create: (body: {
    twitchRewardId: string;
    title?: string;
    cost?: number;
    actionType?: number;
    actionPayload?: string;
    autoFulfill?: boolean;
  }) => api.post<ChannelPointReward>("/api/channel-points", body),

  update: (id: number, body: {
    title?: string;
    actionType?: number;
    actionPayload?: string;
    autoFulfill?: boolean;
    isEnabled?: boolean;
  }) => api.put<ChannelPointReward>(`/api/channel-points/${id}`, body),

  delete: (id: number) => api.del(`/api/channel-points/${id}`),
};
