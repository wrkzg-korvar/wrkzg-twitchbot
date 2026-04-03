export interface Role {
  id: number;
  name: string;
  priority: number;
  color: string | null;
  icon: string | null;
  autoAssign: RoleAutoAssignCriteria | null;
  createdAt: string;
  userCount?: number;
}

export interface RoleAutoAssignCriteria {
  minWatchedMinutes: number | null;
  minPoints: number | null;
  minMessages: number | null;
  mustBeFollower: boolean | null;
  mustBeSubscriber: boolean | null;
}
