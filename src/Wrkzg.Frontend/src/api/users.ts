import { api } from "./client";
import type { User } from "../types/users";

export interface PaginatedUsers {
  items: User[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const usersApi = {
  getPaginated: (params: {
    search?: string;
    sortBy?: string;
    order?: string;
    page?: number;
    pageSize?: number;
  }) => {
    const qs = new URLSearchParams();
    if (params.search) qs.set("search", params.search);
    if (params.sortBy) qs.set("sortBy", params.sortBy);
    if (params.order) qs.set("order", params.order);
    if (params.page) qs.set("page", params.page.toString());
    if (params.pageSize) qs.set("pageSize", params.pageSize.toString());
    return api.get<PaginatedUsers>(`/api/users?${qs.toString()}`);
  },

  getById: (id: number) => api.get<User>(`/api/users/${id}`),

  update: (id: number, data: { points?: number; isBanned?: boolean }) =>
    api.put<User>(`/api/users/${id}`, data),

  getCount: () => api.get<{ count: number }>("/api/users/count"),
};
