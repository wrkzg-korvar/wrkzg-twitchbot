import { api } from "./client";
import type { User } from "../types/users";

const BASE = "/api/users";

export const usersApi = {
  getAll: (sortBy = "points", order = "desc", limit = 100) =>
    api.get<User[]>(`${BASE}?sortBy=${sortBy}&order=${order}&limit=${limit}`),
};
