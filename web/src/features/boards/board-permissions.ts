import { boardRoles, type BoardDetail, type BoardRole } from './boards-api'

export interface BoardPermissions {
  role: BoardRole | null
  canEdit: boolean
  canAdmin: boolean
}

/**
 * Mirrors BoardAccessService: an explicit membership wins, and every team member reads a team-visible board.
 * Archived boards are read-only for everyone, but admins can still restore them.
 */
export function boardPermissions(board: BoardDetail, userId: string, archived: boolean): BoardPermissions {
  const member = board.members.find((m) => m.userId === userId)
  const role: BoardRole | null = member?.role ?? (board.visibility === 'Team' ? 'Viewer' : null)
  const rank = role ? boardRoles.indexOf(role) : -1
  return {
    role,
    canEdit: !archived && rank >= boardRoles.indexOf('Editor'),
    canAdmin: rank >= boardRoles.indexOf('Admin'),
  }
}

export const boardRoleLabels: Record<BoardRole, string> = {
  Admin: 'Administrador',
  Editor: 'Editor',
  Viewer: 'Lector',
}

export const boardRoleDescriptions: Record<BoardRole, string> = {
  Admin: 'Gestiona miembros, renombra y archiva el tablero',
  Editor: 'Crea y mueve columnas y tarjetas, comenta y adjunta',
  Viewer: 'Solo puede ver el tablero',
}
