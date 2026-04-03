import { api } from "./client";
import type { Role, RoleAutoAssignCriteria } from "../types/roles";

export const rolesApi = {
  getAll: () => api.get<Role[]>("/api/roles"),

  create: (body: {
    name: string;
    priority?: number;
    color?: string;
    icon?: string;
    autoAssign?: RoleAutoAssignCriteria | null;
  }) => api.post<Role>("/api/roles", body),

  update: (id: number, body: {
    name?: string;
    priority?: number;
    color?: string;
    icon?: string;
    autoAssign?: RoleAutoAssignCriteria | null;
  }) => api.put<Role>(`/api/roles/${id}`, body),

  delete: (id: number) => api.del(`/api/roles/${id}`),

  getUserRoles: (userId: number) => api.get<Role[]>(`/api/users/${userId}/roles`),

  assignRole: (userId: number, roleId: number) =>
    api.post("/api/roles/assign", { userId, roleId }),

  removeRole: (userId: number, roleId: number) =>
    api.del(`/api/roles/assign/${userId}/${roleId}`),

  evaluateAll: () => api.post<{ usersUpdated: number }>("/api/roles/evaluate"),
};
