import { api } from "./client";
import type { SpamFilterConfig } from "../types/spamFilter";

const BASE = "/api/spam-filter";

export const spamFilterApi = {
  get: () => api.get<SpamFilterConfig>(BASE),

  save: (config: SpamFilterConfig) => api.put<void>(BASE, config),
};
