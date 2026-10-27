import { api, type CreatedResponse } from '@/lib/api/client'

// Ordered like the API's TeamRole enum, so roles compare by privilege.
export const teamRoles = ['Member', 'Admin', 'Owner'] as const
export type TeamRole = (typeof teamRoles)[number]

export interface TeamSummary {
  id: string
  name: string
  myRole: TeamRole
  memberCount: number
}

export interface TeamMember {
  userId: string
  displayName: string
  email: string
  role: TeamRole
}

export const teamsApi = {
  list: () => api.get<TeamSummary[]>('/teams'),
  create: (name: string) => api.post<CreatedResponse>('/teams', { name }),
  members: (teamId: string) => api.get<TeamMember[]>(`/teams/${teamId}/members`),
  addMember: (teamId: string, email: string, role: TeamRole) =>
    api.post(`/teams/${teamId}/members`, { email, role }),
  changeRole: (teamId: string, userId: string, role: TeamRole) =>
    api.patch(`/teams/${teamId}/members/${userId}`, { role }),
  removeMember: (teamId: string, userId: string) => api.delete(`/teams/${teamId}/members/${userId}`),
}

export const teamRoleLabels: Record<TeamRole, string> = {
  Owner: 'Dueño',
  Admin: 'Administrador',
  Member: 'Miembro',
}

export function canManageTeam(role: TeamRole | undefined): boolean {
  return role === 'Owner' || role === 'Admin'
}
